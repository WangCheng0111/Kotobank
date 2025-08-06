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
    [ObservableProperty] private string _greeting = "Welcome to Avalonia!";
    [ObservableProperty] private string _selectedFilePath = string.Empty;
    [ObservableProperty] private bool _isProcessing = false;
    
    private readonly ExcelService _excelService;
    private readonly DatabaseService _databaseService;
    
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
            Greeting = "无法获取应用窗口，文件选择失败";
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
            SelectedFilePath = selectedFile.Name; // 显示文件名而不是完整路径
            Greeting = $"已选择文件: {SelectedFilePath}";
            
            // 开始处理Excel文件
            await ProcessExcelFileAsync(selectedFile);
        }
        else
        {
            Greeting = "未选择任何文件";
        }
    }
    
    private async Task ProcessExcelFileAsync(IStorageFile file)
    {
        try
        {
            IsProcessing = true;
            Greeting = "正在处理Excel文件，请稍候...";
            
            // 读取Excel文件
            var sheetsData = await _excelService.ReadExcelFileAsync(file).ConfigureAwait(false);
            
            if (sheetsData.Count == 0)
            {
                Greeting = "Excel文件中没有找到有效的工作表";
                return;
            }
            
            // 将数据库保存操作也移到后台线程
            await Task.Run(async () =>
            {
                int totalSheets = sheetsData.Count;
                int processedSheets = 0;
                
                foreach (var sheetData in sheetsData)
                {
                    if (sheetData.Words.Count > 0)
                    {
                        // 更新进度提示（需要回到UI线程更新UI）
                        await UpdateGreetingAsync($"正在保存工作表 {processedSheets + 1}/{totalSheets}: {sheetData.SheetName} ({sheetData.Words.Count}个单词)");
                        
                        await _databaseService.SaveSheetDataAsync(sheetData.SheetName, sheetData.Words).ConfigureAwait(false);
                        processedSheets++;
                    }
                    else
                    {
                        // 跳过空的工作表
                        await UpdateGreetingAsync($"跳过空工作表: {sheetData.SheetName}");
                        await Task.Delay(200).ConfigureAwait(false); // 短暂延迟，让用户看到提示
                    }
                }
                
                // 完成提示
                await UpdateGreetingAsync($"导入成功！共处理了 {processedSheets} 个工作表，总计 {sheetsData.Sum(s => s.Words.Count)} 个单词");
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Greeting = $"处理Excel文件时出错: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }
    
    private async Task UpdateGreetingAsync(string message)
    {
        // 确保在UI线程上更新UI属性
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            Greeting = message;
        });
    }
}