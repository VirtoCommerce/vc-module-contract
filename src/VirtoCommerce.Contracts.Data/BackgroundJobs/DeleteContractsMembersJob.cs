using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Core.Models.Search;
using VirtoCommerce.Contracts.Core.Services;
using VirtoCommerce.Platform.Core.DistributedLock;
using VirtoCommerce.Platform.Core.Jobs;

namespace VirtoCommerce.Contracts.Data.BackgroundJobs
{
    public class DeleteContractsMembersJobPayload
    {
        public List<string> ContractCodes { get; set; } = [];
    }

    /// <summary>
    /// Removes the contract user group from all members of deleted contracts.
    /// </summary>
    public class DeleteContractsMembersJob(
        IContractMembersService contractMembersService,
        IContractMembersSearchService contractMembersSearchService,
        IDistributedLock distributedLock)
        : IBackgroundJobHandler<DeleteContractsMembersJobPayload>
    {
        // Replaces Hangfire's [DisableConcurrentExecution(10)]: one run at a time across the worker fleet. Waiting up to
        // 10 seconds and then failing (so the engine retries the job) keeps the Hangfire semantics; skipping instead
        // would lose the cleanup, since this is a one-off job rather than a recurring one.
        private const string LockResource = "contracts:job:delete-contract-members";
        private static readonly TimeSpan _lockTimeout = TimeSpan.FromSeconds(10);
        private const int PageSize = 20;

        public virtual Task Execute(DeleteContractsMembersJobPayload payload, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            return distributedLock.ExecuteAsync(LockResource, async _ =>
            {
                foreach (var contractCode in payload.ContractCodes)
                {
                    await DeleteContractMembersAsync(contractCode);
                }
            }, _lockTimeout, cancellationToken);
        }

        protected virtual async Task DeleteContractMembersAsync(string contractCode)
        {
            var countResult = await contractMembersSearchService.SearchAsync(new ContractMembersSearchCriteria
            {
                ContractCode = contractCode,
            });

            for (var i = 0; i < countResult.TotalCount; i += PageSize)
            {
                var searchResult = await contractMembersSearchService.SearchAsync(new ContractMembersSearchCriteria
                {
                    ContractCode = contractCode,
                    Take = PageSize,
                });

                var memberIds = searchResult.Results.Select(x => x.Id).ToList();

                var relation = new ContractMembers
                {
                    ContractCode = contractCode,
                    MemberIds = memberIds,
                };

                await contractMembersService.DeleteContractMembers(relation);
            }
        }
    }
}
