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
        }
    }
}