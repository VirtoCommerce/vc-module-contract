using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.Contracts.Core.Models.Search
{
    public class ContractMembersSearchCriteria : SearchCriteriaBase
    {
        public string ContractId { get; set; }

        public string ContractCode { get; set; }

        public bool DeepSearch { get; set; }
    }
}
