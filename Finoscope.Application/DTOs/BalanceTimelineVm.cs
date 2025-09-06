namespace Finoscope.Application.DTOs
{
    // Müşterinin borç seyrini ve maksimum borç bilgisini içeren ana DTO
    public class BalanceTimelineVm
    {
        public long CustomerId { get; init; }
        public string Unvan { get; init; } 
        public IReadOnlyList<BalancePointDto> Points { get; init; } = Array.Empty<BalancePointDto>();
        public MaxDebtResultDto? MaxDebt { get; init; }
    }
}
