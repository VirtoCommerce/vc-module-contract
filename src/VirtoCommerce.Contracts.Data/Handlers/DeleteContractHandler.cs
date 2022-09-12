using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using VirtoCommerce.Contracts.Core.Events;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Core.Models.Search;
using VirtoCommerce.Contracts.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;

namespace VirtoCommerce.Contracts.Data.Handlers
{
    /// <summary>
    /// Delete contract user group from deleted contract members
    /// </summary>
    public sealed class DeleteContractHandler : IEventHandler<ContractChangedEvent>
    {
        private readonly IContractMembersService _contractMembersService;
        private readonly IContractMembersSearchService _contractMembersSearchService;

        public DeleteContractHandler(IContractMembersService contractMembersService, IContractMembersSearchService contractMembersSearchService)
        {
            _contractMembersService = contractMembersService;
            _contractMembersSearchService = contractMembersSearchService;
        }

        public Task Handle(ContractChangedEvent message)
        {
            var contractIds = message.ChangedEntries
                .Where(x => x.EntryState == EntryState.Deleted)
                .Select(x => x.OldEntry.Code)
                .ToList();

            if (contractIds.Any())
            {
                BackgroundJob.Enqueue(() => DeleteAllContractsMembersAsync(contractIds));
            }

            return Task.CompletedTask;
        }

        [DisableConcurrentExecution(10)]
        public async Task DeleteAllContractsMembersAsync(List<string> contractCodes)
        {
            foreach (var contractCode in contractCodes)
            {
                await DeleteContractMembersAsync(contractCode);
            }
        }

        private async Task DeleteContractMembersAsync(string contractCode)
        {
            var countResult = await _contractMembersSearchService.SearchAsync(new ContractMembersSearchCriteria
            {
                ContractCode = contractCode,
            });

            var pageSize = 20;

            for (var i = 0; i < countResult.TotalCount; i += pageSize)
            {
                var searchResult = await _contractMembersSearchService.SearchAsync(new ContractMembersSearchCriteria
                {
                    ContractCode = contractCode,
                    Take = pageSize,
                });

                var memberIds = searchResult.Results.Select(x => x.Id).ToList();

                var relation = new ContractMembers
                {
                    ContractCode = contractCode,
                    MemberIds = memberIds,
                };

                await _contractMembersService.DeleteContractMembers(relation);
            }
        }
    }
}
