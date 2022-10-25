using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.Contracts.Data.Models;
using VirtoCommerce.Platform.Data.Infrastructure;

namespace VirtoCommerce.Contracts.Data.Repositories
{
    public class ContractRepository : DbContextRepositoryBase<ContractDbContext>, IContractRepository
    {
        public ContractRepository(ContractDbContext dbContext)
            : base(dbContext)
        {
        }

        public IQueryable<ContractEntity> Contracts => DbContext.Set<ContractEntity>();
        public IQueryable<ContractDynamicPropertyObjectValueEntity> DynamicPropertyObjectValues => DbContext.Set<ContractDynamicPropertyObjectValueEntity>();

        public async Task<IEnumerable<ContractEntity>> GetContractsByIdsAsync(IEnumerable<string> ids)
        {
            var contacts = await Contracts.Where(x => ids.Contains(x.Id)).ToListAsync();

            var contactIds = contacts.Select(x => x.Id).ToList();
            await DynamicPropertyObjectValues.Where(x => contactIds.Contains(x.ObjectId)).LoadAsync();

            return contacts;
        }
    }
}
