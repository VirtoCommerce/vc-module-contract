using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.Contracts.Core;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Core.Models.Search;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.Contracts.Web.Controllers.Api
{
    [Route("api/contracts")]
    public class ContractController : Controller
    {
        private readonly ICrudService<Contract> _contractService;
        private readonly ISearchService<ContractSearchCriteria, ContractSearchResult, Contract> _contractSearchService;

        public ContractController(
            ICrudService<Contract> contractService,
            ISearchService<ContractSearchCriteria, ContractSearchResult, Contract> contractSearchService)
        {
            _contractService = contractService;
            _contractSearchService = contractSearchService;
        }

        [HttpGet]
        [Route("{id}")]
        [Authorize(ModuleConstants.Security.Permissions.Access)]
        public async Task<ActionResult<Contract>> GetContractById(string id)
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
        public async Task<ActionResult<Contract>> CreateContract([FromBody] Contract contract)
        {
            try
            {
                await _contractService.SaveChangesAsync(new[] { contract });
            }
            catch (ValidationException ex)
            {
                return BadRequest(GetErrorMessage(ex));
            }

            return Ok(contract);
        }

        [HttpPut]
        [Route("")]
        [Authorize(ModuleConstants.Security.Permissions.Update)]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        public async Task<ActionResult> UpdateContract([FromBody] Contract contract)
        {
            try
            {
                await _contractService.SaveChangesAsync(new[] { contract });
            }
            catch (ValidationException ex)
            {
                return BadRequest(GetErrorMessage(ex));
            }

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


        private static dynamic GetErrorMessage(ValidationException ex)
        {
            var message = string.Join(Environment.NewLine, ex.Errors.Select(x => x.ErrorMessage));
            return new { message };
        }
    }
}
