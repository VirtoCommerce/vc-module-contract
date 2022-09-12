using System.Collections.Generic;

namespace VirtoCommerce.Contracts.Core.Models
{
    public class ContractMembers
    {
        public string ContractId { get; set; }

        public string ContractCode { get; set; }

        public IEnumerable<string> MemberIds { get; set; }
    }
}
