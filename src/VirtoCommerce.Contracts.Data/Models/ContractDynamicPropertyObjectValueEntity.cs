using VirtoCommerce.Platform.Data.Model;

namespace VirtoCommerce.Contracts.Data.Models
{
    public class ContractDynamicPropertyObjectValueEntity : DynamicPropertyObjectValueEntity
    {
        public virtual ContractEntity Contract { get; set; }
    }
}
