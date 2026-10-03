using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Contracts.Core.Events;
using VirtoCommerce.Contracts.Data.BackgroundJobs;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Jobs;

namespace VirtoCommerce.Contracts.Data.Handlers
{
    /// <summary>
    /// Delete contract user group from deleted contract members
    /// </summary>
    public sealed class DeleteContractHandler : IEventHandler<ContractChangedEvent>
    {
        public async Task Handle(ContractChangedEvent message)
        {
            var contracts = message.ChangedEntries
                .Where(x => x.EntryState == EntryState.Deleted)
                .Select(x => x.OldEntry)
                .ToList();

            if (contracts.Any())
            {
                var basePricelistAssignmentIds = new List<string>();
                var priorityPricelistAssignmentIds = new List<string>();
                var contractCodes = new List<string>();

                foreach (var contract in contracts)
                {
                    basePricelistAssignmentIds.Add(contract.BasePricelistAssignmentId);
                    priorityPricelistAssignmentIds.Add(contract.PriorityPricelistAssignmentId);
                    contractCodes.Add(contract.Code);
                }

                await EnqueueDeletePricelistAssignments(basePricelistAssignmentIds, priorityPricelistAssignmentIds);
                await EnqueueDeleteContractsMembers(contractCodes);
            }
        }

        /// <summary>
        /// Kept for background jobs enqueued by an earlier version, which reference this method by name.
        /// Hands the work to <see cref="DeletePricelistAssignmentsJob"/>; remove this once no such job can still be pending.
        /// </summary>
        // Signature is byte-identical on purpose: Hangfire persists a queued job as type name + method name +
        // parameter types + serialized args, so changing any of them would strand already-queued entries as Failed.
        [Obsolete("Enqueued indirectly by legacy Hangfire jobs only; new work uses DeletePricelistAssignmentsJob.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
        public Task DeletePricelistAssignmentsAsync(List<string> basePricelistAssignmentIds, List<string> priorityPricelistAssignmentIds)
        {
            return EnqueueDeletePricelistAssignments(basePricelistAssignmentIds, priorityPricelistAssignmentIds);
        }

        /// <summary>
        /// Kept for background jobs enqueued by an earlier version, which reference this method by name.
        /// Hands the work to <see cref="DeleteContractsMembersJob"/>; remove this once no such job can still be pending.
        /// </summary>
        [Obsolete("Enqueued indirectly by legacy Hangfire jobs only; new work uses DeleteContractsMembersJob.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
        public Task DeleteAllContractsMembersAsync(List<string> contractCodes)
        {
            return EnqueueDeleteContractsMembers(contractCodes);
        }

        // The static facade, not an injected IBackgroundJob: RegisterEventHandler resolves this handler once from
        // the root provider and holds it for the process lifetime, so it must not capture a Scoped dependency.
        private static Task EnqueueDeletePricelistAssignments(List<string> basePricelistAssignmentIds, List<string> priorityPricelistAssignmentIds)
        {
            var payload = AbstractTypeFactory<DeletePricelistAssignmentsJobPayload>.TryCreateInstance();
            payload.BasePricelistAssignmentIds = basePricelistAssignmentIds;
            payload.PriorityPricelistAssignmentIds = priorityPricelistAssignmentIds;

            return BackgroundJob.Enqueue<DeletePricelistAssignmentsJob>(payload);
        }

        private static Task EnqueueDeleteContractsMembers(List<string> contractCodes)
        {
            var payload = AbstractTypeFactory<DeleteContractsMembersJobPayload>.TryCreateInstance();
            payload.ContractCodes = contractCodes;

            return BackgroundJob.Enqueue<DeleteContractsMembersJob>(payload);
        }
    }
}
