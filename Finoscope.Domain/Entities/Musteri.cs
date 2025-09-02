using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finoscope.Domain.Entities
{
    public class Musteri
    {
        public int Id { get; set; }
        public string? Unvan { get; set; }
        public ICollection<Fatura>? Faturalar { get; set; }
    }
}
