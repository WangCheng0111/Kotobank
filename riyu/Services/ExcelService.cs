using Avalonia.Platform.Storage;
using ClosedXML.Excel;
using riyu.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace riyu.Services;

public class ExcelService
{
    public class SheetData
    {
        public string SheetName { get; set; } = string.Empty;
        public List<Word> Words { get; set; } = new();
    }
    
    public async Task<List<SheetData>> ReadExcelFileAsync(IStorageFile file)
    {
        // 将整个Excel读取操作移到后台线程
        return await Task.Run(async () =>
        {
            var sheetsData = new List<SheetData>();
            
            Stream stream;
            
            // 在Windows上，尝试使用文件共享访问
            if (!OperatingSystem.IsAndroid())
            {
                // Windows桌面端：使用文件路径和共享访问
                var filePath = file.Path.LocalPath;
                stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            else
            {
                // Android：使用OpenReadAsync
                stream = await file.OpenReadAsync();
            }
            
            using (stream)
            {
                using var workbook = new XLWorkbook(stream);
                
                foreach (var worksheet in workbook.Worksheets)
                {
                    var sheetData = new SheetData
                    {
                        SheetName = worksheet.Name,
                        Words = ExtractWordsFromSheet(worksheet)
                    };
                    
                    sheetsData.Add(sheetData);
                }
            }
            
            return sheetsData;
        });
    }
    
    private List<Word> ExtractWordsFromSheet(IXLWorksheet worksheet)
    {
        var words = new List<Word>();
        
        // 从第2行开始读取（假设第1行是标题）
        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        
        for (int row = 2; row <= lastRowUsed; row++)
        {
            var japanese = worksheet.Cell(row, 1).GetString().Trim();
            var chinese = worksheet.Cell(row, 2).GetString().Trim();
            var partOfSpeech = worksheet.Cell(row, 3).GetString().Trim();
            
            // 跳过空行
            if (string.IsNullOrEmpty(japanese) && string.IsNullOrEmpty(chinese))
                continue;
            
            var word = new Word
            {
                Japanese = japanese,
                Chinese = chinese,
                PartOfSpeech = partOfSpeech
            };
            
            words.Add(word);
        }
        
        return words;
    }
}