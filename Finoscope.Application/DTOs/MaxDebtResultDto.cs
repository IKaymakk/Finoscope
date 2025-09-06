using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finoscope.Application.DTOs;


public class MaxDebtResultDto
{
    public int MusteriId { get; init; }
    public string? Unvan { get; init; }
    public DateTime Date { get; init; }
    public decimal Balance { get; init; }        // o tarihteki bakiye
}
