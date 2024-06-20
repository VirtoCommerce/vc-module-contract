using GraphQL;
using GraphQL.Types;
using VirtoCommerce.Contracts.Core.Models.Search;
using VirtoCommerce.Xapi.Core.BaseQueries;

namespace VirtoCommerce.Contracts.ExperienceApi.Queries
{
    public class OrganizationContractsQuery : SearchQuery<ContractSearchResult>
    {
        public string OrganizationId { get; set; }
        public string StoreId { get; set; }
        public string VendorId { get; set; }
        public IList<string> Statuses { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }


        public override IEnumerable<QueryArgument> GetArguments()
        {
            yield return Argument<NonNullGraphType<StringGraphType>>(nameof(OrganizationId));
            yield return Argument<StringGraphType>(nameof(StoreId));
            yield return Argument<StringGraphType>(nameof(VendorId));
            yield return Argument<ListGraphType<StringGraphType>>(nameof(Statuses));
            yield return Argument<DateTimeGraphType>(nameof(StartDate));
            yield return Argument<DateTimeGraphType>(nameof(EndDate));
        }

        public override void Map(IResolveFieldContext context)
        {
            base.Map(context);

            OrganizationId = context.GetArgument<string>(nameof(OrganizationId));
            StoreId = context.GetArgument<string>(nameof(StoreId));
            VendorId = context.GetArgument<string>(nameof(VendorId));
            Statuses = context.GetArgument<IList<string>>(nameof(Statuses));
            StartDate = context.GetArgument<DateTime?>(nameof(StartDate));
            EndDate = context.GetArgument<DateTime?>(nameof(EndDate));
        }
    }
}
