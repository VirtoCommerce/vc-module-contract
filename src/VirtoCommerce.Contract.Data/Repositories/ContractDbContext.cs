using EntityFrameworkCore.Triggers;
using Microsoft.EntityFrameworkCore;

namespace VirtoCommerce.Contract.Data.Repositories
{
    public class ContractDbContext : DbContextWithTriggers
    {
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

            //modelBuilder.Entity<ContractEntity>().ToTable("Contract").HasKey(x => x.Id);
            //modelBuilder.Entity<ContractEntity>().Property(x => x.Id).HasMaxLength(128).ValueGeneratedOnAdd();
        }
    }
}
