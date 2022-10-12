using System.Collections.Generic;

namespace VirtoCommerce.Contracts.Core.Models
{
    public class RestoreContractProductPrices
    {
        public string ContractId { get; set; }

        public List<string> ProductIds { get; set; } = new List<string>();

        public List<string> PriceIds { get; set; } = new List<string>();
    }
}
