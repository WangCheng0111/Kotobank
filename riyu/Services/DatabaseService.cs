using Microsoft.EntityFrameworkCore;
using riyu.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq; // Added for OrderBy
using System.Text.Json;

namespace riyu.Services;

public class DatabaseService
{
    private readonly string _baseDataPath;
    private readonly string _indexFilePath;
    private List<SheetInfo>? _cachedSheets;
    private DateTime _lastIndexUpdate = DateTime.MinValue;
    
    public DatabaseService()
    {
        // 获取应用数据目录
        _baseDataPath = GetAppDataPath();
        
        // 确保WordTables目录存在
        var wordTablesPath = Path.Combine(_baseDataPath, "WordTables");
        if (!Directory.Exists(wordTablesPath))
        {
            Directory.CreateDirectory(wordTablesPath);
        }
        
        // 索引文件路径
        _indexFilePath = Path.Combine(wordTablesPath, "sheets_index.json");
    }
    
    public async Task SaveSheetDataAsync(string sheetName, List<Word> words)
    {
        // 创建sheet专用文件夹
        var sheetFolderName = SanitizeFileName(sheetName);
        var sheetFolderPath = Path.Combine(_baseDataPath, "WordTables", sheetFolderName);
        
        if (!Directory.Exists(sheetFolderPath))
        {
            Directory.CreateDirectory(sheetFolderPath);
        }
        
        // 数据库文件路径
        var dbPath = Path.Combine(sheetFolderPath, "words.db");
        
        // 创建数据库并保存数据
        using var context = new WordDbContext(dbPath);
        
        // 确保数据库被创建
        await context.Database.EnsureCreatedAsync();
        
        // 清空现有数据（如果重新导入）
        context.Words.RemoveRange(context.Words);
        
        // 添加新数据
        await context.Words.AddRangeAsync(words);
        
        // 保存到数据库
        await context.SaveChangesAsync();
        
        // 更新索引文件
        await UpdateSheetsIndexAsync();
        
        // 清除缓存
        _cachedSheets = null;
    }
    
    private string GetAppDataPath()
    {
        string appDataPath;
        
        // 跨平台获取应用数据目录
        if (OperatingSystem.IsAndroid())
        {
            // Android: 使用应用私有存储目录
            appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        }
        else
        {
            // Windows/其他桌面平台: 使用AppData目录
            appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        
        var appPath = Path.Combine(appDataPath, "日语单词斩");
        
        if (!Directory.Exists(appPath))
        {
            Directory.CreateDirectory(appPath);
        }
        
        return appPath;
    }
    
    private string SanitizeFileName(string fileName)
    {
        // 清理文件名中的非法字符
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        
        // 限制文件名长度
        if (sanitized.Length > 50)
        {
            sanitized = sanitized[..50];
        }
        
        return sanitized;
    }
    
    public async Task<List<SheetInfo>> GetAllSheetsAsync()
    {
        // 检查缓存是否有效
        if (_cachedSheets != null && IsIndexUpToDate())
        {
            return _cachedSheets;
        }
        
        // 尝试从索引文件加载
        var sheets = await LoadSheetsFromIndexAsync();
        if (sheets != null)
        {
            _cachedSheets = sheets;
            _lastIndexUpdate = File.GetLastWriteTime(_indexFilePath);
            return sheets;
        }
        
        // 如果索引文件不存在或无效，重新扫描并创建索引
        sheets = await ScanAndCreateIndexAsync();
        _cachedSheets = sheets;
        return sheets;
    }
    
    private async Task<List<SheetInfo>?> LoadSheetsFromIndexAsync()
    {
        try
        {
            if (!File.Exists(_indexFilePath))
                return null;
                
            var jsonContent = await File.ReadAllTextAsync(_indexFilePath);
            var sheets = JsonSerializer.Deserialize<List<SheetInfo>>(jsonContent);
            
            // 验证索引文件的有效性
            if (sheets != null && await ValidateIndexAsync(sheets))
            {
                return sheets;
            }
        }
        catch (Exception)
        {
            // 索引文件损坏，返回null以触发重新扫描
        }
        
        return null;
    }
    
    private Task<bool> ValidateIndexAsync(List<SheetInfo> sheets)
    {
        foreach (var sheet in sheets)
        {
            var dbPath = Path.Combine(sheet.FolderPath, "words.db");
            if (!File.Exists(dbPath))
                return Task.FromResult(false);
        }
        
        return Task.FromResult(true);
    }
    
    private async Task<List<SheetInfo>> ScanAndCreateIndexAsync()
    {
        var sheets = new List<SheetInfo>();
        var wordTablesPath = Path.Combine(_baseDataPath, "WordTables");
        
        if (!Directory.Exists(wordTablesPath))
        {
            return sheets;
        }
        
        var sheetFolders = Directory.GetDirectories(wordTablesPath);
        
        foreach (var sheetFolder in sheetFolders)
        {
            var dbPath = Path.Combine(sheetFolder, "words.db");
            
            if (File.Exists(dbPath))
            {
                try
                {
                    using var context = new WordDbContext(dbPath);
                    var wordCount = await context.Words.CountAsync();
                    
                    // 从文件夹名还原sheet名称（移除下划线替换）
                    var folderName = Path.GetFileName(sheetFolder);
                    var sheetName = folderName.Replace("_", " ");
                    
                    sheets.Add(new SheetInfo
                    {
                        Name = sheetName,
                        WordCount = wordCount,
                        FolderPath = sheetFolder
                    });
                }
                catch (Exception)
                {
                    // 如果某个数据库损坏，跳过它
                }
            }
        }
        
        // 按文件夹创建顺序排序
        var sortedSheets = sheets.OrderBy(s => s.Name, new SheetNameComparer()).ToList();
        
        // 保存到索引文件
        await SaveSheetsIndexAsync(sortedSheets);
        
        return sortedSheets;
    }
    
    private async Task SaveSheetsIndexAsync(List<SheetInfo> sheets)
    {
        try
        {
            var jsonContent = JsonSerializer.Serialize(sheets, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            await File.WriteAllTextAsync(_indexFilePath, jsonContent);
        }
        catch (Exception)
        {
            // 如果保存索引失败，不影响主要功能
        }
    }
    
    private async Task UpdateSheetsIndexAsync()
    {
        // 清除缓存并重新扫描
        _cachedSheets = null;
        await ScanAndCreateIndexAsync();
    }
    
    private bool IsIndexUpToDate()
    {
        if (!File.Exists(_indexFilePath))
            return false;
            
        var lastWriteTime = File.GetLastWriteTime(_indexFilePath);
        return lastWriteTime == _lastIndexUpdate;
    }
    
    // 清除缓存的方法（用于调试或手动刷新）
    public void ClearCache()
    {
        _cachedSheets = null;
        _lastIndexUpdate = DateTime.MinValue;
    }
}

// 新增的SheetInfo类
public class SheetInfo
{
    public string Name { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public string FolderPath { get; set; } = string.Empty;
}

// 自定义比较器，用于按sheet名称排序
public class SheetNameComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;
        
        // 尝试提取数字部分进行排序
        var xNumber = ExtractNumber(x);
        var yNumber = ExtractNumber(y);
        
        if (xNumber.HasValue && yNumber.HasValue)
        {
            return xNumber.Value.CompareTo(yNumber.Value);
        }
        
        // 如果无法提取数字，按字符串排序
        return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
    }
    
    private int? ExtractNumber(string text)
    {
        // 尝试从文本中提取数字（如 "sheet1" -> 1, "工作表2" -> 2）
        var match = System.Text.RegularExpressions.Regex.Match(text, @"\d+");
        if (match.Success && int.TryParse(match.Value, out int number))
        {
            return number;
        }
        return null;
    }
}