using VirtoCommerce.Contracts.Core.Models.Search;
using VirtoCommerce.Contracts.Core.Services;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;

namespace VirtoCommerce.Contracts.ExperienceApi.Queries
{
    public class ContractsQueryHandler : IQueryHandler<ContractsQuery, ContractSearchResult>
    {
        private readonly IContractSearchService _contractSearchService;

        public ContractsQueryHandler(IContractSearchService contractSearchService)
        {
            _contractSearchService = contractSearchService;
        }

        public virtual async Task<ContractSearchResult> Handle(ContractsQuery request, CancellationToken cancellationToken)
        {
            var result = await _contractSearchService.SearchAsync(GetSearchCriteria(request));

            return result;
        }

        protected virtual ContractSearchCriteria GetSearchCriteria(ContractsQuery request)
        {
            var criteria = request.GetSearchCriteria<ContractSearchCriteria>();
            criteria.StoreId = request.StoreId;
            criteria.VendorId = request.VendorId;
            criteria.Codes = request.Codes;
            criteria.Statuses = request.Statuses;
            criteria.ActiveStartDate = request.StartDate;
            criteria.ActiveEndDate = request.EndDate;

            return criteria;
        }
    }
}
