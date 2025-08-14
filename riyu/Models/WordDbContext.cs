using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace riyu.Models;

public class WordDbContext : DbContext
{
    public DbSet<Word> Words { get; set; } = null!;
    
    private readonly string _databasePath;
    
    public WordDbContext(string databasePath)
    {
        _databasePath = databasePath;
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Private
        }.ToString();
        
        optionsBuilder.UseSqlite(connectionString, options =>
        {
            options.CommandTimeout(30);
        });
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Word>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Japanese).IsRequired();
            entity.Property(e => e.Chinese).IsRequired();
            entity.Property(e => e.PartOfSpeech).HasDefaultValue(string.Empty);
        });
    }
    
    public override void Dispose()
    {
        // 确保连接被正确释放
        try
        {
            var connection = Database.GetDbConnection();
            if (connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();
            }
        }
        catch
        {
            // 忽略释放连接时的错误
        }
        
        base.Dispose();
    }
}