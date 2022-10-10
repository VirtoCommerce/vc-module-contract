using System;
using System.Text.RegularExpressions;
using FluentValidation;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Core.Models.Search;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.Contracts.Data.Validation
{
    public class ContractValidator : AbstractValidator<Contract>
    {
        // Inject search service like a factory to avoid circular dependencies errors
        public ContractValidator(
            Func<ISearchService<ContractSearchCriteria, ContractSearchResult, Contract>> contractSearchServiceFactory)
        {
            RuleFor(association => association.Name).NotNull().NotEmpty().WithMessage("Name cannot be empty");
            RuleFor(association => association.Code).NotNull().NotEmpty().WithMessage("Code cannot be empty")
                .Custom((code, context) =>
                {
                    var rx = new Regex(@"/[^\w_-]/;", RegexOptions.Compiled);
                    if (rx.IsMatch(code))
                    {
                        context.AddFailure($"Code cannot contais special symbols except hyphen and underscore.");
                    }
                });

            RuleFor(x => x).CustomAsync(async (contract, context, token) =>
            {
                if (!contract.IsTransient())
                {
                    return;
                }

                var searchCriteria = new ContractSearchCriteria()
                {
                    Code = contract.Code,
                    Take = 0,
                };

                var contractSearchService = contractSearchServiceFactory();
                var searchResult = await contractSearchService.SearchAsync(searchCriteria);

                if (searchResult.TotalCount > 0)
                {
                    context.AddFailure($"Contract with code \"{contract.Code}\" already exists.");
                }
            });
        }
    }
}
