using Microsoft.EntityFrameworkCore;
using FinFree.Api.Models;

namespace FinFree.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User 設定
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
        });

        // Category 設定
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Categories)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Transaction 設定
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Transactions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 預設分類種子資料
        SeedDefaultCategories(modelBuilder);
    }

    private void SeedDefaultCategories(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Category>().HasData(
            // 收入分類
            new Category { Id = 1, Name = "薪資", Type = TransactionType.Income, UserId = null, CreatedAt = seedDate },
            new Category { Id = 2, Name = "獎金", Type = TransactionType.Income, UserId = null, CreatedAt = seedDate },
            new Category { Id = 3, Name = "投資收益", Type = TransactionType.Income, UserId = null, CreatedAt = seedDate },
            new Category { Id = 4, Name = "其他收入", Type = TransactionType.Income, UserId = null, CreatedAt = seedDate },

            // 支出分類
            new Category { Id = 5, Name = "飲食", Type = TransactionType.Expense, UserId = null, CreatedAt = seedDate },
            new Category { Id = 6, Name = "交通", Type = TransactionType.Expense, UserId = null, CreatedAt = seedDate },
            new Category { Id = 7, Name = "娛樂", Type = TransactionType.Expense, UserId = null, CreatedAt = seedDate },
            new Category { Id = 8, Name = "購物", Type = TransactionType.Expense, UserId = null, CreatedAt = seedDate },
            new Category { Id = 9, Name = "醫療", Type = TransactionType.Expense, UserId = null, CreatedAt = seedDate },
            new Category { Id = 10, Name = "其他支出", Type = TransactionType.Expense, UserId = null, CreatedAt = seedDate }
        );
    }
}
