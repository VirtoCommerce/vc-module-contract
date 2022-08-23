using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VirtoCommerce.Contract.Data.Repositories
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ContractDbContext>
    {
        public ContractDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<ContractDbContext>();

            builder.UseSqlServer("Data Source=(local);Initial Catalog=VirtoCommerce3target40;Trusted_Connection=True;Connect Timeout=30");

            return new ContractDbContext(builder.Options);
        }
    }
}
