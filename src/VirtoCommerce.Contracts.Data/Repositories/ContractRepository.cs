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
        public IQueryable<ContractAttachmentEntity> ContractAttachments => DbContext.Set<ContractAttachmentEntity>();

        public async Task<IList<ContractEntity>> GetContractsByIdsAsync(IList<string> ids)
        {
            var contracts = await Contracts.Where(x => ids.Contains(x.Id)).ToListAsync();

            if (contracts.Any())
            {
                var contractIds = contracts.Select(x => x.Id).ToList();
                await DynamicPropertyObjectValues.Where(x => contractIds.Contains(x.ObjectId)).LoadAsync();
                await ContractAttachments.Where(x => contractIds.Contains(x.ContractId)).LoadAsync();
            }

            return contracts;
        }
    }
}
