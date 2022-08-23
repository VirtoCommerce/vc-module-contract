using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.Contract.Core.Events;
using VirtoCommerce.Contract.Data.Models;
using VirtoCommerce.Contract.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Data.GenericCrud;

namespace VirtoCommerce.Contract.Data.Services
{
    public class ContractService : CrudService<Core.Models.Contract, ContractEntity, ContractChangingEvent, ContractChangedEvent>
    {
        public ContractService(Func<IContractRepository> repositoryFactory, IPlatformMemoryCache platformMemoryCache, IEventPublisher eventPublisher)
            : base(repositoryFactory, platformMemoryCache, eventPublisher)
        {
        }

        protected override Task<IEnumerable<ContractEntity>> LoadEntities(IRepository repository, IEnumerable<string> ids, string responseGroup)
        {
            return ((IContractRepository)repository).GetContractsByIdsAsync(ids);

        }
    }
}
