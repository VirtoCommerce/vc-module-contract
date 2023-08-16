using System;
using System.Text.RegularExpressions;
using FluentValidation;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Core.Models.Search;
using VirtoCommerce.Contracts.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.Contracts.Data.Validation
{
    public class ContractValidator : AbstractValidator<Contract>
    {
        // Inject search service like a factory to avoid circular dependencies errors
        public ContractValidator(
            Func<IContractSearchService> contractSearchServiceFactory)
        {
            RuleFor(association => association.Name).NotNull().NotEmpty().WithMessage("Name cannot be empty");
            RuleFor(association => association.Code).NotNull().NotEmpty().WithMessage("Code cannot be empty")
                .Custom((code, context) =>
                {
                    var rx = new Regex(@"/[^\w_-]/;", RegexOptions.Compiled);
                    if (rx.IsMatch(code))
                    {
                        context.AddFailure("Code cannot contain special symbols except hyphen and underscore.");
                    }
                });

            RuleFor(x => x).CustomAsync(async (contract, context, token) =>
            {
                if (!contract.IsTransient())
                {
                    return;
                }

                var searchCriteria = AbstractTypeFactory<ContractSearchCriteria>.TryCreateInstance();
                searchCriteria.Codes = new[] { contract.Code };
                searchCriteria.Take = 0;

                var contractSearchService = contractSearchServiceFactory();
                var searchResult = await contractSearchService.SearchNoCloneAsync(searchCriteria);

                if (searchResult.TotalCount > 0)
                {
                    context.AddFailure($"Contract with code \"{contract.Code}\" already exists.");
                }
            });
        }
    }
}
