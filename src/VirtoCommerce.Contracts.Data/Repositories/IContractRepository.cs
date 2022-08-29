using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Contracts.Data.Models;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.Contracts.Data.Repositories
{
    public interface IContractRepository : IRepository
    {
        IQueryable<ContractEntity> Contracts { get; }

        Task<IEnumerable<ContractEntity>> GetContractsByIdsAsync(IEnumerable<string> ids);
    }
}
