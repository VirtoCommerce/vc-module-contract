using System;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.Contracts.Core.Models
{
    public class ContractAttachment : AuditableEntity, ICloneable
    {
        public string Name { get; set; }
        public string Url { get; set; }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }
    }
}
