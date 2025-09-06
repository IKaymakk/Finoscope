using Finoscope.Application.DTOs;
using Finoscope.Application.Interfaces.Repositories;
using Finoscope.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Finoscope.Application.Features.CustomerBalanceTimeline
{
    /// <summary>
    ///  Seçilen Müşterinin Faturalandırılma Durumu ve En Yüksek Borç Tarihi.
    /// </summary>
    public class GetBalanceTimelineQueryHandler : IRequestHandler<GetBalanceTimelineQuery, BalanceTimelineVm?>
    {
        private readonly IAccountingReadRepository _accountingReadRepository;

        public GetBalanceTimelineQueryHandler(IAccountingReadRepository accountingReadRepository)
        {
            _accountingReadRepository = accountingReadRepository;
        }

        public async Task<BalanceTimelineVm?> Handle(GetBalanceTimelineQuery request, CancellationToken cancellationToken)
        {
            // Müşteri bilgisi
            var customerInfo = await _accountingReadRepository.GetCustomerInfoAsync(request.customerId);
            if (customerInfo == null)
            {
                return null;
            }

            // Faturaları alıyoruz
            var invoices = await _accountingReadRepository.GetCustomerInvoicesAsync(request.customerId);

            if (invoices == null || !invoices.Any())
            {
                return CreateEmptyResponse(customerInfo);
            }

            var balancePoints = CalculateBalanceTimeline(invoices, request.StartDate, request.EndDate);
            var maxDebtPoint = FindMaxDebtPoint(balancePoints, customerInfo);

            return CreateResponse(customerInfo, balancePoints, maxDebtPoint);
        }

        /// <summary>
        /// Tüm fatura olaylarını kullanarak borç seyrini hesapla ve filtrelenmiş listeyi döndür
        /// </summary>
        private List<BalancePointDto> CalculateBalanceTimeline(
            List<Fatura> invoices,
            DateTime? startDate,
            DateTime? endDate)
        {
            // Tüm olayları (fatura/ödeme) oluştur ve günlük net değişimleri grupla
            var allDailyChanges = CreateBalanceEvents(invoices)
                .GroupBy(e => DateOnly.FromDateTime(e.Date))
                .OrderBy(g => g.Key)
                .Select(g => new BalancePointDto
                {
                    Date = g.Key,
                    DailyChange = g.Sum(x => x.Amount)
                })
                .ToList();

            // Başlangıç tarihi öncesindeki total durumu hesapla
            var start = startDate?.Date;
            decimal runningBalance = 0m;
            if (start.HasValue)
            {
                runningBalance = allDailyChanges
                    .Where(p => p.Date.ToDateTime(TimeOnly.MinValue).Date < start.Value)
                    .Sum(p => p.DailyChange);
            }

            // Filtrelenmiş tarihler için running balance'ı uygula
            var filteredPoints = allDailyChanges
                .Where(p =>
                    (!startDate.HasValue || p.Date.ToDateTime(TimeOnly.MinValue).Date >= startDate.Value.Date) &&
                    (!endDate.HasValue || p.Date.ToDateTime(TimeOnly.MinValue).Date <= endDate.Value.Date))
                .ToList();

            var result = new List<BalancePointDto>();
            foreach (var p in filteredPoints)
            {
                runningBalance += p.DailyChange;
                result.Add(new BalancePointDto
                {
                    Date = p.Date,
                    DailyChange = p.DailyChange,
                    EndOfDayBalance = runningBalance
                });
            }

            return result;
        }

        /// <summary>
        /// Borç seyrinden en yüksek borç anını bulur.
        /// </summary>
        private MaxDebtResultDto? FindMaxDebtPoint(List<BalancePointDto> balancePoints, Musteri customerInfo)
        {
            if (!balancePoints.Any())
                return null;

            var maxPoint = balancePoints
                .OrderByDescending(p => p.EndOfDayBalance)
                .ThenBy(p => p.Date)
                .FirstOrDefault();

            if (maxPoint == null)
                return null;

            return new MaxDebtResultDto
            {
                MusteriId = customerInfo.Id,
                Unvan = customerInfo.Unvan,
                Date = maxPoint.Date.ToDateTime(TimeOnly.MinValue),
                Balance = maxPoint.EndOfDayBalance
            };
        }

        /// <summary>
        /// Faturaları borç ve ödeme eventlerine dönüştürür.
        /// </summary>
        private static List<(DateTime Date, decimal Amount)> CreateBalanceEvents(IEnumerable<Fatura> invoices)
        {
            var events = new List<(DateTime Date, decimal Amount)>();

            foreach (var invoice in invoices)
            {
                if (invoice.FaturaTarihi.HasValue)
                {
                    events.Add((invoice.FaturaTarihi.Value, invoice.FaturaTutari ?? 0m));
                }

                if (invoice.OdemeTarihi.HasValue)
                {
                    events.Add((invoice.OdemeTarihi.Value, -(invoice.FaturaTutari ?? 0m)));
                }
            }
            return events;
        }


        /// <summary>
        /// Başarılı bir response nesnesi oluşturur
        /// </summary>
        private static BalanceTimelineVm CreateResponse(
            Musteri customerInfo,
            List<BalancePointDto> balancePoints,
            MaxDebtResultDto? maxDebtPoint)
        {
            return new BalanceTimelineVm
            {
                CustomerId = customerInfo.Id,
                Unvan = customerInfo.Unvan,
                Points = balancePoints,
                MaxDebt = maxDebtPoint
            };
        }

        /// <summary>
        /// Boş bir response nesnesi oluşturur
        /// </summary>
        private static BalanceTimelineVm CreateEmptyResponse(Musteri customerInfo)
        {
            return new BalanceTimelineVm
            {
                CustomerId = customerInfo.Id,
                Unvan = customerInfo.Unvan,
                Points = new List<BalancePointDto>(),
                MaxDebt = null
            };
        }
    }
}
