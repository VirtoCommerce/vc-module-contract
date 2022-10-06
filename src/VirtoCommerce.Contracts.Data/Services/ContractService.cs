using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using VirtoCommerce.Contracts.Core.Events;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Data.Extensions;
using VirtoCommerce.Contracts.Data.Models;
using VirtoCommerce.Contracts.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Data.GenericCrud;

namespace VirtoCommerce.Contracts.Data.Services
{
    public class ContractService : CrudService<Contract, ContractEntity, ContractChangingEvent, ContractChangedEvent>
    {
        private readonly AbstractValidator<Contract> _contractValidator;

        public ContractService(
            Func<IContractRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            IEventPublisher eventPublisher,
            AbstractValidator<Contract> contractValidator)
            : base(repositoryFactory, platformMemoryCache, eventPublisher)
        {
            _contractValidator = contractValidator;
        }

        protected override Task<IEnumerable<ContractEntity>> LoadEntities(IRepository repository, IEnumerable<string> ids, string responseGroup)
        {
            return ((IContractRepository)repository).GetContractsByIdsAsync(ids);
        }

        protected override async Task BeforeSaveChanges(IEnumerable<Contract> models)
        {
            foreach (var model in models.Where(x => string.IsNullOrWhiteSpace(x.Code)))
            {
                model.Code = model.Name.ToContractCode();
            }

            foreach (var model in models)
            {
                await _contractValidator.ValidateAndThrowAsync(model);
            }

            await base.BeforeSaveChanges(models);
        }
    }
}
