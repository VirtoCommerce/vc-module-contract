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

    private static bool IsPriceListShouldBeUpdated(GenericChangedEntry<Contract> entry)
    {
        return entry.EntryState == EntryState.Modified &&
               (entry.NewEntry.StartDate != entry.OldEntry.StartDate || entry.NewEntry.EndDate != entry.OldEntry.EndDate) &&
               !string.IsNullOrWhiteSpace(entry.NewEntry.BasePricelistAssignmentId) &&
               !string.IsNullOrWhiteSpace(entry.NewEntry.PriorityPricelistAssignmentId);
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
