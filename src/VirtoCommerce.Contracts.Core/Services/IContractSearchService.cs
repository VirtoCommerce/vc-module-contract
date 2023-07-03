using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Core.Models.Search;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.Contracts.Core.Services;

public interface IContractSearchService : ISearchService<ContractSearchCriteria, ContractSearchResult, Contract>
{
}
