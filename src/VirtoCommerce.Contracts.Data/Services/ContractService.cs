using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using VirtoCommerce.Contracts.Core.Events;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Core.Services;
using VirtoCommerce.Contracts.Data.Extensions;
using VirtoCommerce.Contracts.Data.Models;
using VirtoCommerce.Contracts.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Data.GenericCrud;

namespace VirtoCommerce.Contracts.Data.Services
{
    public class ContractService : CrudService<Contract, ContractEntity, ContractChangingEvent, ContractChangedEvent>, IContractService
    {
        private readonly Func<IContractRepository> _repositoryFactory;
        private readonly AbstractValidator<Contract> _contractValidator;

        public ContractService(
            Func<IContractRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            IEventPublisher eventPublisher,
            AbstractValidator<Contract> contractValidator)
            : base(repositoryFactory, platformMemoryCache, eventPublisher)
        {
            _repositoryFactory = repositoryFactory;
            _contractValidator = contractValidator;
        }

        protected override Task<IList<ContractEntity>> LoadEntities(IRepository repository, IList<string> ids, string responseGroup)
        {
            return ((IContractRepository)repository).GetContractsByIdsAsync(ids);
        }

        protected override async Task BeforeSaveChanges(IList<Contract> models)
        {
            // override code for existing contracts to prevent code changes via api
            using (var repository = _repositoryFactory())
            {
                var dataExistEntities = await LoadExistingEntities(repository, models);

                foreach (var model in models)
                {
                    var originalEntity = FindExistingEntity(dataExistEntities, model);
                    if (originalEntity != null)
                    {
                        model.Code = originalEntity.Code;
                        model.StoreId = originalEntity.StoreId;
                    }
                }
            }

            // auto generate code for empty contract codes
            foreach (var model in models.Where(x => string.IsNullOrWhiteSpace(x.Code)))
            {
                model.Code = model.Name.ToContractCode();
            }

            // validate contract before save
            foreach (var model in models)
            {
                await _contractValidator.ValidateAndThrowAsync(model);
            }

            await base.BeforeSaveChanges(models);
        }
    }
}
