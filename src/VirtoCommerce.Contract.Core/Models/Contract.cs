using System;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.Contract.Core.Models
{
    public class Contract : AuditableEntity, ICloneable
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string StoreId { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }
    }
}
