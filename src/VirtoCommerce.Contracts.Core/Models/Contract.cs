using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;

namespace VirtoCommerce.Contracts.Core.Models
{
    public class Contract : AuditableEntity, IHasDynamicProperties, ICloneable
    {
        public string Name { get; set; }

        public string Code { get; set; }

        public string Description { get; set; }

        public string StoreId { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string BasePricelistAssignmentId { get; set; }

        public string PriorityPricelistAssignmentId { get; set; }

        public string ObjectType => typeof(Contract).FullName;

        public ICollection<DynamicObjectProperty> DynamicProperties { get; set; } = new List<DynamicObjectProperty>();

        public virtual object Clone()
        {
            var clone = MemberwiseClone() as Contract;

            clone.DynamicProperties = DynamicProperties?.Select(x => x.Clone()).OfType<DynamicObjectProperty>().ToList();

            return clone;
        }
    }
}
