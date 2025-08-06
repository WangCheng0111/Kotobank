using Microsoft.EntityFrameworkCore;

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
        optionsBuilder.UseSqlite($"Data Source={_databasePath}");
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
}