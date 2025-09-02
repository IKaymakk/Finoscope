using Finoscope.Application.DTOs;
using Finoscope.Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finoscope.Application.Features.GetMaxDebtDate;

public class GetMaxDebtDateHandler : IRequestHandler<GetMaxDebtDateQuery, MaxDebtResultDto?>
{
    private readonly IAccountingReadRepository _repo;
    public GetMaxDebtDateHandler(IAccountingReadRepository repo) => _repo = repo;

    public Task<MaxDebtResultDto?> Handle(GetMaxDebtDateQuery request, CancellationToken cancellationToken)
        => _repo.GetMaxDebtDateAsync(request.MusteriId, request.Start, request.End, cancellationToken);
}
