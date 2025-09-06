using Finoscope.Application.DTOs;
using Finoscope.Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finoscope.Application.Features.GetAllCustomers;
    
    /// <summary>
    /// Tüm Müşterileri Getir
    /// </summary>
public class GetAllCustomersQueryHandler : IRequestHandler<GetAllCustomersQuery, IList<CustomerDto>>
{
    private readonly IAccountingReadRepository _repo;

    public GetAllCustomersQueryHandler(IAccountingReadRepository repo)
    {
        _repo = repo;
    }

    public async Task<IList<CustomerDto>> Handle(GetAllCustomersQuery request, CancellationToken cancellationToken)
    {
        var customers = await _repo.GetAllCustomersAsync();

        return customers.Select(x => new CustomerDto
        {
            Id = x.Id,
            Unvan = x.Unvan
        }).ToList();


    }
}
