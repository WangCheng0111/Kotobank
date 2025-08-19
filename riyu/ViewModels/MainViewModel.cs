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
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media;

namespace riyu.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    // 窗口引用
    private Window? _window;
    
    // 设置窗口引用的方法
    public void SetWindowReference(Window window)
    {
        _window = window;
    }


    [ObservableProperty] private bool _isProcessing = false;
    
    // 窗口边距属性 - 用于窗口最大化时的边距调整
    [ObservableProperty] private Thickness _windowPadding = new Thickness(0);
    
    // 界面状态控制
    [ObservableProperty] private bool _hasImportedData = false; // 是否有导入的数据
    [ObservableProperty] private bool _isInitialized = false; // 是否已初始化
    [ObservableProperty] private bool _isLoadingData = true; // 是否正在加载数据
    
    // 计算属性：是否可以显示单词表按钮
    public bool CanShowWordListButton => HasImportedData && !IsLoadingData;
    
    // 窗口控制按钮状态
    private string _maximizeButtonIcon = "\uE922"; // 最大化按钮图标
    public string MaximizeButtonIcon
    {
        get => _maximizeButtonIcon;
        set => SetProperty(ref _maximizeButtonIcon, value);
    }
    
    // 播放状态控制
    [ObservableProperty] private bool _isPlaying = false; // 是否正在播放
    // 播放相关配色（进度条、中文释义）
    [ObservableProperty] private IBrush _progressBarBrush = new SolidColorBrush(Color.Parse("#666666"));
    [ObservableProperty] private IBrush _chineseTranslationBrush = new SolidColorBrush(Color.Parse("#2d2d2d"));
    
    // 进度控制
    [ObservableProperty] private double _progress = 0.0; // 进度值 0.0-1.0
    [ObservableProperty] private string _pageDisplay = "0/0"; // 页码显示
    
    // 进度管理
    [ObservableProperty] private int _studyIndex = -1; // 已学习进度，从-1开始
    [ObservableProperty] private int _currentIndex = 0; // 当前单词索引
    [ObservableProperty] private int _totalCount = 0; // 总单词数
    
    // 词性标签可见性控制
    [ObservableProperty] private bool _isWordTypeVisible = true;
    
    // 听写完成状态控制
    [ObservableProperty] private bool _isDictationCompleted = false; // 是否已完成听写
    
    // UI元素的属性
    [ObservableProperty] private string _chineseTranslation = string.Empty;
    [ObservableProperty] private string _japaneseInput = string.Empty;
    [ObservableProperty] private string _wordType = string.Empty;
    
    // 导入进度对话框控制
    [ObservableProperty] private bool _isImportDialogVisible = false;
    [ObservableProperty] private string _importStatus = "正在读取Excel文件...";
    [ObservableProperty] private double _importProgress = 0.0; // 进度值 0-100
    [ObservableProperty] private double _dialogTranslateY = 200.0; // 对话框Y轴偏移，用于滑动动画
    [ObservableProperty] private double _dialogOpacity = 0.0; // 对话框不透明度，用于淡入动画
    [ObservableProperty] private double _overlayOpacity = 0.0; // 遮罩不透明度，用于淡入动画
    
    // 单词表对话框控制
    [ObservableProperty] private bool _isWordListDialogVisible = false;
    [ObservableProperty] private double _wordListDialogTranslateY = 200.0; // 单词表对话框Y轴偏移，用于滑动动画
    [ObservableProperty] private double _wordListDialogOpacity = 0.0; // 单词表对话框不透明度，用于淡入动画
    [ObservableProperty] private double _wordListOverlayOpacity = 0.0; // 单词表遮罩不透明度，用于淡入动画
    
    // 单词表列表数据
    [ObservableProperty] private ObservableCollection<SheetInfo> _sheets = new();
    [ObservableProperty] private bool _isLoadingSheets = false;
    
    // 删除操作标志
    private bool _isDeleting = false;
    
    private readonly ExcelService _excelService;
    private readonly DatabaseService _databaseService;
    
    // 存储导入的单词数据
    private List<Models.Word> _importedWords = new List<Models.Word>();
    
    public MainViewModel()
    {
        _excelService = new ExcelService();
        _databaseService = new DatabaseService();
        
        // 初始化时检查数据库状态
        _ = InitializeAsync();
    }

    /// <summary>
    /// 初始化方法 - 检查本地数据库状态
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            // 设置加载状态
            IsLoadingData = true;
            // 检查是否有导入的单词表
            var hasSheets = await _databaseService.HasAnySheetsAsync();
            HasImportedData = hasSheets;
            
            if (hasSheets)
            {
                // 如果有数据，加载第一个sheet的第一个单词和总单词数量
                var firstWord = await _databaseService.GetFirstWordAsync();
                if (firstWord != null)
                {
                    ChineseTranslation = firstWord.Chinese;
                    WordType = string.IsNullOrEmpty(firstWord.PartOfSpeech) ? "未知" : firstWord.PartOfSpeech;
                    
                    // 获取第一个sheet的总单词数量
                    var sheets = await _databaseService.GetAllSheetsAsync();
                    if (sheets.Count > 0)
                    {
                        var firstSheet = sheets[0];
                        var words = _databaseService.GetWordsBySheetName(firstSheet.Name);
                        
                        // 更新进度相关的属性
                        TotalCount = words.Count;
                        CurrentIndex = 0;
                        StudyIndex = -1;
                        Progress = 0.0;
                        PageDisplay = $"0/{TotalCount}";
                        IsDictationCompleted = false; // 重置听写完成状态
                        
                        // 存储单词列表供后续使用
                        _importedWords = words;
                    }
                }
            }
            // 如果没有数据，界面会自动显示导入界面，不需要设置任何默认值
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"初始化时出错: {ex.Message}");
            // 出错时默认为无数据状态
            HasImportedData = false;
        }
        finally
        {
            IsLoadingData = false;
            IsInitialized = true;
        }
    }
    
    // 更新窗口边距的方法 - 供窗口状态变化时调用
    public void UpdateWindowPadding(bool isMaximized)
    {
        WindowPadding = isMaximized ? new Thickness(7) : new Thickness(0);
    }
    
    // 更新最大化按钮图标的方法
    public void UpdateMaximizeButtonIcon(bool isMaximized)
    {
        MaximizeButtonIcon = isMaximized ? "\uE923" : "\uE922";
    }
    
    // 窗口控制命令
    private RelayCommand? _minimizeWindowCommand;
    public RelayCommand MinimizeWindowCommand => _minimizeWindowCommand ??= new RelayCommand(MinimizeWindow);
    
    private RelayCommand? _maximizeWindowCommand;
    public RelayCommand MaximizeWindowCommand => _maximizeWindowCommand ??= new RelayCommand(MaximizeWindow);
    
    private RelayCommand? _closeWindowCommand;
    public RelayCommand CloseWindowCommand => _closeWindowCommand ??= new RelayCommand(CloseWindow);
    
    private void MinimizeWindow()
    {
        if (_window != null)
        {
            _window.WindowState = WindowState.Minimized;
        }
    }
    
    private void MaximizeWindow()
    {
        if (_window != null)
        {
            if (_window.WindowState == WindowState.Maximized)
            {
                _window.WindowState = WindowState.Normal;
            }
            else
            {
                _window.WindowState = WindowState.Maximized;
            }
        }
    }
    
    private void CloseWindow()
    {
        if (_window != null)
        {
            _window.Close();
        }
    }
    
    // 切换播放状态命令
    [RelayCommand]
    private void TogglePlay()
    {
        IsPlaying = !IsPlaying;
        if (IsPlaying)
        {
            // 播放时进度条变红；中文释义先保持默认，等待校验后再变色
            ProgressBarBrush = new SolidColorBrush(Colors.Red);
            ChineseTranslationBrush = new SolidColorBrush(Color.Parse("#2d2d2d"));
        }
        else
        {
            // 暂停时恢复默认
            ProgressBarBrush = new SolidColorBrush(Color.Parse("#666666"));
            ChineseTranslationBrush = new SolidColorBrush(Color.Parse("#2d2d2d"));
        }
    }
    
    // 确认答案命令
    [RelayCommand]
    private void ConfirmAnswer(string? inputFromView)
    {
        // 如果听写已完成，执行"再来一次"逻辑（暂停模式维持原行为，播放模式下也允许重来）
        if (IsDictationCompleted && !IsPlaying)
        {
            RestartDictation();
            return;
        }

        // 检查是否有当前单词
        if (_importedWords.Count == 0 || CurrentIndex >= _importedWords.Count)
        {
            return;
        }

        var currentWord = _importedWords[CurrentIndex];

        // 使用来自视图的即时文本（Android 上确保包含 IME 合成），回退到绑定值
        var actualInput = (inputFromView ?? JapaneseInput) ?? string.Empty;
        // 比较用户输入的日语单词与正确答案（忽略大小写和空格）
        bool isCorrect = string.Equals(
            actualInput.Trim(),
            currentWord.Japanese.Trim(),
            StringComparison.OrdinalIgnoreCase
        );

        if (IsPlaying)
        {
            // 播放模式：仅校验并变色，不跳转；始终清空输入
            ChineseTranslationBrush = isCorrect
                ? new SolidColorBrush(Colors.Green)
                : new SolidColorBrush(Colors.Red);
            JapaneseInput = string.Empty;
            return;
        }

        // 暂停模式：沿用原逻辑
        if (isCorrect)
        {
            NavigateToNextWord();
        }
        else
        {
            JapaneseInput = string.Empty;
        }
    }
    
    // 重新开始听写
    private void RestartDictation()
    {
        // 回到第一个单词
        CurrentIndex = 0;
        StudyIndex = -1;
        Progress = 0.0;
        PageDisplay = $"0/{TotalCount}";
        JapaneseInput = string.Empty;
        IsDictationCompleted = false;
        
        // 更新显示的单词内容
        UpdateCurrentWordDisplay();
    }
    
    // 上一个单词命令
    [RelayCommand]
    private void NavigateToPreviousWord()
    {
        // 如果是第一个单词，不做任何操作
        if (CurrentIndex <= 0)
            return;
            
        // 后退到上一个单词
        CurrentIndex--;
        
        // 清空日语输入框
        JapaneseInput = string.Empty;
        
        // 更新显示的单词内容
        UpdateCurrentWordDisplay();
        
        // 更新进度条和页码显示
        UpdatePageDisplay();
    }
    
    // 下一个单词命令
    [RelayCommand]
    private void NavigateToNextWord()
    {
        // 如果是最后一个单词，不做任何操作
        if (CurrentIndex >= TotalCount)
            return;
            
        // 前进到下一个单词
        CurrentIndex++;
        
        // 清空日语输入框
        JapaneseInput = string.Empty;
        
        // 更新显示的单词内容
        UpdateCurrentWordDisplay();
        
        // 更新进度条和页码显示
        UpdatePageDisplay();
    }
    
    // 更新当前显示的单词内容
    private void UpdateCurrentWordDisplay()
    {
        if (_importedWords.Count > 0 && CurrentIndex >= 0)
        {
            // 检查是否到达或超过最后一个单词
            if (CurrentIndex >= TotalCount)
            {
                // 已完成所有单词，显示完成提示
                ChineseTranslation = "你已完成此次听写";
                WordType = string.Empty;
                IsWordTypeVisible = false;
                IsDictationCompleted = true; // 设置听写完成状态
                // 显示完成时，中文释义颜色恢复为默认
                ChineseTranslationBrush = new SolidColorBrush(Color.Parse("#2d2d2d"));
            }
            else
            {
                // 显示当前单词内容
                var currentWord = _importedWords[CurrentIndex];
                ChineseTranslation = currentWord.Chinese;
                WordType = string.IsNullOrEmpty(currentWord.PartOfSpeech) ? "未知" : currentWord.PartOfSpeech;
                IsWordTypeVisible = true;
                IsDictationCompleted = false; // 重置听写完成状态
                // 切换单词时恢复默认颜色（暂停模式完全保持旧体验；播放模式下等待校验再变色）
                ChineseTranslationBrush = new SolidColorBrush(Color.Parse("#2d2d2d"));
            }
        }
    }
    
    // 进度更新方法
    private void UpdateProgress()
    {
        // 计算进度：已学习的单词数 / 总单词数
        // StudyIndex从-1开始，所以需要+1
        int completedCount = StudyIndex + 1;
        
        // 确保进度在0到1之间
        if (TotalCount == 0)
        {
            Progress = 0;
        }
        else
        {
            Progress = (double)completedCount / TotalCount;
        }
        
        // 页码显示：已学习的单词数/总数
        PageDisplay = $"{completedCount}/{TotalCount}";
    }
    
    // 更新页码显示，同时更新进度条以反映当前位置
    private void UpdatePageDisplay()
    {
        // 更新进度条以反映当前位置
        if (TotalCount == 0)
        {
            Progress = 0;
        }
        else
        {
            // 使用CurrentIndex来计算进度，这样第一个单词时进度条显示为0
            // 最后一个单词时进度条显示为满格
            Progress = (double)(CurrentIndex) / TotalCount;
        }
        
        // 页码显示：当前单词位置/总数（从0开始）
        PageDisplay = $"{CurrentIndex}/{TotalCount}";
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
        
        FilePickerOpenOptions options;
        if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime)
        {
            // Android 平台：仅允许常见的 xlsx/xlsm MIME，避免 txt/yaml 被显示
            options = new FilePickerOpenOptions
            {
                Title = "选择Excel文件",
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new("Excel 宏启用 (*.xlsm)")
                    {
                        MimeTypes = new[]
                        {
                            "application/vnd.ms-excel.sheet.macroEnabled.12",
                            "application/vnd.ms-excel.sheet.macroenabled.12"
                        }
                    },
                    new("Excel 工作簿 (*.xlsx)")
                    {
                        MimeTypes = new[]
                        {
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                        }
                    }
                }
            };
        }
        else
        {
            // 桌面平台：严格到 xlsx/xlsm
            options = new FilePickerOpenOptions
            {
                Title = "选择Excel文件",
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new("Excel文件")
                    {
                        Patterns = new[] { "*.xlsx", "*.xlsm" },
                        MimeTypes = new[]
                        {
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            "application/vnd.ms-excel.sheet.macroEnabled.12"
                        }
                    }
                }
            };
        }
        
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        
        if (files != null && files.Count > 0)
        {
            var selectedFile = files[0];
            var ext = Path.GetExtension(selectedFile.Name)?.ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xlsm")
            {
                await ShowMessageDialog("文件类型不支持", "仅支持导入 .xlsx 和 .xlsm 文件。");
                return;
            }

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
                int overriddenCount = 0;
                int createdCount = 0;
                int totalWords = 0;
                
                foreach (var sheetData in sheetsData)
                {
                    if (sheetData.Words.Count > 0)
                    {
                        bool exists = _databaseService.SheetExists(sheetData.SheetName);
                        string actionText = exists ? "覆盖" : "导入";
                        if (exists) overriddenCount++; else createdCount++;
                        totalWords += sheetData.Words.Count;
                        
                        // 更新进度提示（需要回到UI线程更新UI）
                        await UpdateImportStatusAsync($"正在{actionText}工作表 {processedSheets + 1}/{totalSheets}: {sheetData.SheetName} ({sheetData.Words.Count}个单词)");
                        
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
                
                // 存储第一个sheet的单词（导入后默认显示第一个sheet）
                _importedWords.Clear();
                if (sheetsData.Count > 0)
                {
                    _importedWords.AddRange(sheetsData[0].Words);
                }
                
                // 显示第一个单词的中文释义和词性（如果有的话）
                if (_importedWords.Count > 0)
                {
                    await UpdateChineseTranslationAsync(_importedWords[0].Chinese);
                    await UpdateWordTypeAsync(_importedWords[0].PartOfSpeech);
                }
                
                // 更新界面状态 - 现在有导入的数据了
                await Dispatcher.UIThread.InvokeAsync(() => 
                {
                    HasImportedData = true;
                    TotalCount = _importedWords.Count;  // 设置总单词数
                    CurrentIndex = 0;                   // 设置当前索引为0
                    StudyIndex = -1;                    // 设置学习进度为-1
                    Progress = 0.0;                     // 设置进度条为0
                    PageDisplay = $"0/{TotalCount}";    // 设置页码显示
                });
                
                // 设置进度到100%
                await UpdateImportProgressAsync(100.0);
                
                // 完成提示（覆盖/新建统计）
                await UpdateImportStatusAsync($"导入成功！覆盖 {overriddenCount} 个，新建 {createdCount} 个，总计 {totalWords} 个单词");
                
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
    
    private async Task UpdateWordTypeAsync(string wordType)
    {
        // 确保在UI线程上更新UI属性
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            WordType = string.IsNullOrEmpty(wordType) ? "未知" : wordType;
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
    
    [RelayCommand]
    private async Task ShowWordListDialog()
    {
        // 设置初始动画状态
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            WordListDialogTranslateY = 200.0; // 从下方滑入
            WordListDialogOpacity = 0.0;
            WordListOverlayOpacity = 0.0;
            
            // 显示单词表对话框
            IsWordListDialogVisible = true;
        });
        
        // 启动滑动和淡入动画
        _ = Task.Run(async () =>
        {
            await Task.Delay(50); // 短暂延迟确保对话框已渲染
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                WordListDialogTranslateY = 0.0;
                WordListDialogOpacity = 1.0;
                WordListOverlayOpacity = 1.0;
            });
        });
        
        // 切换为加载中并后台加载sheet列表（不阻塞UI）
        await Dispatcher.UIThread.InvokeAsync(() => { IsLoadingSheets = true; });
        _ = LoadSheetsAsync();
    }
    
    [RelayCommand]
    private async Task HideWordListDialog()
    {
        // 现代风离场动画：先执行动画，再隐藏对话框
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            // 开始离场动画：对话框向下滑动并淡出，遮罩淡出
            WordListDialogTranslateY = -200.0;  // 向下滑动到屏幕外（负值表示向下滑出）
            WordListDialogOpacity = 0.0;       // 对话框淡出
            WordListOverlayOpacity = 0.0;      // 遮罩淡出
        });
        
        // 等待动画完成（与不透明度动画时间一致）
        await Task.Delay(500); // 等待不透明度动画完成（0.5秒）
        
        // 动画完成后隐藏对话框并重置状态
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            IsWordListDialogVisible = false;
            WordListDialogTranslateY = 200.0;  // 重置为入场动画的初始位置
            WordListDialogOpacity = 0.0;       // 重置为入场动画的初始透明度
            WordListOverlayOpacity = 0.0;      // 重置为入场动画的初始透明度
        });
    }
    
    private async Task LoadSheetsAsync()
    {
        try
        {
            // 如果正在删除，跳过数据库操作
            if (_isDeleting)
            {
                return;
            }
            
            // 将磁盘与数据库读取放到后台线程，避免阻塞UI
            var sheets = await Task.Run(async () => await _databaseService.GetAllSheetsAsync());
            
            // 再次检查是否正在删除
            if (_isDeleting)
            {
                return;
            }
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Sheets.Clear();
                foreach (var sheet in sheets)
                {
                    Sheets.Add(sheet);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载sheet列表时出错: {ex.Message}");
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => { IsLoadingSheets = false; });
        }
    }
    
    [RelayCommand]
    private async Task RefreshSheetsList()
    {
        // 清除缓存并重新加载
        _databaseService.ClearCache();
        
        await Dispatcher.UIThread.InvokeAsync(() => { IsLoadingSheets = true; });
        await LoadSheetsAsync();
    }
    
    [RelayCommand]
    private async Task SelectSheet(SheetInfo sheetInfo)
    {
        try
        {
            // 关闭单词表列表对话框
            IsWordListDialogVisible = false;
            WordListDialogOpacity = 0.0;
            WordListOverlayOpacity = 0.0;
            WordListDialogTranslateY = 200.0;
            
            // 加载选中的sheet的单词
            await LoadSheetWordsAsync(sheetInfo);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"选择sheet时出错: {ex.Message}");
        }
    }
    
    private async Task LoadSheetWordsAsync(SheetInfo sheetInfo)
    {
        try
        {
            // 从数据库加载指定sheet的单词
            var words = await Task.Run(() => _databaseService.GetWordsBySheetName(sheetInfo.Name));
            
            if (words != null && words.Count > 0)
            {
                // 更新UI状态
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    HasImportedData = true;
                    TotalCount = words.Count;
                    CurrentIndex = 0;
                    StudyIndex = -1;
                    Progress = 0.0;
                    PageDisplay = $"0/{TotalCount}";
                    IsDictationCompleted = false; // 重置听写完成状态
                    
                    // 存储单词列表供后续使用
                    _importedWords = words;
                });
                
                // 显示第一个单词
                var firstWord = words[0];
                await UpdateChineseTranslationAsync(firstWord.Chinese);
                await UpdateWordTypeAsync(firstWord.PartOfSpeech);
            }
            else
            {
                // 如果没有单词，显示提示
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    HasImportedData = false;
                    ChineseTranslation = string.Empty;
                    WordType = string.Empty;
                    TotalCount = 0;
                    CurrentIndex = 0;
                    StudyIndex = -1;
                    Progress = 0.0;
                    PageDisplay = "0/0";
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载sheet单词时出错: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task DeleteAllSheets()
    {
        try
        {
            // 显示确认对话框
            var result = await ShowConfirmationDialog("确认删除", "确定要删除所有已导入的单词表吗？此操作不可恢复。");
            
            if (result)
            {
                // 设置删除标志
                _isDeleting = true;
                
                // 先更新UI状态，避免在删除过程中进行数据库查询
                await Dispatcher.UIThread.InvokeAsync(() => 
                {
                    // 更新界面状态 - 现在没有数据了
                    HasImportedData = false;
                    ChineseTranslation = string.Empty;
                    WordType = string.Empty;
                    Sheets.Clear();
                });
                
                // 执行删除操作（不显示加载状态）
                await Task.Run(() => _databaseService.DeleteAllSheets());
                
                // 显示成功消息
                await ShowMessageDialog("删除成功", "所有单词表已成功删除。如果某些文件无法立即删除，将在下次重启时自动删除。");
                
                // 重置删除标志
                _isDeleting = false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"删除所有单词表时出错: {ex.Message}");
            
            // 检查是否是文件被占用错误
            if (ex.Message.Contains("being used by another process") || ex.Message.Contains("The process cannot access the file"))
            {
                await ShowMessageDialog("删除失败", "无法删除文件，可能是因为文件正在被使用。请关闭所有相关程序后重试。");
            }
            else if (ex.Message.Contains("Access to the path") || ex.Message.Contains("is denied"))
            {
                await ShowMessageDialog("删除失败", "无法删除文件，可能是因为权限不足。某些文件将在下次重启时自动删除。");
            }
            else
            {
                await ShowMessageDialog("删除失败", $"删除单词表时出错: {ex.Message}");
            }
            
            // 重置删除标志
            _isDeleting = false;
        }
    }
    
    private async Task<bool> ShowConfirmationDialog(string title, string message)
    {
        // 简单的确认对话框实现
        // 在实际应用中，你可能想要使用更复杂的对话框组件
        return await Task.Run(() => 
        {
            // 这里可以集成实际的对话框组件
            // 暂时返回true作为示例
            return true;
        });
    }
    
    private async Task ShowMessageDialog(string title, string message)
    {
        // 简单的消息对话框实现
        // 在实际应用中，你可能想要使用更复杂的对话框组件
        await Task.Run(() => 
        {
            System.Diagnostics.Debug.WriteLine($"{title}: {message}");
        });
    }

}