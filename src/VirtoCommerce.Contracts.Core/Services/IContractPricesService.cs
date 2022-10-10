using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Core.Models.Search;
using VirtoCommerce.PricingModule.Core.Model;

namespace VirtoCommerce.Contracts.Core.Services
{
    public interface IContractPricesService
    {
        Task<Contract> LinkPricelist(ContractPricelist contractPricelist);
        Task<ContractPricesSearchResult> SearchContractGroupedPrices(ContractPricesSearchCriteria searchCriteria);
        Task<IEnumerable<MergedPrice>> GetContractProductPrices(ContractProduct contractProduct);
        Task SaveContractPrice(ContractProductPrices contractProductPrices);
        Task RestoreContractPrices(RestoreContractProductPrices contractProductPrices);
    }
}
