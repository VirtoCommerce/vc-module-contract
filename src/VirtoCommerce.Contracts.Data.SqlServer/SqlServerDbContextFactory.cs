using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VirtoCommerce.Contracts.Data.Repositories;

namespace VirtoCommerce.Contracts.Data.SqlServer
{
    public class SqlServerDbContextFactory : IDesignTimeDbContextFactory<ContractDbContext>
    {
        public ContractDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<ContractDbContext>();
            var connectionString = args.Any() ? args[0] : "Data Source=(local);Initial Catalog=VirtoCommerce3;Persist Security Info=True;User ID=virto;Password=virto;MultipleActiveResultSets=True;Connect Timeout=30";

            builder.UseSqlServer(
                connectionString,
                db => db.MigrationsAssembly(typeof(SqlServerDbContextFactory).Assembly.GetName().Name));

            return new ContractDbContext(builder.Options);
        }
    }
}
