using Finoscope.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finoscope.Application.Features.GetMaxDebtDate;

public sealed record GetMaxDebtDateQuery(int MusteriId, DateTime? Start, DateTime? End) : IRequest<MaxDebtResultDto?>;
