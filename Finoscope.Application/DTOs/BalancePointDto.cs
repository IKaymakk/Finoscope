namespace Finoscope.Application.DTOs
{
    //Gün sonunda oluşan borç durumu
    public sealed class BalancePointDto
    {
        public DateOnly Date { get; init; }
        public decimal EndOfDayBalance { get; init; }
        
        // O günkü net bakiye değişimi
        public decimal DailyChange { get; init; }
    }
}
