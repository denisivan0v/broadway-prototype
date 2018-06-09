using Microsoft.EntityFrameworkCore;

using NuClear.Broadway.Interfaces.Models;

namespace NuClear.Broadway.DataProjection
{
    public sealed class DataProjectionContext : DbContext
    {
        public DataProjectionContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<SecondRubric> SecondRubrics { get; set; }
        public DbSet<Rubric> Rubrics { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Firm> Firms { get; set; }
        public DbSet<CardForERM> Cards { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            const string CardCode = nameof(CardCode);
            modelBuilder.Entity<RubricLocalization>(
                builder =>
                    {
                        builder.Property<long>("Id").ForNpgsqlUseSequenceHiLo();
                        builder.Property(x => x.Lang).IsRequired();
                        builder.Property(x => x.Name).IsRequired();
                        builder.HasKey("Id");
                        builder.ToTable("RubricLocalizations");
                    });
            modelBuilder.Entity<BranchLocalization>(
                builder =>
                    {
                        builder.Property<long>("Id").ForNpgsqlUseSequenceHiLo();
                        builder.Property(x => x.Lang).IsRequired();
                        builder.Property(x => x.Name).IsRequired();
                        builder.HasKey("Id");
                        builder.ToTable("BranchLocalizations");
                    });
            modelBuilder.Entity<Category>(
                builder =>
                    {
                        builder.HasKey(x => x.Code);
                        builder.Property(x => x.Code).ValueGeneratedNever();
                        builder.Property(x => x.IsDeleted).IsRequired();
                        builder.HasMany(x => x.Localizations).WithOne();
                        builder.Ignore(x => x.SecondRubrics);
                    });
            modelBuilder.Entity<SecondRubric>(
                builder =>
                    {
                        builder.HasKey(x => x.Code);
                        builder.Property(x => x.Code).ValueGeneratedNever();
                        builder.Property(x => x.IsDeleted).IsRequired();
                        builder.HasMany(x => x.Localizations).WithOne();
                        builder.Ignore(x => x.Rubrics);
                    });
            modelBuilder.Entity<Rubric>(
                builder =>
                    {
                        builder.HasKey(x => x.Code);
                        builder.Property(x => x.Code).ValueGeneratedNever();
                        builder.Property(x => x.Code).IsRequired();
                        builder.HasMany(x => x.Localizations).WithOne();
                        builder.HasMany(x => x.Branches).WithOne().HasForeignKey(x => x.RubricCode);
                    });
            modelBuilder.Entity<RubricBranch>(
                builder =>
                    {
                        builder.HasKey(x => new { x.RubricCode, x.BranchCode });
                        builder.Property(x => x.RubricCode).IsRequired();
                        builder.Property(x => x.BranchCode).IsRequired();
                        builder.ToTable("RubricsBranches");
                    });
            modelBuilder.Entity<Branch>(
                builder =>
                    {
                        builder.HasKey(x => x.Code);
                        builder.Property(x => x.Code).ValueGeneratedNever();
                        builder.Property(x => x.Code).IsRequired();
                        builder.Property(x => x.DefaultCountryCode).IsRequired();
                        builder.Property(x => x.DefaultLang).IsRequired();
                        builder.HasMany(x => x.Localizations).WithOne();
                        builder.Ignore(x => x.EnabledLanguages);
                    });
            modelBuilder.Entity<Firm>(
                builder =>
                    {
                        builder.HasKey(x => x.Code);
                        builder.Property(x => x.Code).ValueGeneratedNever();
                        builder.Property(x => x.Code).IsRequired();
                        builder.Ignore(x => x.Cards);
                    });
            modelBuilder.Entity<CardForERM.Rubric>(
                builder =>
                    {
                        builder.Property<long>(CardCode);
                        builder.Property(x => x.Code).HasColumnName("RubricCode");
                        builder.HasKey(nameof(CardForERM.Rubric.Code), CardCode);
                        builder.ToTable("CardsRubrics");
                    });
            modelBuilder.Entity<CardForERM>(
                builder =>
                    {
                        builder.HasKey(x => x.Code);
                        builder.Property(x => x.Code).ValueGeneratedNever();
                        builder.Property(x => x.Code).IsRequired();
                        builder.OwnsOne(x => x.Address);
                        builder.HasMany(x => x.Rubrics).WithOne().HasForeignKey(CardCode).IsRequired();
                    });
        }
    }
}