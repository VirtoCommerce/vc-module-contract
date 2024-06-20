using MediatR;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.Contracts.ExperienceApi.Queries
{
    public class ContractQueryHandler : IQueryHandler<ContractQuery, Contract>
    {
        private readonly IContractService _contractService;

        public ContractQueryHandler(IContractService contractService, IMediator mediator)
        {
            _contractService = contractService;
        }

        public virtual Task<Contract> Handle(ContractQuery request, CancellationToken cancellationToken)
        {
            return _contractService.GetByIdAsync(request.Id);
        }
    }
}
