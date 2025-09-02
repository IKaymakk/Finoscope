using Finoscope.Domain.Entities;
using Finoscope.Infrastructure.Context;
using Finoscope.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FinoscopeTest
{
    public class AccountingRepositoryTests
    {
        /// <summary>
        /// Test sırasında gerçek veritabanına dokunmadan repository metodlarını test edebilmek için
        /// </summary>
        private FinoscopeDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<FinoscopeDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new FinoscopeDbContext(options);
        }

        /// <summary>
        /// Tek fatura ve ödenmemiş durum testi
        /// Temel senaryo için
        /// </summary>
        [Fact]
        public async Task MaxDebt_SingleInvoice_Unpaid()
        {
            using var context = GetInMemoryDbContext(nameof(MaxDebt_SingleInvoice_Unpaid));

            // Test müşterisi ve faturası
            context.Musteriler.Add(new Musteri { Id = 1, Unvan = "Test Müşteri" });
            context.Faturalar.Add(new Fatura
            {
                Id = 1,
                MusteriId = 1,
                FaturaTarihi = new DateTime(2023, 01, 01),
                FaturaTutari = 1000,
                OdemeTarihi = null // henüz ödenmemiş
            });
            await context.SaveChangesAsync();

            // Repository 
            var repo = new AccountingReadRepository(context);

            // Maksimum borç tarihi 
            var result = await repo.GetMaxDebtDateAsync(1);

            // Test: Metod null dönmemeli
            Assert.NotNull(result);

            // Test: Müşteri ID doğru dönmeli
            Assert.Equal(1, result.MusteriId);

            // Test: Bakiye doğru hesaplanmalı
            Assert.Equal(1000, result.Balance);

            // Test: Maksimum borç tarihi fatura tarihi ile eşleşmeli
            Assert.Equal(new DateTime(2023, 01, 01), result.Date);
        }

        /// <summary>
        /// Birden fazla fatura ve ödeme içeren senaryo
        /// Mesela gerçek hayattaki karmaşık durumlar
        /// </summary>
        [Fact]
        public async Task MaxDebt_MultipleInvoicesWithPayments()
        {
            using var context = GetInMemoryDbContext(nameof(MaxDebt_MultipleInvoicesWithPayments));

            context.Musteriler.Add(new Musteri { Id = 2, Unvan = "Test Müşteri 2" });
            context.Faturalar.AddRange(
                new Fatura { Id = 1, MusteriId = 2, FaturaTarihi = new DateTime(2023, 01, 01), FaturaTutari = 1000, OdemeTarihi = new DateTime(2023, 01, 05) },
                new Fatura { Id = 2, MusteriId = 2, FaturaTarihi = new DateTime(2023, 01, 03), FaturaTutari = 2000, OdemeTarihi = null },
                new Fatura { Id = 3, MusteriId = 2, FaturaTarihi = new DateTime(2023, 01, 07), FaturaTutari = 500, OdemeTarihi = new DateTime(2023, 01, 08) }
            );
            await context.SaveChangesAsync();

            var repo = new AccountingReadRepository(context);
            var result = await repo.GetMaxDebtDateAsync(2);

            // Max borç 2. fatura sonrası oluşmalı: 2023-01-03, bakiye 3000 çünkü
            Assert.NotNull(result);
            Assert.Equal(2, result.MusteriId);
            Assert.Equal(new DateTime(2023, 01, 03), result.Date);
            Assert.Equal(3000, result.Balance);
        }

        /// <summary>
        /// Aynı gün birden fazla fatura testi
        /// Günlük borç toplamı doğru mu
        /// </summary>
        [Fact]
        public async Task MaxDebt_MultipleInvoicesSameDay()
        {
            using var context = GetInMemoryDbContext(nameof(MaxDebt_MultipleInvoicesSameDay));

            context.Musteriler.Add(new Musteri { Id = 3, Unvan = "Test Müşteri 3" });
            context.Faturalar.AddRange(
                new Fatura { Id = 1, MusteriId = 3, FaturaTarihi = new DateTime(2023, 02, 01), FaturaTutari = 500, OdemeTarihi = null },
                new Fatura { Id = 2, MusteriId = 3, FaturaTarihi = new DateTime(2023, 02, 01), FaturaTutari = 700, OdemeTarihi = null }
            );
            await context.SaveChangesAsync();

            var repo = new AccountingReadRepository(context);
            var result = await repo.GetMaxDebtDateAsync(3);

            // Maksimum borç aynı günün toplamı
            Assert.NotNull(result);
            Assert.Equal(3, result.MusteriId);
            Assert.Equal(new DateTime(2023, 02, 01), result.Date);
            Assert.Equal(1200, result.Balance);
        }

        /// <summary>
        /// Start ve end date parametreleri ile borç aralığı testi
        /// Belirli bir tarih aralığı seçşlen durumlar için
        /// </summary>
        [Fact]
        public async Task MaxDebt_StartEndDateFiltering()
        {
            using var context = GetInMemoryDbContext(nameof(MaxDebt_StartEndDateFiltering));

            context.Musteriler.Add(new Musteri { Id = 4, Unvan = "Test Müşteri 4" });
            context.Faturalar.AddRange(
                new Fatura { Id = 1, MusteriId = 4, FaturaTarihi = new DateTime(2023, 03, 01), FaturaTutari = 100, OdemeTarihi = null },
                new Fatura { Id = 2, MusteriId = 4, FaturaTarihi = new DateTime(2023, 03, 05), FaturaTutari = 200, OdemeTarihi = null },
                new Fatura { Id = 3, MusteriId = 4, FaturaTarihi = new DateTime(2023, 03, 10), FaturaTutari = 300, OdemeTarihi = null }
            );
            await context.SaveChangesAsync();

            var repo = new AccountingReadRepository(context);
            var result = await repo.GetMaxDebtDateAsync(4, start: new DateTime(2023, 03, 02), end: new DateTime(2023, 03, 08));

            // Sadece belirtilen tarih aralığına göre maksimum borç
            Assert.NotNull(result);
            Assert.Equal(new DateTime(2023, 03, 05), result.Date);
            Assert.Equal(300, result.Balance);
        }

        /// <summary>
        /// Hiç fatura olmayan müşteri testi
        /// Metod null dönmeli
        /// </summary>
        [Fact]
        public async Task MaxDebt_NoInvoices_ReturnsNull()
        {
            using var context = GetInMemoryDbContext(nameof(MaxDebt_NoInvoices_ReturnsNull));

            context.Musteriler.Add(new Musteri { Id = 5, Unvan = "Test Müşteri 5" });
            await context.SaveChangesAsync();

            var repo = new AccountingReadRepository(context);
            var result = await repo.GetMaxDebtDateAsync(5);

            Assert.Null(result);
        }
    }
}
