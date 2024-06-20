using GraphQL.Types;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.Xapi.Core.Services;

namespace VirtoCommerce.Contracts.ExperienceApi.Schemas
{
    public class ContractType : ExtendableGraphType<Contract>
    {
        public ContractType(IDynamicPropertyResolverService dynamicPropertyResolverService)
        {
            Field(x => x.Id, nullable: false);
            Field(x => x.Name, nullable: false);
            Field(x => x.Code, nullable: false);
            Field(x => x.Description, nullable: true);
            Field(x => x.VendorId, nullable: true);
            Field(x => x.StoreId, nullable: true);
            Field(x => x.Status, nullable: true);
            Field(x => x.StartDate, nullable: true);
            Field(x => x.EndDate, nullable: true);

            ExtendableField<ListGraphType<DynamicPropertyValueType>>(
                nameof(Contract.DynamicProperties),
                "Contract dynamic property values",
                QueryArgumentPresets.GetArgumentForDynamicProperties(),
                context => dynamicPropertyResolverService.LoadDynamicPropertyValues(context.Source, context.GetArgumentOrValue<string>("cultureName")));
        }
    }
}
