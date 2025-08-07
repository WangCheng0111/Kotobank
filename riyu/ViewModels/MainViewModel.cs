using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using riyu.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace riyu.ViewModels;

public partial class MainViewModel : ViewModelBase
{


    [ObservableProperty] private bool _isProcessing = false;
    
    // UI元素的属性
    [ObservableProperty] private string _chineseTranslation = "这里将显示中文释义";
    [ObservableProperty] private string _japaneseInput = string.Empty;
    
    // 导入进度对话框控制
    [ObservableProperty] private bool _isImportDialogVisible = false;
    [ObservableProperty] private string _importStatus = "正在读取Excel文件...";
    [ObservableProperty] private double _importProgress = 0.0; // 进度值 0-100
    [ObservableProperty] private double _dialogTranslateY = 200.0; // 对话框Y轴偏移，用于滑动动画
    [ObservableProperty] private double _dialogOpacity = 0.0; // 对话框不透明度，用于淡入动画
    [ObservableProperty] private double _overlayOpacity = 0.0; // 遮罩不透明度，用于淡入动画
    
    private readonly ExcelService _excelService;
    private readonly DatabaseService _databaseService;
    
    // 存储导入的单词数据
    private List<Models.Word> _importedWords = new List<Models.Word>();
    
    public MainViewModel()
    {
        _excelService = new ExcelService();
        _databaseService = new DatabaseService();
    }
    
    [RelayCommand]
    private async Task SelectExcelFile()
    {
        TopLevel? topLevel = null;
        
        // 在MVVM模式中，我们通过ApplicationLifetime获取主窗口
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Windows桌面应用
            topLevel = desktop.MainWindow;
        }
        else if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            // Android应用
            topLevel = TopLevel.GetTopLevel(singleView.MainView);
        }
        
        if (topLevel == null) 
        {
            return;
        }
        
        var options = new FilePickerOpenOptions
        {
            Title = "选择Excel文件",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("Excel文件")
                {
                    Patterns = new[] { "*.xlsx", "*.xls" },
                    MimeTypes = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/vnd.ms-excel" }
                },
                new("所有文件")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        };
        
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        
        if (files != null && files.Count > 0)
        {
            var selectedFile = files[0];

            // 设置对话框初始位置（在上方）和透明度
            DialogTranslateY = 200.0; // 保持200.0，但MarginConverter现在使用底部外边距，所以是从上方滑入
            DialogOpacity = 0.0;
            OverlayOpacity = 0.0;
            
            // 显示导入对话框
            IsImportDialogVisible = true;
            ImportStatus = "正在读取Excel文件...";
            ImportProgress = 0.0;
            
            // 启动滑动和淡入动画
            _ = Task.Run(async () =>
            {
                await Task.Delay(50); // 短暂延迟确保对话框已渲染
                await Dispatcher.UIThread.InvokeAsync(() => 
                {
                    DialogTranslateY = 0.0;
                    DialogOpacity = 1.0;
                    OverlayOpacity = 1.0;
                });
            });
            
            // 开始处理Excel文件
            await ProcessExcelFileAsync(selectedFile);
        }
    }
    
    private async Task ProcessExcelFileAsync(IStorageFile file)
    {
        try
        {
            IsProcessing = true;
            ImportStatus = "正在处理Excel文件，请稍候...";
            await UpdateImportProgressAsync(10.0);
            
            // 读取Excel文件
            var sheetsData = await _excelService.ReadExcelFileAsync(file).ConfigureAwait(false);
            await UpdateImportProgressAsync(20.0);
            
            if (sheetsData.Count == 0)
            {
                ImportStatus = "Excel文件中没有找到有效的工作表";
                await UpdateImportProgressAsync(100.0);
                await Task.Delay(2000); // 显示错误消息2秒
                await HideImportDialogAsync();
                return;
            }
            
            // 将数据库保存操作也移到后台线程
            await Task.Run(async () =>
            {
                int totalSheets = sheetsData.Count;
                int processedSheets = 0;
                double baseProgress = 20.0; // 从20%开始
                double maxProgress = 80.0;  // 到80%结束
                
                foreach (var sheetData in sheetsData)
                {
                    if (sheetData.Words.Count > 0)
                    {
                        // 更新进度提示（需要回到UI线程更新UI）
                        await UpdateImportStatusAsync($"正在保存工作表 {processedSheets + 1}/{totalSheets}: {sheetData.SheetName} ({sheetData.Words.Count}个单词)");
                        
                        // 计算当前进度
                        double currentProgress = baseProgress + (maxProgress - baseProgress) * processedSheets / totalSheets;
                        await UpdateImportProgressAsync(currentProgress);
                        
                        await _databaseService.SaveSheetDataAsync(sheetData.SheetName, sheetData.Words).ConfigureAwait(false);
                        processedSheets++;
                    }
                    else
                    {
                        // 跳过空的工作表
                        await UpdateImportStatusAsync($"跳过空工作表: {sheetData.SheetName}");
                        
                        // 即使跳过，也要更新进度
                        double currentProgress = baseProgress + (maxProgress - baseProgress) * processedSheets / totalSheets;
                        await UpdateImportProgressAsync(currentProgress);
                        
                        await Task.Delay(200).ConfigureAwait(false); // 短暂延迟，让用户看到提示
                    }
                }
                
                // 存储所有导入的单词
                _importedWords.Clear();
                foreach (var sheetData in sheetsData)
                {
                    _importedWords.AddRange(sheetData.Words);
                }
                
                // 显示第一个单词的中文释义（如果有的话）
                if (_importedWords.Count > 0)
                {
                    await UpdateChineseTranslationAsync(_importedWords[0].Chinese);
                }
                
                // 设置进度到100%
                await UpdateImportProgressAsync(100.0);
                
                // 完成提示
                await UpdateImportStatusAsync($"导入成功！共处理了 {processedSheets} 个工作表，总计 {sheetsData.Sum(s => s.Words.Count)} 个单词");
                
                // 显示成功消息1秒后关闭对话框
                await Task.Delay(1000).ConfigureAwait(false);
                await HideImportDialogAsync();
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await UpdateImportProgressAsync(100.0); // 即使出错也设置进度到100%
            await UpdateImportStatusAsync($"处理Excel文件时出错: {ex.Message}");
            await Task.Delay(3000); // 显示错误消息3秒
            await HideImportDialogAsync();
        }
        finally
        {
            IsProcessing = false;
        }
    }
    

    private async Task UpdateChineseTranslationAsync(string translation)
    {
        // 确保在UI线程上更新UI属性
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            ChineseTranslation = translation;
        });
    }
    
    private async Task UpdateImportStatusAsync(string status)
    {
        // 确保在UI线程上更新UI属性
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            ImportStatus = status;
        });
    }
    
    private async Task UpdateImportProgressAsync(double progress)
    {
        // 确保在UI线程上更新UI属性
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            ImportProgress = progress;
        });
    }
    
    private async Task HideImportDialogAsync()
    {
        // 现代风离场动画：先执行动画，再隐藏对话框
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            // 开始离场动画：对话框向下滑动并淡出，遮罩淡出
            DialogTranslateY = -200.0;  // 向下滑动到屏幕外（负值表示向下滑出）
            DialogOpacity = 0.0;       // 对话框淡出
            OverlayOpacity = 0.0;      // 遮罩淡出
        });
        
        // 等待动画完成（与不透明度动画时间一致）
        await Task.Delay(500); // 等待不透明度动画完成（0.5秒）
        
        // 动画完成后隐藏对话框并重置状态
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            IsImportDialogVisible = false;
            ImportProgress = 0.0;  // 重置进度条为0
            ImportStatus = "正在读取Excel文件...";  // 重置状态文本
            DialogTranslateY = 200.0;  // 重置为入场动画的初始位置
            DialogOpacity = 0.0;       // 重置为入场动画的初始透明度
            OverlayOpacity = 0.0;      // 重置为入场动画的初始透明度
        });
    }

}