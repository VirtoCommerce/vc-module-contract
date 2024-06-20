using GraphQL;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Core.Models.Search;
using VirtoCommerce.Contracts.ExperienceApi.Authorization;
using VirtoCommerce.Contracts.ExperienceApi.Schemas;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;

namespace VirtoCommerce.Contracts.ExperienceApi.Queries
{
    public class OrganizationContractsQueryBuilder : SearchQueryBuilder<OrganizationContractsQuery, ContractSearchResult, Contract, ContractType>
    {
        protected override string Name => "organizationContracts";

        public OrganizationContractsQueryBuilder(IMediator mediator, IAuthorizationService authorizationService)
            : base(mediator, authorizationService)
        {

        }

        protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, OrganizationContractsQuery request)
        {
            await base.BeforeMediatorSend(context, request);
            await Authorize(context, request, new ContractAuthorizationRequirement());
            context.CopyArgumentsToUserContext();
        }
    }
}
