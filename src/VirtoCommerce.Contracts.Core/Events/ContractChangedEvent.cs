using System.Collections.Generic;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Platform.Core.Events;

namespace VirtoCommerce.Contracts.Core.Events
{
    public class ContractChangedEvent : GenericChangedEntryEvent<Contract>
    {
        public ContractChangedEvent(IEnumerable<GenericChangedEntry<Contract>> changedEntries)
            : base(changedEntries)
        {
        }
    }
}
