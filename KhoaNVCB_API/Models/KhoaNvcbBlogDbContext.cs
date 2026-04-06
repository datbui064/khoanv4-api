using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace KhoaNVCB_API.Models;

public partial class KhoaNvcbBlogDbContext : DbContext
{
    public KhoaNvcbBlogDbContext()
    {
    }

    public KhoaNvcbBlogDbContext(DbContextOptions<KhoaNvcbBlogDbContext> options)
        : base(options)
    {
    }
    public DbSet<Topic> Topics { get; set; }
    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<ExternalResource> ExternalResources { get; set; }

    public virtual DbSet<Post> Posts { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<QuizAttempt> QuizAttempts { get; set; }

    public virtual DbSet<QuizSession> QuizSessions { get; set; }

    public virtual DbSet<QuizSetting> QuizSettings { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }
    public DbSet<SupportTicket> SupportTickets { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=sql5113.site4now.net;Database=db_ac7970_khoanv4;User Id=db_ac7970_khoanv4_admin;Password=12345678@NV4;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PK__Accounts__349DA5A6DEBD8657");

            entity.HasIndex(e => e.Username, "UQ__Accounts__536C85E4C0EAC112").IsUnique();

            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A0B75ACA4FE");

            entity.Property(e => e.CategoryName).HasMaxLength(200);
            entity.Property(e => e.Slug)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK__Categorie__Paren__09A971A2");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.CommentId).HasName("PK__Comments__C3B4DFCA8FC20199");

            entity.Property(e => e.Content).HasMaxLength(1000);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasDefaultValue("");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasDefaultValue("Ẩn danh");
            entity.Property(e => e.IsHidden).HasDefaultValue(false);
            entity.Property(e => e.Website).HasMaxLength(255);

            entity.HasOne(d => d.Post).WithMany(p => p.Comments)
                .HasForeignKey(d => d.PostId)
                .HasConstraintName("FK__Comments__PostId__0B91BA14");
        });

        modelBuilder.Entity<ExternalResource>(entity =>
        {
            entity.HasKey(e => e.ResourceId).HasName("PK__External__4ED1816F7BE773E7");

            entity.Property(e => e.ResourceType)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Post).WithMany(p => p.ExternalResources)
                .HasForeignKey(d => d.PostId)
                .HasConstraintName("FK__ExternalR__PostI__0C85DE4D");
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.PostId).HasName("PK__Posts__AA1260189FDDAAD7");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PublishedDate).HasColumnType("datetime");
            entity.Property(e => e.Slug)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.SourceType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Manual");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Draft");
            entity.Property(e => e.Summary).HasMaxLength(1000);
            entity.Property(e => e.Title).HasMaxLength(500);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.Author).WithMany(p => p.Posts)
                .HasForeignKey(d => d.AuthorId)
                .HasConstraintName("FK__Posts__AuthorId__0D7A0286");

            entity.HasOne(d => d.Category).WithMany(p => p.Posts)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Posts__CategoryI__0E6E26BF");

            entity.HasMany(d => d.Tags).WithMany(p => p.Posts)
                .UsingEntity<Dictionary<string, object>>(
                    "PostTag",
                    r => r.HasOne<Tag>().WithMany()
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__PostTags__TagId__10566F31"),
                    l => l.HasOne<Post>().WithMany()
                        .HasForeignKey("PostId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__PostTags__PostId__0F624AF8"),
                    j =>
                    {
                        j.HasKey("PostId", "TagId").HasName("PK__PostTags__7C45AF82D8B357E3");
                        j.ToTable("PostTags");
                    });
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.QuestionId).HasName("PK__Question__0DC06FAC160EBE09");

            entity.Property(e => e.CorrectAnswer).HasMaxLength(1);

            entity.HasOne(d => d.Category).WithMany(p => p.Questions)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Questions_Categories");
        });

        modelBuilder.Entity<QuizAttempt>(entity =>
        {
            entity.HasKey(e => e.AttemptId).HasName("PK__QuizAtte__891A68E68DB7C399");

            entity.Property(e => e.AttemptDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ClassName)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasDefaultValue("");
            entity.Property(e => e.StudentIdOrEmail)
                .HasMaxLength(255)
                .HasDefaultValue("");
        });

        modelBuilder.Entity<QuizSession>(entity =>
        {
            entity.ToTable("QuizSession");
            entity.HasKey(e => e.SessionId).HasName("PK__QuizSess__C9F492903100EBF4");

            entity.HasIndex(e => e.SessionCode, "UQ__QuizSess__30AEBB84A2F65E60").IsUnique();

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SessionCode).HasMaxLength(50);
            entity.Property(e => e.SessionName).HasMaxLength(255);
        });

        modelBuilder.Entity<QuizSetting>(entity =>
        {
            entity.HasKey(e => e.SettingId).HasName("PK__QuizSett__54372B1DA9ADD0F5");

            entity.Property(e => e.QuestionCount).HasDefaultValue(20);
            entity.Property(e => e.TimeLimitMinutes).HasDefaultValue(15);

            entity.HasOne(d => d.Category).WithMany(p => p.QuizSettings)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuizSettings_Categories");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.TagId).HasName("PK__Tags__657CF9AC43487F5D");

            entity.Property(e => e.TagName).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}