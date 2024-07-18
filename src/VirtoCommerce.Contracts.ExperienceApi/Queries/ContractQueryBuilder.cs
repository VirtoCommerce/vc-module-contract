using GraphQL;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.ExperienceApi.Authorization;
using VirtoCommerce.Contracts.ExperienceApi.Schemas;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;

namespace VirtoCommerce.Contracts.ExperienceApi.Queries
{
    public class ContractQueryBuilder : QueryBuilder<ContractQuery, Contract, ContractType>
    {
        protected override string Name => "contract";

        public ContractQueryBuilder(IMediator mediator, IAuthorizationService authorizationService)
            : base(mediator, authorizationService)
        {
        }

        protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, ContractQuery request)
        {
            await base.BeforeMediatorSend(context, request);
            context.CopyArgumentsToUserContext();
        }

        protected override async Task AfterMediatorSend(IResolveFieldContext<object> context, ContractQuery request, Contract response)
        {
            await base.AfterMediatorSend(context, request, response);
            await Authorize(context, response, new ContractAuthorizationRequirement());
        }
    }
}
