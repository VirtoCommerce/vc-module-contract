using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VirtoCommerce.Contracts.Data.Repositories
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ContractDbContext>
    {
        public ContractDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<ContractDbContext>();

            builder.UseSqlServer("Data Source=(local);Initial Catalog=VirtoCommerce3target47-2;Persist Security Info=True;Trusted_Connection=True;MultipleActiveResultSets=True;Connect Timeout=30");

            return new ContractDbContext(builder.Options);
        }
    }
}
