using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.Contracts.Core;
using VirtoCommerce.Contracts.Core.Models;
using VirtoCommerce.Contracts.Core.Models.Search;
using VirtoCommerce.Contracts.Core.Services;
using VirtoCommerce.PricingModule.Core.Model.Search;

namespace VirtoCommerce.Contracts.Web.Controllers.Api
{
    [Route("api/contracts/prices")]
    public class ContractPricesController : Controller
    {
        private readonly IContractPricesService _contractPricesService;

        public ContractPricesController(IContractPricesService contractPricesService)
        {
            _contractPricesService = contractPricesService;
        }

        [HttpPost]
        [Route("linkpricelist")]
        [Authorize(ModuleConstants.Security.Permissions.Update)]
        public async Task<ActionResult<Contract>> LinkPricelist([FromBody] ContractPricelist request)
        {
            var result = await _contractPricesService.LinkPricelist(request);

            return Ok(result);
        }

        [HttpPost]
        [Route("search")]
        [Authorize(ModuleConstants.Security.Permissions.Read)]
        public async Task<ActionResult<MergedPriceGroupSearchResult>> SearchContractGroupedPrices([FromBody] ContractPricesSearchCriteria searchCriteria)
        {
            var result = await _contractPricesService.SearchContractGroupedPrices(searchCriteria);

            return Ok(result);
        }

        [HttpPost]
        [Route("search/products")]
        [Authorize(ModuleConstants.Security.Permissions.Read)]
        public async Task<ActionResult<MergedPriceSearchResult>> SearchContractProductPrices([FromBody] ContractProduct contractProduct)
        {
            var result = await _contractPricesService.GetContractProductPrices(contractProduct);

            return Ok(result);
        }

        [HttpPost]
        [Route("products")]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        [Authorize(ModuleConstants.Security.Permissions.Update)]
        public async Task<ActionResult<MergedPriceSearchResult>> SaveContractPrice([FromBody] ContractProductPrices model)
        {
            await _contractPricesService.SaveContractPrice(model);

            return NoContent();
        }

        [HttpPost]
        [Route("restore")]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        [Authorize(ModuleConstants.Security.Permissions.Update)]
        public async Task<ActionResult> RestoreContractPrices([FromBody] RestoreContractProductPrices contractProducts)
        {
            await _contractPricesService.RestoreContractPrices(contractProducts);

            return NoContent();
        }
    }
}
