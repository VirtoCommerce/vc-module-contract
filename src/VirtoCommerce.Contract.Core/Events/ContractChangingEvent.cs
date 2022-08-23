using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Events;

namespace VirtoCommerce.Contract.Core.Events
{
    public class ContractChangingEvent : GenericChangedEntryEvent<Models.Contract>
    {
        public ContractChangingEvent(IEnumerable<GenericChangedEntry<Models.Contract>> changedEntries)
            : base(changedEntries)
        {
        }
    }
}
