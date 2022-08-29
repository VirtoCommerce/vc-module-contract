using System.Collections.Generic;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Platform.Core.Events;

namespace VirtoCommerce.Contracts.Core.Events
{
    public class ContractChangingEvent : GenericChangedEntryEvent<Contract>
    {
        public ContractChangingEvent(IEnumerable<GenericChangedEntry<Contract>> changedEntries)
            : base(changedEntries)
        {
        }
    }
}
