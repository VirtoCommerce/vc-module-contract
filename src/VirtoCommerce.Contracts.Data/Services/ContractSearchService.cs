using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Core.Models.Search;
using VirtoCommerce.Contracts.Core.Services;
using VirtoCommerce.Contracts.Data.Models;
using VirtoCommerce.Contracts.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Data.GenericCrud;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Model.Search;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.Contracts.Data.Services
{
    public class ContractSearchService : SearchService<ContractSearchCriteria, ContractSearchResult, Contract, ContractEntity>, IContractSearchService
    {
        readonly IStoreSearchService _storeSearchService;

        public ContractSearchService(
            Func<IContractRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            IContractService crudService,
            IOptions<CrudOptions> crudOptions,
            IStoreSearchService storeSearchService)
            : base(repositoryFactory, platformMemoryCache, crudService, crudOptions)
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

            if (!string.IsNullOrEmpty(criteria.VendorId))
            {
                query = query.Where(x => x.VendorId == criteria.VendorId);
            }

            if (!string.IsNullOrEmpty(criteria.StoreId))
            {
                query = query.Where(x => x.StoreId == criteria.StoreId);
            }

            if (!criteria.StoreIds.IsNullOrEmpty())
            {
                query = query.Where(x => criteria.StoreIds.Contains(x.StoreId));
            }

            if (!criteria.Codes.IsNullOrEmpty())
            {
                query = query.Where(x => criteria.Codes.Contains(x.Code));
            }

            if (!criteria.Statuses.IsNullOrEmpty())
            {
                query = query.Where(x => criteria.Statuses.Contains(x.Status));
            }

            query = WithOnlyActiveCondition(query, criteria);
            query = WithDateConditions(query, criteria);


            return query;
        }

        /// <summary>
        /// Find store by name and create predicate with Name and Code conditions
        /// </summary>
        private Expression<Func<ContractEntity, bool>> GetKeywordPredicate(string keyword)
        {
            var predicate = PredicateBuilder.False<ContractEntity>();

            predicate = predicate.Or(x => x.Name.Contains(keyword));
            predicate = predicate.Or(x => x.Code.Contains(keyword));

            // find storeIds by keyword and add them to the predicate
            var storeSearchCriteria = AbstractTypeFactory<StoreSearchCriteria>.TryCreateInstance();
            storeSearchCriteria.Keyword = keyword;
            storeSearchCriteria.ResponseGroup = StoreResponseGroup.StoreInfo.ToString();
            storeSearchCriteria.Take = 0;

            var storeCountResult = _storeSearchService.SearchNoCloneAsync(storeSearchCriteria).GetAwaiter().GetResult();

            if (storeCountResult.TotalCount > 0)
            {
                storeSearchCriteria.Take = storeCountResult.TotalCount;
                var storeSearchResult = _storeSearchService.SearchNoCloneAsync(storeSearchCriteria).GetAwaiter().GetResult();

                var storeIds = storeSearchResult.Results.Select(x => x.Id).ToList();
                predicate = predicate.Or(x => storeIds.Contains(x.StoreId));
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

        private static IQueryable<ContractEntity> WithOnlyActiveCondition(IQueryable<ContractEntity> query, ContractSearchCriteria criteria)
        {
            if (criteria.OnlyActive)
            {
                var now = DateTime.UtcNow;
                query = query.Where(x => (x.StartDate == null || now >= x.StartDate) && (x.EndDate == null || x.EndDate >= now));
            }

            return query;
        }

        private static IQueryable<ContractEntity> WithDateConditions(IQueryable<ContractEntity> query, ContractSearchCriteria criteria)
        {
            if (criteria.ActiveStartDate != null)
            {
                query = query.Where(x => x.StartDate >= criteria.ActiveStartDate || x.StartDate == null);
            }

            if (criteria.ActiveEndDate != null)
            {
                query = query.Where(x => x.EndDate <= criteria.ActiveEndDate || x.EndDate == null);
            }

            return query;
        }
    }
}
