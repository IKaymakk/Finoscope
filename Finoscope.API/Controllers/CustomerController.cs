using Finoscope.Application.Features.GetMaxDebtDate;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Finoscope.API.Controllers
{
    [ApiController]
    [Route("api/customers")]
    public sealed class CustomersController : ControllerBase
    {
        private readonly IMediator _mediator;
        public CustomersController(IMediator mediator) => _mediator = mediator;

        [HttpGet("{id:int}/balances/max")]
        public async Task<IActionResult> GetMaxDebt(int id, [FromQuery] DateTime? start = null, [FromQuery] DateTime? end = null)
        {
            var result = await _mediator.Send(new GetMaxDebtDateQuery(id, start, end));
            if (result == null) return NotFound();
            return Ok(result);
        }
    }

}
