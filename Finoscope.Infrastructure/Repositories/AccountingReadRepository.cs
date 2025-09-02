using Finoscope.Application.DTOs;
using Finoscope.Application.Interfaces.Repositories;
using Finoscope.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finoscope.Infrastructure.Repositories;

public class AccountingReadRepository : IAccountingReadRepository
{
    private readonly FinoscopeDbContext _context;
    public AccountingReadRepository(FinoscopeDbContext context) => _context = context;

    /// <summary>
    /// Verilen müşteri için maksimum borç tarihini ve bakiyesini hesaplıyoruz
    /// </summary>
    /// <param name="musteriId">Müşteri Id</param>
    /// <param name="start">Opsiyonel başlangıç tarihi</param>
    /// <param name="end">Opsiyonel bitiş tarihi</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>MaxDebtResultDto veya null</returns>
    public async Task<MaxDebtResultDto?> GetMaxDebtDateAsync(int musteriId, DateTime? start = null, DateTime? end = null, CancellationToken ct = default)
    {
        // end belirtilmişse end'e kadar al
        var query = _context.Faturalar
            .AsNoTracking()
            .Where(f => f.MusteriId == musteriId);

        if (end.HasValue)
        {
            var filterEndDate = end.Value.Date;
            query = query.Where(f =>
                (f.FaturaTarihi.HasValue && f.FaturaTarihi.Value.Date <= filterEndDate) ||
                (f.OdemeTarihi.HasValue && f.OdemeTarihi.Value.Date <= filterEndDate)
            );
        }


        var invoices = await query
            .Select(x => new
            {
                FaturaTarihi = x.FaturaTarihi,
                OdemeTarihi = x.OdemeTarihi,
                Tutar = x.FaturaTutari ?? 0m
            })
            .ToListAsync(ct);

        if (!invoices.Any()) return null;

        // 2) event
        var events = invoices
            .SelectMany(i =>
            {
                var list = new List<(DateTime date, decimal delta)>();
                if (i.FaturaTarihi.HasValue) list.Add((i.FaturaTarihi.Value.Date, i.Tutar));
                if (i.OdemeTarihi.HasValue) list.Add((i.OdemeTarihi.Value.Date, -i.Tutar));
                return list;
            })
            .GroupBy(e => e.date)
            .Select(g => new { Date = g.Key, Net = g.Sum(x => x.delta) })
            .OrderBy(x => x.Date)
            .ToList();

        if (!events.Any()) return null;

        // 3) Aralık belirle
        var startDate = (start?.Date) ?? events.First().Date;
        var endDate = (end?.Date) ?? events.Last().Date;

        // 4) Başlangıçtaki (start öncesi) running balance
        decimal running = events.Where(e => e.Date < startDate).Sum(e => e.Net);

        // 5) Aralıktaki her tarih için end-of-day balance hesapla ve maksimumu bul
        DateTime? maxDate = null;
        decimal maxBalance = decimal.MinValue;

        var inRange = events.Where(e => e.Date >= startDate && e.Date <= endDate).ToList();
        foreach (var e in inRange)
        {
            running += e.Net; // gün sonu değişimi 
            if (running > maxBalance || (running == maxBalance && (maxDate == null || e.Date < maxDate)))
            {
                maxBalance = running;
                maxDate = e.Date;
            }
        }

        if (maxDate == null) return null;

        // 6) Müşteri bilgisi
        var cust = await _context.Musteriler
            .AsNoTracking()
            .Where(m => m.Id == musteriId)
            .Select(m => new { m.Id, m.Unvan })
            .FirstOrDefaultAsync(ct);

        return new MaxDebtResultDto
        {
            MusteriId = cust?.Id ?? musteriId,
            Unvan = cust?.Unvan,
            Date = maxDate.Value,
            Balance = maxBalance
        };
    }
}
