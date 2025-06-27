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
    private const int ContractsMaxBatchSize = 25;

    public async Task Handle(ContractChangedEvent message)
    {
        var contracts = message.ChangedEntries
                .Where(x => IsPriceListShouldBeUpdated(x))
                .Select(x => x.NewEntry)
                .ToList();

        var totalCount = contracts.Count;
        var skip = 0;

        while (totalCount > skip)
        {
            var contractsBatch = contracts.Skip(skip).Take(ContractsMaxBatchSize).ToList();

            var priceListAssignmentIds = contractsBatch
                .SelectMany(x => new[] { x.BasePricelistAssignmentId, x.PriorityPricelistAssignmentId })
                .Where(x => !x.IsNullOrEmpty())
                .Distinct()
                .ToList();

            var priceListAssignments = await pricelistAssignmentService.GetAsync(priceListAssignmentIds);

            foreach (var priceListAssignment in priceListAssignments)
            {
                var contract = contractsBatch.FirstOrDefault(x => x.BasePricelistAssignmentId == priceListAssignment.Id || x.PriorityPricelistAssignmentId == priceListAssignment.Id);

                if (contract != null)
                {
                    priceListAssignment.StartDate = contract.StartDate;
                    priceListAssignment.EndDate = contract.EndDate;
                }
            }

            await pricelistAssignmentService.SaveChangesAsync(priceListAssignments);
            skip += ContractsMaxBatchSize;
        }
    }

    private bool IsPriceListShouldBeUpdated(GenericChangedEntry<Contract> entry)
    {
        var newEntry = entry.NewEntry;
        var oldEntry = entry.OldEntry;

        return entry.EntryState == EntryState.Modified &&
               (newEntry.StartDate != oldEntry.StartDate || newEntry.EndDate != oldEntry.EndDate) &&
               !newEntry.BasePricelistAssignmentId.IsNullOrEmpty() &&
               !newEntry.PriorityPricelistAssignmentId.IsNullOrEmpty();
    }
}
