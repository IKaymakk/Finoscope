using Finoscope.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finoscope.Application.Features.CustomerBalanceTimeline
{
    /// <summary>
    /// Belirli bir müşterinin borç seyrini ve maksimum borç anını getirmek için kullanılan sorgu.
    /// </summary>
    public record GetBalanceTimelineQuery(int customerId, DateTime? StartDate = null, DateTime? EndDate = null)
      : IRequest<BalanceTimelineVm?>;
}
