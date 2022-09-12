using System.Collections.Generic;
using VirtoCommerce.PricingModule.Core.Model;

namespace VirtoCommerce.Contracts.Core.Models
{
    public class ContractProductPrices
    {
        public string ContractId { get; set; }

        public IEnumerable<Price> Prices { get; set; } = new List<Price>();
    }
}
