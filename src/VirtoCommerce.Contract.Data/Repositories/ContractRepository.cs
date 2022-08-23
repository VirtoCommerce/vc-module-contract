using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.Contract.Data.Models;
using VirtoCommerce.Platform.Data.Infrastructure;

namespace VirtoCommerce.Contract.Data.Repositories
{
    public class ContractRepository : DbContextRepositoryBase<ContractDbContext>, IContractRepository
    {
        public ContractRepository(ContractDbContext dbContext)
            : base(dbContext)
        {
        }

        public IQueryable<ContractEntity> Contracts => DbContext.Set<ContractEntity>();

        public async Task<IEnumerable<ContractEntity>> GetContractsByIdsAsync(IEnumerable<string> ids)
        {
            return await Contracts.Where(x => ids.Contains(x.Id)).ToListAsync();
        }
    }
}
