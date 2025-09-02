using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finoscope.Domain.Entities
{
    public class Fatura
    {
        public int Id { get; set; }
        public int? MusteriId { get; set; }
        public DateTime? FaturaTarihi { get; set; }
        public decimal? FaturaTutari { get; set; }

        /// <summary>
        /// Faturanın ödeme tarihi (null ise henüz ödenmemiş)
        /// </summary>
        public DateTime? OdemeTarihi { get; set; }

        public Musteri? Musteri { get; set; }
    }

}
