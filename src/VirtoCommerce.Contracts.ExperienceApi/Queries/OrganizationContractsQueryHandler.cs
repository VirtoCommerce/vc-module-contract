using VirtoCommerce.Contracts.Core.Models.Search;
using VirtoCommerce.Contracts.Core.Services;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.Contracts.ExperienceApi.Queries
{
    public class OrganizationContractsQueryHandler : IQueryHandler<OrganizationContractsQuery, ContractSearchResult>
    {
        private readonly IContractSearchService _contractSearchService;
        private readonly IMemberService _memberService;

        public OrganizationContractsQueryHandler(IContractSearchService contractSearchService, IMemberService memberService)
        {
            _contractSearchService = contractSearchService;
            _memberService = memberService;
        }

        public virtual async Task<ContractSearchResult> Handle(OrganizationContractsQuery request, CancellationToken cancellationToken)
        {
            var result = AbstractTypeFactory<ContractSearchResult>.TryCreateInstance();
            var organization = await GetOrganization(request.OrganizationId);

            if (organization == null || organization.Groups.IsNullOrEmpty())
            {
                return result;
            }

            var criteria = GetSearchCriteria(request);
            criteria.Codes = organization.Groups;
            result = await _contractSearchService.SearchAsync(criteria);

            return result;
        }

        protected virtual ContractSearchCriteria GetSearchCriteria(OrganizationContractsQuery request)
        {
            var criteria = request.GetSearchCriteria<ContractSearchCriteria>();
            criteria.StoreId = request.StoreId;
            criteria.VendorId = request.VendorId;
            criteria.Statuses = request.Statuses;
            criteria.ActiveStartDate = request.StartDate;
            criteria.ActiveEndDate = request.EndDate;

            return criteria;
        }

        protected virtual async Task<Organization> GetOrganization(string organizationId)
        {
            var organization = await _memberService.GetByIdAsync(organizationId) as Organization;
            return organization;
        }
    }
}
