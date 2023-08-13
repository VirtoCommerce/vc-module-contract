using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.ExperienceApi.Queries;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Model.Search;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ExperienceApiModule.Core;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.Contracts.ExperienceApi.Authorization
{
    public class ContractAuthorizationRequirement : IAuthorizationRequirement
    {
    }

    public class ContractAuthorizationHandler : AuthorizationHandler<ContractAuthorizationRequirement>
    {
        private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;
        private readonly IMemberService _memberService;
        private readonly IMemberSearchService _memberSearchService;

        public ContractAuthorizationHandler(
            Func<UserManager<ApplicationUser>> userManagerFactory,
            IMemberService memberService,
            IMemberSearchService memberSearchService)
        {
            _userManagerFactory = userManagerFactory;
            _memberService = memberService;
            _memberSearchService = memberSearchService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ContractAuthorizationRequirement requirement)
        {
            var result = context.User.IsInRole(PlatformConstants.Security.SystemRoles.Administrator);

            if (!result)
            {
                var currentUserId = GetUserId(context);

                switch (context.Resource)
                {
                    case Contract contract:
                        result = await IsContractAvailable(currentUserId, contract.Code);
                        break;

                    case ContractsQuery query:
                        result = await IsContactInOrganization(currentUserId, query.OrganizationId);
                        var organization = await GetOrganization(query.OrganizationId);
                        result = result && organization != null && !organization.Groups.IsNullOrEmpty();
                        if (result)
                        {
                            query.Codes = organization.Groups;
                        }
                        break;
                }
            }

            if (result)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }

        protected virtual async Task<bool> IsContactInOrganization(string userId, string organizationId)
        {
            var contact = await GetContact(userId);
            return contact != null && contact.Organizations.Contains(organizationId);
        }

        protected virtual async Task<bool> IsContractAvailable(string userId, string contractCode)
        {
            var contact = await GetContact(userId);

            if (contact == null)
            {
                return false;
            }

            var memberSearchCriteria = new MembersSearchCriteria
            {
                Group = contractCode,
                ResponseGroup = MemberResponseGroup.Default.ToString(),
                Skip = 0,
                Take = 20,
                MemberType = nameof(Organization)
            };
            var memberSearchResult = await _memberSearchService.SearchMembersAsync(memberSearchCriteria);

            return contact.Organizations.Intersect(memberSearchResult.Results.Select(x => x.Id)).Any();
        }

        protected virtual async Task<Organization> GetOrganization(string organizationId)
        {
            var organization = await _memberService.GetByIdAsync(organizationId) as Organization;
            return organization;
        }

        protected virtual async Task<Contact> GetContact(string userId)
        {
            Contact contact = null;

            using var userManager = _userManagerFactory();
            var user = await userManager.FindByIdAsync(userId);

            if (!string.IsNullOrEmpty(user?.MemberId))
            {
                contact = await _memberService.GetByIdAsync(user.MemberId) as Contact;
            }

            return contact;
        }

        protected virtual string GetUserId(AuthorizationHandlerContext context)
        {
            return
                context.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                context.User.FindFirstValue("name") ??
                AnonymousUser.UserName;
        }
    }
}
