using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.Contracts.Core.Models.Search
{
    public class ContractPricesSearchCriteria : SearchCriteriaBase
    {
        public string ContractId { get; set; }

        public List<string> ProductIds { get; set; }
    }
}
