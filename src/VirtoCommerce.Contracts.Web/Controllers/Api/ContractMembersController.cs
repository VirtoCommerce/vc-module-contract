using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.Contracts.Core;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Core.Models.Search;
using VirtoCommerce.Contracts.Core.Services;

namespace VirtoCommerce.Contracts.Web.Controllers.Api
{
    [Route("api/contracts-members")]
    public class ContractMembersController : Controller
    {
        private readonly IContractMembersService _contractMembersService;
        private readonly IContractMembersSearchService _contractMembersSearchService;

        public ContractMembersController(IContractMembersService contractMembersService, IContractMembersSearchService contractMembersSearchService)
        {
            _contractMembersService = contractMembersService;
            _contractMembersSearchService = contractMembersSearchService;
        }

        [HttpPost]
        [Route("search")]
        [Authorize(ModuleConstants.Security.Permissions.Read)]
        public async Task<ActionResult<ContractMembersSearchResult>> SearchContractMembers([FromBody] ContractMembersSearchCriteria searchCriteria)
        {
            var result = await _contractMembersSearchService.SearchAsync(searchCriteria);
            return Ok(result);
        }

        [HttpPost]
        [Route("add")]
        [Authorize(ModuleConstants.Security.Permissions.Update)]
        public async Task<ActionResult> AddContractMembers([FromBody] ContractMembers addContractMemberRequest)
        {
            await _contractMembersService.AddContractMembers(addContractMemberRequest);
            return NoContent();
        }

        [HttpPost]
        [Route("delete")]
        [Authorize(ModuleConstants.Security.Permissions.Delete)]
        public async Task<ActionResult> DeleteContractMembers([FromBody] ContractMembers deleteContractMembersRequest)
        {
            await _contractMembersService.DeleteContractMembers(deleteContractMembersRequest);
            return NoContent();
        }
    }
}
