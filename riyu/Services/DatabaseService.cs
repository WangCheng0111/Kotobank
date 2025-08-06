using Microsoft.EntityFrameworkCore;
using riyu.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace riyu.Services;

public class DatabaseService
{
    private readonly string _baseDataPath;
    
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
}