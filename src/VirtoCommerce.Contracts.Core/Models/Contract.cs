using System;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.Contracts.Core.Models
{
    public class Contract : AuditableEntity, ICloneable
    {
        public string Name { get; set; }

        public string Code { get; set; }

        public string Description { get; set; }

        public string StoreId { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string BasePricelistAssignmentId { get; set; }

        public string PriorityPricelistAssignmentId { get; set; }


        public virtual object Clone()
        {
            return MemberwiseClone();
        }
    }
}
