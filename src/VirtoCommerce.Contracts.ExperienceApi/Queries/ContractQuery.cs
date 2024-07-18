using GraphQL;
using GraphQL.Types;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Xapi.Core.BaseQueries;

namespace VirtoCommerce.Contracts.ExperienceApi.Queries
{
    public class ContractQuery : Query<Contract>
    {
        public string Id { get; set; }

        public override IEnumerable<QueryArgument> GetArguments()
        {
            yield return Argument<StringGraphType>(nameof(Id));
        }

        public override void Map(IResolveFieldContext context)
        {
            Id = context.GetArgument<string>(nameof(Id));
        }
    }
}
