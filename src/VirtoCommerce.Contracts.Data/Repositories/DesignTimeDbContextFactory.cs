using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VirtoCommerce.Contracts.Data.Repositories
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ContractDbContext>
    {
        public ContractDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<ContractDbContext>();

            builder.UseSqlServer("Data Source=(local);Initial Catalog=VirtoCommerce3;Persist Security Info=True;User ID=virto;Password=virto;MultipleActiveResultSets=True;Connect Timeout=30");

            return new ContractDbContext(builder.Options);
        }
    }
}
