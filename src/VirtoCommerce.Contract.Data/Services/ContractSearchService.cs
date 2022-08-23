using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Contract.Core.Models.Search;
using VirtoCommerce.Contract.Data.Models;
using VirtoCommerce.Contract.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Data.GenericCrud;

namespace VirtoCommerce.Contract.Data.Services
{
    public class ContractSearchService : SearchService<ContractSearchCriteria, ContractSearchResult, Core.Models.Contract, ContractEntity>
    {
        public ContractSearchService(
            Func<IContractRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            ICrudService<Core.Models.Contract> service)
            : base(repositoryFactory, platformMemoryCache, service)
        {
        }

        protected override IQueryable<ContractEntity> BuildQuery(IRepository repository, ContractSearchCriteria criteria)
        {
            var query = ((IContractRepository)repository).Contracts;

            if (!criteria.ObjectIds.IsNullOrEmpty())
            {
                query = query.Where(x => criteria.ObjectIds.Contains(x.Id));
            }

            if (!string.IsNullOrEmpty(criteria.Keyword))
            {
                query = query.Where(x => x.Name.Contains(criteria.Keyword) || x.Description.Contains(criteria.Keyword));
            }

            if (!string.IsNullOrEmpty(criteria.StoreId))
            {
                query = query.Where(x => x.StoreId == criteria.StoreId);
            }

            if (criteria.OnlyActive)
            {
                var now = DateTime.UtcNow;
                query = query.Where(x => (x.StartDate == null || now >= x.StartDate) && (x.EndDate == null || x.EndDate >= now));
            }

            return query;
        }

        protected override IList<SortInfo> BuildSortExpression(ContractSearchCriteria criteria)
        {
            var sortInfos = criteria.SortInfos;

            if (sortInfos.IsNullOrEmpty())
            {
                sortInfos = new[]
                {
                    new SortInfo
                    {
                        SortColumn = nameof(ContractEntity.Name)
                    }
                };
            }

            return sortInfos;
        }
    }
}
