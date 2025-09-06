using Finoscope.Application.DTOs;
using Finoscope.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finoscope.Application.Interfaces.Repositories;

public interface IAccountingReadRepository
{
    /// <summary>
    ///  Verilen müşteri için, (isteğe bağlı) start/end aralığında maksimum borç tarihini ve borcu dönüyoruz.
    /// </summary>
    /// <param name="musteriId">Müşteri Id</param>
    /// <param name="start">Opsiyonel başlangıç tarihi</param>
    /// <param name="end">Opsiyonel bitiş tarihi</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>MaxDebtResultDto veya null</returns>
    Task<MaxDebtResultDto?> GetMaxDebtDateAsync(int musteriId, DateTime? start = null, DateTime? end = null, CancellationToken ct = default);

    Task<List<Fatura>> GetCustomerInvoicesAsync(int musteriId);
    Task<List<Musteri>> GetAllCustomersAsync();
    Task<Musteri?> GetCustomerInfoAsync(int musteriId);


   
}
