using System;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Contracts.Core.Events;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.PricingModule.Core.Services;

namespace VirtoCommerce.Contracts.Data.Handlers;
public class UpdateContractHandler(IPricelistAssignmentService pricelistAssignmentService) : IEventHandler<ContractChangedEvent>
{
    public async Task Handle(ContractChangedEvent message)
    {
        var contracts = message.ChangedEntries
                .Where(x => IsPriceListShouldBeUpdated(x))
                .Select(x => x.NewEntry)
                .ToList();

        foreach (var contract in contracts)
        {
            await UpdatePricelistAssignmentDates(contract.BasePricelistAssignmentId, contract.StartDate, contract.EndDate);
            await UpdatePricelistAssignmentDates(contract.PriorityPricelistAssignmentId, contract.StartDate, contract.EndDate);
        }
    }

    private bool IsPriceListShouldBeUpdated(GenericChangedEntry<Contract> entry)
    {
        var newEntry = entry.NewEntry;
        var oldEntry = entry.OldEntry;

        return entry.EntryState == EntryState.Modified &&
               (newEntry.StartDate != oldEntry.StartDate || newEntry.EndDate != oldEntry.EndDate) &&
               newEntry.BasePricelistAssignmentId.IsNullOrEmpty() &&
               newEntry.PriorityPricelistAssignmentId.IsNullOrEmpty();
    }

    private async Task UpdatePricelistAssignmentDates(string pricelistAssignmentId, DateTime? startDate, DateTime? endDate)
    {
        var priceListAssignment = await pricelistAssignmentService.GetByIdAsync(pricelistAssignmentId);

        if (priceListAssignment != null)
        {
            priceListAssignment.StartDate = startDate;
            priceListAssignment.EndDate = endDate;
            await pricelistAssignmentService.SaveChangesAsync([priceListAssignment]);
        }
    }
}
