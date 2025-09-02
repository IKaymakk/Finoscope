using Finoscope.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finoscope.Application.Interfaces.Repositories;

public interface IAccountingReadRepository
{
    /// <summary>
    ///  Verilen müşteri için, (isteğe bağlı) start/end aralığında maksimum borç tarihini ve bakiyesini dönüyoruz.
    /// </summary>
    /// <param name="musteriId">Müşteri Id</param>
    /// <param name="start">Opsiyonel başlangıç tarihi</param>
    /// <param name="end">Opsiyonel bitiş tarihi</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>MaxDebtResultDto veya null</returns>
    Task<MaxDebtResultDto?> GetMaxDebtDateAsync(int musteriId, DateTime? start = null, DateTime? end = null, CancellationToken ct = default);
}
