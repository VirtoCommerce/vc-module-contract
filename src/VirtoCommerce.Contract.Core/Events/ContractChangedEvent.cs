using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Events;

namespace VirtoCommerce.Contract.Core.Events
{
    public class ContractChangedEvent : GenericChangedEntryEvent<Models.Contract>
    {
        public ContractChangedEvent(IEnumerable<GenericChangedEntry<Models.Contract>> changedEntries)
            : base(changedEntries)
        {
        }
    }
}
