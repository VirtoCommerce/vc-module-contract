using System.Reflection;
using EntityFrameworkCore.Triggers;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.Contracts.Data.Models;

namespace VirtoCommerce.Contracts.Data.Repositories
{
    public class ContractDbContext : DbContextWithTriggers
    {
        private const int _maxLength128 = 128;

        public ContractDbContext(DbContextOptions<ContractDbContext> options)
            : base(options)
        {
        }

        protected ContractDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ContractEntity>().ToTable("Contract").HasKey(x => x.Id);
            modelBuilder.Entity<ContractEntity>().Property(x => x.Id).HasMaxLength(_maxLength128).ValueGeneratedOnAdd();

            modelBuilder.Entity<ContractDynamicPropertyObjectValueEntity>().ToTable("ContractDynamicPropertyObjectValue").HasKey(x => x.Id);
            modelBuilder.Entity<ContractDynamicPropertyObjectValueEntity>().Property(x => x.Id).HasMaxLength(128).ValueGeneratedOnAdd();
            modelBuilder.Entity<ContractDynamicPropertyObjectValueEntity>().Property(x => x.DecimalValue).HasColumnType("decimal(18,5)");
            modelBuilder.Entity<ContractDynamicPropertyObjectValueEntity>().HasOne(p => p.Contract)
                .WithMany(s => s.DynamicPropertyObjectValues).HasForeignKey(k => k.ObjectId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ContractDynamicPropertyObjectValueEntity>().HasIndex(x => new { x.ObjectType, x.ObjectId })
                .IsUnique(false)
                .HasDatabaseName("IX_ContractDynamicPropertyObjectValueEntity_ObjectType_ObjectId");

            modelBuilder.Entity<ContractAttachmentEntity>().ToTable("ContractAttachment").HasKey(x => x.Id);
            modelBuilder.Entity<ContractAttachmentEntity>().Property(x => x.Id).HasMaxLength(128).ValueGeneratedOnAdd();
            modelBuilder.Entity<ContractAttachmentEntity>().HasOne(x => x.Contract).WithMany(x => x.Attachments)
                        .HasForeignKey(x => x.ContractId).IsRequired().OnDelete(DeleteBehavior.Cascade);

            // Allows configuration for an entity type for different database types.
            // Applies configuration from all <see cref="IEntityTypeConfiguration{TEntity}" in VirtoCommerce.Contracts.Data.XXX project. /> 
            switch (this.Database.ProviderName)
            {
                case "Pomelo.EntityFrameworkCore.MySql":
                    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.Contracts.Data.MySql"));
                    break;
                case "Npgsql.EntityFrameworkCore.PostgreSQL":
                    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.Contracts.Data.PostgreSql"));
                    break;
                case "Microsoft.EntityFrameworkCore.SqlServer":
                    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.Contracts.Data.SqlServer"));
                    break;
            }

        }
    }
}
