using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Core.Models.Search;
using VirtoCommerce.Contracts.Core.Services;
using VirtoCommerce.CoreModule.Core.Conditions;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.PricingModule.Core.Model;
using VirtoCommerce.PricingModule.Core.Model.Conditions;
using VirtoCommerce.PricingModule.Core.Model.Search;
using VirtoCommerce.PricingModule.Core.Services;

namespace VirtoCommerce.Contracts.Data.Services
{
    public sealed class ContractPricesService : IContractPricesService
    {
        private const int _maxPriority = 10000;

        private readonly ICrudService<Contract> _contractService;
        private readonly ICrudService<Price> _priceService;
        private readonly ICrudService<Pricelist> _pricelistService;
        private readonly ICrudService<PricelistAssignment> _pricelistAssignmentService;
        private readonly IMergedPriceSearchService _mergedPriceSearchService;

        public ContractPricesService(ICrudService<Contract> contractService,
            ICrudService<Price> priceService,
            ICrudService<Pricelist> pricelistService,
            ICrudService<PricelistAssignment> pricelistAssignmentService,
            IMergedPriceSearchService mergedPriceSearchService)
        {
            _contractService = contractService;
            _priceService = priceService;
            _pricelistService = pricelistService;
            _pricelistAssignmentService = pricelistAssignmentService;
            _mergedPriceSearchService = mergedPriceSearchService;
        }

        public async Task<Contract> LinkPricelist(ContractPricelist request)
        {
            var contract = await _contractService.GetByIdAsync(request.ContractId);
            var basePricelist = await _pricelistService.GetByIdAsync(request.PricelistId, PriceListResponseGroup.NoDetails.ToString());

            if (contract == null || basePricelist == null)
            {
                return null;
            }

            // empty price list for modified prices
            var priorityPriceList = new Pricelist()
            {
                Name = $"Contract-{contract.Name}-{basePricelist.Name}",
                Currency = basePricelist.Currency,
            };

            await _pricelistService.SaveChangesAsync(new List<Pricelist> { priorityPriceList });

            // assignments
            var basePriceListAssigment = new PricelistAssignment
            {
                Name = $"Contract-{contract.Name}-{basePricelist.Name}-Base",
                StoreId = contract.StoreId,
                PricelistId = basePricelist.Id,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                Priority = _maxPriority,
            };

            var priorityPriceListAssigment = new PricelistAssignment
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

            basePriceListAssigment.DynamicExpression = conditionTree;
            priorityPriceListAssigment.DynamicExpression = conditionTree;

            await _pricelistAssignmentService.SaveChangesAsync(new List<PricelistAssignment> { basePriceListAssigment, priorityPriceListAssigment });

            contract.BasePricelistAssignmentId = basePriceListAssigment.Id;
            contract.PriorityPricelistAssignmentId = priorityPriceListAssigment.Id;

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

        public async Task SaveContractPrice(ContractProductPrices model)
        {
            var pricelists = await GetContractPriceLists(model.ContractId);
            if (pricelists == null)
            {
                return;
            }

            // force change pricelist to priority
            foreach (var price in model.Prices)
            {
                price.PricelistId = pricelists.PriorityPriceListId;
            }

            await _priceService.SaveChangesAsync(model.Prices);
        }

        private async Task<ContractPriceListTuple> GetContractPriceLists(string contractId)
        {
            var contract = await _contractService.GetByIdAsync(contractId);
            if (contract == null)
            {
                return null;
            }

            var pricelistAssignments = await _pricelistAssignmentService.GetAsync(new List<string> { contract.BasePricelistAssignmentId, contract.PriorityPricelistAssignmentId });
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

        private sealed class ContractPriceListTuple
        {
            public string BasePriceListId { get; set; }

            public string PriorityPriceListId { get; set; }
        }
    }
}
