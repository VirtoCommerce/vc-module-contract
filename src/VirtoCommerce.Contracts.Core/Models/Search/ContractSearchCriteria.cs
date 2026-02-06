using System;
using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.Contracts.Core.Models.Search
{
    public class ContractSearchCriteria : SearchCriteriaBase
    {
        public bool OnlyActive { get; set; }

        public string VendorId { get; set; }

        public string StoreId { get; set; }

        public IList<string> StoreIds { get; set; }

        public IList<string> Codes { get; set; }

        public IList<string> Statuses { get; set; }

        public DateTime? ActiveStartDate { get; set; }

        public DateTime? ActiveEndDate { get; set; }
    }
}
