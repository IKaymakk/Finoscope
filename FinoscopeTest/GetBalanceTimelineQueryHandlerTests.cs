using Finoscope.Application.Features.CustomerBalanceTimeline;
using Finoscope.Application.Interfaces.Repositories;
using Finoscope.Domain.Entities;
using Moq;
using Xunit;
using System.Threading;
using System.Threading.Tasks;

namespace Finoscope.Tests.Application.Handlers;

/// <summary>
/// GetBalanceTimelineQueryHandler için unit testleri.
/// </summary>
public class GetBalanceTimelineQueryHandlerTests
{
    /// <summary>
    /// Müşteri bulunamadığında handler'ın null döndürmesi gerekir.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnNull_WhenCustomerDoesNotExist()
    {
        var repoMock = new Mock<IAccountingReadRepository>();
        repoMock.Setup(r => r.GetCustomerInfoAsync(It.IsAny<int>()))
                .ReturnsAsync((Musteri?)null);

        var handler = new GetBalanceTimelineQueryHandler(repoMock.Object);
        var query = new GetBalanceTimelineQuery(1, null, null);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Null(result);
    }

    /// <summary>
    /// Birden fazla fatura ve ödeme ile zaman çizelgesinin doğru hesaplandığını test eder.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldCalculateCorrectTimeline_WithMultipleInvoicesAndPayments()
    {
        var mockRepo = new Mock<IAccountingReadRepository>();
        var customerId = 1;
        var customerName = "Test Müşteri";
        var customerInfo = new Musteri { Id = customerId, Unvan = customerName };

        // Mock faturalar:
        var invoices = new List<Fatura>
        {
            new Fatura { MusteriId = customerId, FaturaTarihi = new DateTime(2023, 1, 1), FaturaTutari = 1000m },
            new Fatura { MusteriId = customerId, FaturaTarihi = new DateTime(2023, 1, 5), FaturaTutari = 500m },
            new Fatura { MusteriId = customerId, OdemeTarihi = new DateTime(2023, 1, 10), FaturaTutari = 800m },
            new Fatura { MusteriId = customerId, FaturaTarihi = new DateTime(2023, 1, 15), FaturaTutari = 1200m }
        };

        mockRepo.Setup(x => x.GetCustomerInfoAsync(customerId))
                .ReturnsAsync(customerInfo);
        mockRepo.Setup(x => x.GetCustomerInvoicesAsync(customerId))
                .ReturnsAsync(invoices);

        var handler = new GetBalanceTimelineQueryHandler(mockRepo.Object);
        var query = new GetBalanceTimelineQuery(customerId, null, null);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(customerId, result.CustomerId);
        Assert.Equal(customerName, result.Unvan);
        Assert.NotNull(result.Points);
        Assert.Equal(4, result.Points.Count);

        Assert.Equal(new DateOnly(2023, 1, 1), result.Points[0].Date);
        Assert.Equal(1000m, result.Points[0].EndOfDayBalance);

        Assert.Equal(new DateOnly(2023, 1, 5), result.Points[1].Date);
        Assert.Equal(1500m, result.Points[1].EndOfDayBalance);

        Assert.Equal(new DateOnly(2023, 1, 10), result.Points[2].Date);
        Assert.Equal(700m, result.Points[2].EndOfDayBalance);

        Assert.Equal(new DateOnly(2023, 1, 15), result.Points[3].Date);
        Assert.Equal(1900m, result.Points[3].EndOfDayBalance);

        // Maksimum borç
        Assert.NotNull(result.MaxDebt);
        Assert.Equal(new DateTime(2023, 1, 15), result.MaxDebt.Date);
        Assert.Equal(1900m, result.MaxDebt.Balance);
    }

    /// <summary>
    /// Ödenmemiş faturaların da bakiye hesaplamasına dahil edildiğini test eder.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldCalculateCorrectTimeline_WithUnpaidInvoices()
    {
        var mockRepo = new Mock<IAccountingReadRepository>();
        var customerId = 2;
        var customerName = "Ödenmemiş Fatura Müşteri";
        var customerInfo = new Musteri { Id = customerId, Unvan = customerName };

        // Senaryo: 1000 TL ödenmiş + 1800 TL ödenmemiş
        var invoices = new List<Fatura>
        {
            new Fatura
            {
                MusteriId = customerId,
                FaturaTarihi = new DateTime(2023, 10, 1),
                OdemeTarihi = new DateTime(2023, 10, 5),
                FaturaTutari = 1000m
            },
            new Fatura { MusteriId = customerId, FaturaTarihi = new DateTime(2023, 10, 10), FaturaTutari = 500m },
            new Fatura { MusteriId = customerId, FaturaTarihi = new DateTime(2023, 10, 13), FaturaTutari = 1300m }
        };

        mockRepo.Setup(x => x.GetCustomerInfoAsync(customerId))
                .ReturnsAsync(customerInfo);
        mockRepo.Setup(x => x.GetCustomerInvoicesAsync(customerId))
                .ReturnsAsync(invoices);

        var handler = new GetBalanceTimelineQueryHandler(mockRepo.Object);
        var query = new GetBalanceTimelineQuery(customerId, null, null);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(4, result.Points.Count);

        Assert.Equal(new DateOnly(2023, 10, 1), result.Points[0].Date);
        Assert.Equal(1000m, result.Points[0].EndOfDayBalance);

        Assert.Equal(new DateOnly(2023, 10, 5), result.Points[1].Date);
        Assert.Equal(0m, result.Points[1].EndOfDayBalance);

        Assert.Equal(new DateOnly(2023, 10, 10), result.Points[2].Date);
        Assert.Equal(500m, result.Points[2].EndOfDayBalance);

        Assert.Equal(new DateOnly(2023, 10, 13), result.Points[3].Date);
        Assert.Equal(1800m, result.Points[3].EndOfDayBalance);

        Assert.NotNull(result.MaxDebt);
        Assert.Equal(new DateTime(2023, 10, 13), result.MaxDebt.Date);
        Assert.Equal(1800m, result.MaxDebt.Balance);
    }

    /// <summary>
    /// Yalnızca ödeme bilgisi olduğunda bakiye doğru şekilde azalmalı.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldWork_WhenOnlyPaymentExists()
    {
        var mockRepo = new Mock<IAccountingReadRepository>();
        var customerId = 3;
        var customerInfo = new Musteri { Id = customerId, Unvan = "Sadece Ödeme Yapan Müşteri" };

        var invoices = new List<Fatura>
        {
            new Fatura { MusteriId = customerId, OdemeTarihi = new DateTime(2023, 5, 5), FaturaTutari = 500m }
        };

        mockRepo.Setup(x => x.GetCustomerInfoAsync(customerId))
                .ReturnsAsync(customerInfo);
        mockRepo.Setup(x => x.GetCustomerInvoicesAsync(customerId))
                .ReturnsAsync(invoices);

        var handler = new GetBalanceTimelineQueryHandler(mockRepo.Object);
        var query = new GetBalanceTimelineQuery(customerId, null, null);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Points);
        Assert.Equal(new DateOnly(2023, 5, 5), result.Points[0].Date);
        Assert.Equal(-500m, result.Points[0].EndOfDayBalance);
    }

    /// <summary>
    /// Start ve end tarihleri kullanılarak timeline'ın filtrelendiğini test eder.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldFilterTimeline_ByStartAndEndDate_WithWideDateRange()
    {
        var mockRepo = new Mock<IAccountingReadRepository>();
        var customerId = 5;
        var customerName = "Geniş Tarih Aralığı Test Müşteri";
        var customerInfo = new Musteri { Id = customerId, Unvan = customerName };

        // Mock faturalar: tarihler arası geniş fark var
        var invoices = new List<Fatura>
    {
        new Fatura { MusteriId = customerId, FaturaTarihi = new DateTime(2022, 1, 1), FaturaTutari = 1000m },
        new Fatura { MusteriId = customerId, FaturaTarihi = new DateTime(2022, 6, 15), FaturaTutari = 500m },
        new Fatura { MusteriId = customerId, FaturaTarihi = new DateTime(2023, 1, 10), FaturaTutari = 800m },
        new Fatura { MusteriId = customerId, FaturaTarihi = new DateTime(2023, 8, 20), FaturaTutari = 1200m },
        new Fatura { MusteriId = customerId, FaturaTarihi = new DateTime(2024, 3, 5), FaturaTutari = 700m }
    };

        mockRepo.Setup(x => x.GetCustomerInfoAsync(customerId))
                .ReturnsAsync(customerInfo);
        mockRepo.Setup(x => x.GetCustomerInvoicesAsync(customerId))
                .ReturnsAsync(invoices);

        var handler = new GetBalanceTimelineQueryHandler(mockRepo.Object);

        // 2022-06-01 ile 2023-12-31 arası
        var start = new DateTime(2022, 6, 1);
        var end = new DateTime(2023, 12, 31);
        var query = new GetBalanceTimelineQuery(customerId, start, end);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);

        // Tüm noktaların tarihleri filtre aralığında olmalı
        Assert.All(result.Points, p =>
            Assert.True(p.Date.ToDateTime(new TimeOnly()) >= start &&
                        p.Date.ToDateTime(new TimeOnly()) <= end));

        // Beklenen noktalar
        Assert.Equal(3, result.Points.Count);
        Assert.Equal(new DateOnly(2022, 6, 15), result.Points[0].Date);
        Assert.Equal(new DateOnly(2023, 1, 10), result.Points[1].Date);
        Assert.Equal(new DateOnly(2023, 8, 20), result.Points[2].Date);

        // En yüksek borç sadece bu tarih aralığına göre belirlenmeli
        Assert.NotNull(result.MaxDebt);
        Assert.Equal(new DateTime(2023, 8, 20), result.MaxDebt.Date);
        Assert.Equal(3500m, result.MaxDebt.Balance);
    }

}
