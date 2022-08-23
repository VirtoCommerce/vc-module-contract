using System;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.Contract.Core.Models.Search
{
    public class ContractSearchCriteria : SearchCriteriaBase
    {
        public bool OnlyActive { get; set; }

        public string StoreId { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
