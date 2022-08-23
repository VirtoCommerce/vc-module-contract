using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.Contract.Core;
using VirtoCommerce.Contract.Core.Models.Search;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.Contract.Web.Controllers.Api
{
    [Route("api/contract")]
    public class ContractController : Controller
    {
        private readonly ICrudService<Core.Models.Contract> _contractService;
        private readonly ISearchService<ContractSearchCriteria, ContractSearchResult, Core.Models.Contract> _contractSearchService;

        public ContractController(
            ICrudService<Core.Models.Contract> contractService,
            ISearchService<ContractSearchCriteria, ContractSearchResult, Core.Models.Contract> contractSearchService)
        {
            _contractService = contractService;
            _contractSearchService = contractSearchService;
        }

        [HttpGet]
        [Route("{id}")]
        [Authorize(ModuleConstants.Security.Permissions.Access)]
        public async Task<ActionResult<Core.Models.Contract>> GetContractById(string id)
        {
            var contract = await _contractService.GetByIdAsync(id);
            return Ok(contract);
        }

        [HttpPost]
        [Route("search")]
        [Authorize(ModuleConstants.Security.Permissions.Read)]
        public async Task<ActionResult<ContractSearchResult>> SearchContracts([FromBody] ContractSearchCriteria searchCriteria)
        {
            var searchResult = await _contractSearchService.SearchAsync(searchCriteria);
            return Ok(searchResult);
        }

        [HttpPost]
        [Route("")]
        [Authorize(ModuleConstants.Security.Permissions.Create)]
        public async Task<ActionResult<Core.Models.Contract>> CreateContract([FromBody] Core.Models.Contract contract)
        {
            await _contractService.SaveChangesAsync(new[] { contract });
            return Ok(contract);
        }

        [HttpPut]
        [Route("")]
        [Authorize(ModuleConstants.Security.Permissions.Update)]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        public async Task<ActionResult> UpdateContract([FromBody] Core.Models.Contract contract)
        {
            await _contractService.SaveChangesAsync(new[] { contract });
            return NoContent();
        }

        [HttpDelete]
        [Route("")]
        [Authorize(ModuleConstants.Security.Permissions.Delete)]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        public async Task<ActionResult> DeleteContract([FromQuery] string[] ids)
        {
            await _contractService.DeleteAsync(ids);
            return NoContent();
        }
    }
}
