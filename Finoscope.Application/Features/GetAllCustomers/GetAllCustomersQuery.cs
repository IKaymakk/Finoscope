using Finoscope.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finoscope.Application.Features.GetAllCustomers;

public class GetAllCustomersQuery: IRequest<IList<CustomerDto>>
{
}
