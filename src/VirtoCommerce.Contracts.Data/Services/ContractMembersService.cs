using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Core.Models.Search;
using VirtoCommerce.Contracts.Core.Services;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Model.Search;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.Contracts.Data.Services
{
    public sealed class ContractMembersService : IContractMembersService, IContractMembersSearchService
    {
        private readonly ICrudService<Contract> _contractService;
        private readonly IMemberService _membersService;
        private readonly IMemberSearchService _memberSearchService;

        public ContractMembersService(
            ICrudService<Contract> contractService,
            IMemberService membersService,
            IMemberSearchService memberSearchService)
        {
            _contractService = contractService;
            _membersService = membersService;
            _memberSearchService = memberSearchService;
        }

        public async Task AddContractMembers(ContractMembers contractMembersRelation)
        {
            var contractCode = await GetContractCode(contractMembersRelation.ContractId, contractMembersRelation.ContractCode);
            if (string.IsNullOrEmpty(contractCode))
            {
                return;
            }

            var members = await _membersService.GetByIdsAsync(contractMembersRelation.MemberIds.ToArray(), MemberResponseGroup.WithGroups.ToString());
            var memberToSave = members.Where(member => !member.Groups.Contains(contractCode)).ToArray();

            foreach (var member in memberToSave)
            {
                member.Groups.Add(contractCode);
            }

            await _membersService.SaveChangesAsync(memberToSave);
        }

        public async Task DeleteContractMembers(ContractMembers contractMembersRelation)
        {
            var contractCode = await GetContractCode(contractMembersRelation.ContractId, contractMembersRelation.ContractCode);
            if (string.IsNullOrEmpty(contractCode))
            {
                return;
            }

            var members = await _membersService.GetByIdsAsync(contractMembersRelation.MemberIds.ToArray(), MemberResponseGroup.WithGroups.ToString());
            var memberToSave = members.Where(member => member.Groups.Contains(contractCode)).ToArray();

            foreach (var member in memberToSave)
            {
                member.Groups.Remove(contractCode);
            }

            await _membersService.SaveChangesAsync(memberToSave);
        }

        public async Task<ContractMembersSearchResult> SearchAsync(ContractMembersSearchCriteria criteria)
        {
            var contractCode = await GetContractCode(criteria.ContractId, criteria.ContractCode);

            var result = new ContractMembersSearchResult();
            if (string.IsNullOrEmpty(contractCode))
            {
                return result;
            }

            var memberSearchCriteria = new MembersSearchCriteria
            {
                Group = contractCode,
                ResponseGroup = MemberResponseGroup.Default.ToString(),
                Skip = criteria.Skip,
                Take = criteria.Take,
                Sort = criteria.Sort,
                DeepSearch = criteria.DeepSearch,
                Keyword = criteria.Keyword,
            };
            var memberSearchResult = await _memberSearchService.SearchMembersAsync(memberSearchCriteria);

            result.TotalCount = memberSearchResult.TotalCount;
            result.Results = memberSearchResult.Results;

            return result;
        }

        private async Task<string> GetContractCode(string id, string code = null)
        {
            if (!string.IsNullOrEmpty(code))
            {
                return code;
            }

            if (!string.IsNullOrEmpty(id))
            {
                var contract = await _contractService.GetByIdAsync(id);
                return contract?.Code;
            }

            return null;
        }
    }
}
