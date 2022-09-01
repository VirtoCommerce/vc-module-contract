using System.Threading.Tasks;
using VirtoCommerce.Contracts.Core.Models.Search;

namespace VirtoCommerce.Contracts.Core.Services
{
    public interface IContractMembersSearchService
    {
        Task<ContractMembersSearchResult> SearchAsync(ContractMembersSearchCriteria criteria);
    }
}
