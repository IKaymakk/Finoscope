using Finoscope.Application.DTOs;
using Finoscope.Application.Features.CustomerBalanceTimeline;
using Finoscope.Application.Features.GetAllCustomers;
using Finoscope.Application.Features.GetMaxDebtDate;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Finoscope.API.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;
    public CustomersController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Verilen müşteri Id için maksimum borç miktarı ve tarihi. Net Dönüş.
    /// </summary>
    /// <param name="id">Müşteri Id</param>
    /// <param name="start">Opsiyonel başlangıç tarihi</param>
    /// <param name="end">Opsiyonel bitiş tarihi</param>
    /// <returns>MaxDebtResultDto</returns>
    [HttpGet("{id:int}/balances/max")]
    public async Task<IActionResult> GetMaxDebt(int id, [FromQuery] DateTime? start = null, [FromQuery] DateTime? end = null)
    {
        var result = await _mediator.Send(new GetMaxDebtDateQuery(id, start, end));
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Belirli bir müşteri için borç timeline'ını döner.
    /// </summary>
    /// <param name="id">Müşteri Id</param>
    /// <param name="start">Opsiyonel başlangıç tarihi</param>
    /// <param name="end">Opsiyonel bitiş tarihi</param>
    /// <returns>BalanceTimelineVm</returns>
    [HttpGet("{id:int}/balances/timeline")]
    public async Task<IActionResult> GetBalanceTimeline(int id, [FromQuery] DateTime? start = null, [FromQuery] DateTime? end = null)
    {
        var result = await _mediator.Send(new GetBalanceTimelineQuery(id, start, end));
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Tüm müşterilerin listesini döner.
    /// </summary>
    /// <param name="cancellationToken">İptal tokenı.</param>
    /// <returns>Müşteri listesi.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllCustomers(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllCustomersQuery(), cancellationToken);
        return Ok(result);
    }
}
