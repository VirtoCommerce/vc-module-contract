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
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.PricingModule.Core.Model;

namespace VirtoCommerce.Contracts.Data.Handlers
{
    /// <summary>
    /// Delete contract user group from deleted contract members
    /// </summary>
    public sealed class DeleteContractHandler : IEventHandler<ContractChangedEvent>
    {
        private readonly IContractMembersService _contractMembersService;
        private readonly IContractMembersSearchService _contractMembersSearchService;
        private readonly ICrudService<Pricelist> _pricelistService;
        private readonly ICrudService<PricelistAssignment> _pricelistAssignmentService;

        public DeleteContractHandler(IContractMembersService contractMembersService,
            IContractMembersSearchService contractMembersSearchService,
            ICrudService<Pricelist> pricelistService,
            ICrudService<PricelistAssignment> pricelistAssignmentService)
        {
            _contractMembersService = contractMembersService;
            _contractMembersSearchService = contractMembersSearchService;
            _pricelistService = pricelistService;
            _pricelistAssignmentService = pricelistAssignmentService;
        }

        public Task Handle(ContractChangedEvent message)
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

                BackgroundJob.Enqueue(() => DeletePricelistAssigmentsAsync(basePricelistAssignmentIds, priorityPricelistAssignmentIds));
                BackgroundJob.Enqueue(() => DeleteAllContractsMembersAsync(contractCodes));
            }

            return Task.CompletedTask;
        }

        [DisableConcurrentExecution(10)]
        public async Task DeletePricelistAssigmentsAsync(List<string> basePricelistAssignmentIds, List<string> priorityPricelistAssignmentIds)
        {
            // remove base assignments
            var pageSize = 20;

            for (var i = 0; i < basePricelistAssignmentIds.Count; i += pageSize)
            {
                var priceListAssignmentIds = basePricelistAssignmentIds.OrderBy(x => x).Skip(i).Take(pageSize).ToList();

                await _pricelistAssignmentService.DeleteAsync(priceListAssignmentIds);
            }

            // remove priority assignments and remove priority pricelists
            for (var i = 0; i < priorityPricelistAssignmentIds.Count; i += pageSize)
            {
                var priceListAssignmentIds = priorityPricelistAssignmentIds.OrderBy(x => x).Skip(i).Take(pageSize).ToList();
                var priceListAssignment = await _pricelistAssignmentService.GetAsync(priceListAssignmentIds);

                var pricelistIds = priceListAssignment.Select(x => x.PricelistId).ToList();

                await _pricelistAssignmentService.DeleteAsync(priceListAssignmentIds);
                await _pricelistService.DeleteAsync(pricelistIds);
            }
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
