using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Core.Models.Search;
using VirtoCommerce.Contracts.Core.Services;
using VirtoCommerce.CoreModule.Core.Conditions;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.PricingModule.Core.Model;
using VirtoCommerce.PricingModule.Core.Model.Conditions;
using VirtoCommerce.PricingModule.Core.Model.Search;
using VirtoCommerce.PricingModule.Core.Services;

namespace VirtoCommerce.Contracts.Data.Services
{
    public sealed class ContractPricesService : IContractPricesService
    {
        private const int _maxPriority = 10000;

        private readonly IContractService _contractService;
        private readonly IPriceService _priceService;
        private readonly IPricelistService _pricelistService;
        private readonly IPricelistAssignmentService _pricelistAssignmentService;
        private readonly IMergedPriceSearchService _mergedPriceSearchService;

        public ContractPricesService(
            IContractService contractService,
            IPriceService priceService,
            IPricelistService pricelistService,
            IPricelistAssignmentService pricelistAssignmentService,
            IMergedPriceSearchService mergedPriceSearchService)
        {
            _contractService = contractService;
            _priceService = priceService;
            _pricelistService = pricelistService;
            _pricelistAssignmentService = pricelistAssignmentService;
            _mergedPriceSearchService = mergedPriceSearchService;
        }

        public async Task<Contract> LinkPricelist(ContractPricelist contractPricelist)
        {
            var contract = await _contractService.GetByIdAsync(contractPricelist.ContractId);
            var basePricelist = await _pricelistService.GetNoCloneAsync(contractPricelist.PricelistId, PriceListResponseGroup.NoDetails.ToString());

            if (contract == null || basePricelist == null)
            {
                return null;
            }

            // empty price list for modified prices
            var priorityPriceList = new Pricelist
            {
                Name = $"Contract-{contract.Name}-{basePricelist.Name}",
                Currency = basePricelist.Currency,
            };

            await _pricelistService.SaveChangesAsync(new List<Pricelist> { priorityPriceList });

            // assignments
            var basePricelistAssignment = new PricelistAssignment
            {
                Name = $"Contract-{contract.Name}-{basePricelist.Name}-Base",
                StoreId = contract.StoreId,
                PricelistId = basePricelist.Id,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                Priority = _maxPriority,
            };

            var priorityPricelistAssignment = new PricelistAssignment
            {
                Name = $"{priorityPriceList.Name}-Priority",
                StoreId = contract.StoreId,
                PricelistId = priorityPriceList.Id,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                Priority = _maxPriority + 1,
            };

            // special condition
            var conditionTree = AbstractTypeFactory<PriceConditionTree>.TryCreateInstance();
            conditionTree.All = true;

            var blockPricingCondition = AbstractTypeFactory<BlockPricingCondition>.TryCreateInstance();

            var userGroupsCondition = AbstractTypeFactory<UserGroupsContainsCondition>.TryCreateInstance();
            userGroupsCondition.Group = contract.Code;

            blockPricingCondition.Children.Add(userGroupsCondition);
            conditionTree.Children.Add(blockPricingCondition);

            basePricelistAssignment.DynamicExpression = conditionTree;
            priorityPricelistAssignment.DynamicExpression = conditionTree;

            await _pricelistAssignmentService.SaveChangesAsync(new List<PricelistAssignment> { basePricelistAssignment, priorityPricelistAssignment });

            contract.BasePricelistAssignmentId = basePricelistAssignment.Id;
            contract.PriorityPricelistAssignmentId = priorityPricelistAssignment.Id;

            await _contractService.SaveChangesAsync(new List<Contract> { contract });

            return contract;
        }

        public async Task<ContractPricesSearchResult> SearchContractGroupedPrices(ContractPricesSearchCriteria searchCriteria)
        {
            var result = new ContractPricesSearchResult();

            var pricelists = await GetContractPriceLists(searchCriteria.ContractId);
            if (pricelists == null)
            {
                return result;
            }

            var criteria = new MergedPriceSearchCriteria
            {
                Keyword = searchCriteria.Keyword,
                BasePriceListId = pricelists.BasePriceListId,
                PriorityPriceListId = pricelists.PriorityPriceListId,
                ProductIds = searchCriteria.ProductIds,
                Skip = searchCriteria.Skip,
                Take = searchCriteria.Take,
            };

            var searchResult = await _mergedPriceSearchService.SearchGroupsAsync(criteria);
            result.Results = searchResult.Results;
            result.TotalCount = searchResult.TotalCount;

            return result;
        }

        public async Task<IEnumerable<MergedPrice>> GetContractProductPrices(ContractProduct contractProduct)
        {
            var pricelists = await GetContractPriceLists(contractProduct.ContractId);
            if (pricelists == null)
            {
                return new List<MergedPrice>();
            }

            var criteria = new MergedPriceSearchCriteria
            {
                All = true,
                BasePriceListId = pricelists.BasePriceListId,
                PriorityPriceListId = pricelists.PriorityPriceListId,
                ProductIds = new List<string> { contractProduct.ProductId },
            };

            var result = await _mergedPriceSearchService.SearchGroupPricesAsync(criteria);

            return result.Results;
        }

        public async Task SaveContractPrice(ContractProductPrices contractProductPrices)
        {
            var pricelists = await GetContractPriceLists(contractProductPrices.ContractId);
            if (pricelists == null)
            {
                return;
            }

            // force change pricelist to priority
            foreach (var price in contractProductPrices.Prices)
            {
                price.PricelistId = pricelists.PriorityPriceListId;
            }

            await _priceService.SaveChangesAsync(contractProductPrices.Prices);
        }

        private async Task<ContractPriceListTuple> GetContractPriceLists(string contractId)
        {
            var contract = await _contractService.GetNoCloneAsync(contractId);
            if (contract == null)
            {
                return null;
            }

            var pricelistAssignments = await _pricelistAssignmentService.GetNoCloneAsync(new[] { contract.BasePricelistAssignmentId, contract.PriorityPricelistAssignmentId });
            var basePriceListId = pricelistAssignments.FirstOrDefault(x => x.Id == contract.BasePricelistAssignmentId)?.PricelistId;
            var priorityPriceListId = pricelistAssignments.FirstOrDefault(x => x.Id == contract.PriorityPricelistAssignmentId)?.PricelistId;

            if (string.IsNullOrEmpty(basePriceListId) || string.IsNullOrEmpty(priorityPriceListId))
            {
                return null;
            }

            var result = new ContractPriceListTuple
            {
                BasePriceListId = basePriceListId,
                PriorityPriceListId = priorityPriceListId,
            };

            return result;
        }

        public async Task RestoreContractPrices(RestoreContractProductPrices contractProductPrices)
        {
            var pricelists = await GetContractPriceLists(contractProductPrices.ContractId);
            if (pricelists == null)
            {
                return;
            }

            var criteria = new MergedPriceSearchCriteria
            {
                All = true,
                BasePriceListId = pricelists.BasePriceListId,
                PriorityPriceListId = pricelists.PriorityPriceListId,
                ProductIds = contractProductPrices.ProductIds,
            };

            var result = await _mergedPriceSearchService.SearchGroupPricesAsync(criteria);

            // filter out Base prices if any
            var changedPriceIds = result.Results.Where(x => x.State != MergedPriceState.Base).Select(x => x.Id);

            if (!contractProductPrices.PriceIds.IsNullOrEmpty())
            {
                changedPriceIds = changedPriceIds.Intersect(contractProductPrices.PriceIds);
            }

            await _priceService.DeleteAsync(changedPriceIds.ToList());
        }

        private sealed class ContractPriceListTuple
        {
            public string BasePriceListId { get; init; }

            public string PriorityPriceListId { get; init; }
        }
    }
}
