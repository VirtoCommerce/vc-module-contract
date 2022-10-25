using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Core.Models.Search;
using VirtoCommerce.Contracts.Data.Models;
using VirtoCommerce.Contracts.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Data.GenericCrud;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Model.Search;
using PredicateBuilder = VirtoCommerce.Platform.Core.Common.PredicateBuilder;

namespace VirtoCommerce.Contracts.Data.Services
{
    public class ContractSearchService : SearchService<ContractSearchCriteria, ContractSearchResult, Contract, ContractEntity>
    {
        readonly ISearchService<StoreSearchCriteria, StoreSearchResult, Store> _storeSearchService;

        public ContractSearchService(
            Func<IContractRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            ICrudService<Contract> service,
            ISearchService<StoreSearchCriteria, StoreSearchResult, Store> storeSearchService)
            : base(repositoryFactory, platformMemoryCache, service)
        {
            _storeSearchService = storeSearchService;
        }

        protected override IQueryable<ContractEntity> BuildQuery(IRepository repository, ContractSearchCriteria criteria)
        {
            var query = ((IContractRepository)repository).Contracts;

            if (!criteria.ObjectIds.IsNullOrEmpty())
            {
                query = query.Where(x => criteria.ObjectIds.Contains(x.Id));
            }

            // search contract name OR contract code OR store name by an expression
            if (!string.IsNullOrEmpty(criteria.Keyword))
            {
                var predicate = GetKeywordPredicate(criteria.Keyword);
                query = query.Where(predicate);
            }

            if (!string.IsNullOrEmpty(criteria.StoreId))
            {
                query = query.Where(x => x.StoreId == criteria.StoreId);
            }

            if (!string.IsNullOrEmpty(criteria.Code))
            {
                query = query.Where(x => x.Code == criteria.Code);
            }

            if (criteria.OnlyActive)
            {
                var now = DateTime.UtcNow;
                query = query.Where(x => (x.StartDate == null || now >= x.StartDate) && (x.EndDate == null || x.EndDate >= now));
            }

            return query;
        }

        /// <summary>
        /// Find store by name and create predicate with Name and Code conditions
        /// </summary>
        private Expression<Func<ContractEntity, bool>> GetKeywordPredicate(string keyword)
        {
            var predicate = PredicateBuilder.False<ContractEntity>();

            predicate = PredicateBuilder.Or(predicate, x => x.Name.Contains(keyword));
            predicate = PredicateBuilder.Or(predicate, x => x.Code.Contains(keyword));

            // find storeIds by keyword and add them to the predicate
            var storeSearchCriteria = AbstractTypeFactory<StoreSearchCriteria>.TryCreateInstance();
            storeSearchCriteria.Keyword = keyword;
            storeSearchCriteria.ResponseGroup = StoreResponseGroup.StoreInfo.ToString();
            storeSearchCriteria.Take = 0;

            var storeCountResult = _storeSearchService.SearchAsync(storeSearchCriteria).GetAwaiter().GetResult();

            if (storeCountResult.TotalCount > 0)
            {
                storeSearchCriteria.Take = storeCountResult.TotalCount;
                var storeSearchResult = _storeSearchService.SearchAsync(storeSearchCriteria).GetAwaiter().GetResult();

                var storeIds = storeSearchResult.Results.Select(x => x.Id).ToList();
                predicate = PredicateBuilder.Or(predicate, x => storeIds.Contains(x.StoreId));
            }

            return predicate;
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
