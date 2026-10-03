using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DistributedLock;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.PricingModule.Core.Services;

namespace VirtoCommerce.Contracts.Data.BackgroundJobs
{
    public class DeletePricelistAssignmentsJobPayload
    {
        public List<string> BasePricelistAssignmentIds { get; set; } = [];

        public List<string> PriorityPricelistAssignmentIds { get; set; } = [];
    }

    /// <summary>
    /// Removes the base and priority price list assignments of deleted contracts, and the priority price lists themselves.
    /// </summary>
    public class DeletePricelistAssignmentsJob(
        IPricelistService pricelistService,
        IPricelistAssignmentService pricelistAssignmentService,
        IDistributedLock distributedLock)
        : IBackgroundJobHandler<DeletePricelistAssignmentsJobPayload>
    {
        // Replaces Hangfire's [DisableConcurrentExecution(10)]: one run at a time across the worker fleet. Waiting up to
        // 10 seconds and then failing (so the engine retries the job) keeps the Hangfire semantics; skipping instead
        // would lose the cleanup, since this is a one-off job rather than a recurring one.
        private const string LockResource = "contracts:job:delete-pricelist-assignments";
        private static readonly TimeSpan _lockTimeout = TimeSpan.FromSeconds(10);
        private const int PageSize = 20;

        public virtual Task Execute(DeletePricelistAssignmentsJobPayload payload, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            return distributedLock.ExecuteAsync(LockResource, _ => DeleteAsync(payload), _lockTimeout, cancellationToken);
        }

        protected virtual async Task DeleteAsync(DeletePricelistAssignmentsJobPayload payload)
        {
            // remove base assignments
            for (var i = 0; i < payload.BasePricelistAssignmentIds.Count; i += PageSize)
            {
                var priceListAssignmentIds = payload.BasePricelistAssignmentIds.OrderBy(x => x).Skip(i).Take(PageSize).ToList();

                await pricelistAssignmentService.DeleteAsync(priceListAssignmentIds);
            }

            // remove priority assignments and remove priority pricelists
            for (var i = 0; i < payload.PriorityPricelistAssignmentIds.Count; i += PageSize)
            {
                var priceListAssignmentIds = payload.PriorityPricelistAssignmentIds.OrderBy(x => x).Skip(i).Take(PageSize).ToList();
                var priceListAssignment = await pricelistAssignmentService.GetNoCloneAsync(priceListAssignmentIds);

                var pricelistIds = priceListAssignment.Select(x => x.PricelistId).ToList();

                await pricelistAssignmentService.DeleteAsync(priceListAssignmentIds);
                await pricelistService.DeleteAsync(pricelistIds);
            }
        }
    }
}
