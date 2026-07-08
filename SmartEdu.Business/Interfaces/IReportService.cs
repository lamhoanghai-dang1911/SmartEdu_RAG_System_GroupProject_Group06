using SmartEdu.Shared.DTOs;

namespace SmartEdu.Business.Interfaces
{
    public interface IReportService
    {
        Task<RevenueSummaryDto> GetRevenueAsync(DateTime startUtc, DateTime endUtc);
        Task<UserUsageDto> GetUserUsageAsync(DateTime startUtc, DateTime endUtc);
        Task<UserUsageDto> GetUserUsageForUserAsync(int userId, DateTime startUtc, DateTime endUtc);
        Task<TokenTimeSeriesDto> GetTokenTimeSeriesAsync(DateTime startUtc, DateTime endUtc, string granularity);
        Task<DashboardSummaryDto> GetDashboardSummaryAsync(DateTime startUtc, DateTime endUtc);
        Task<TokenTimeSeriesDto> GetTokenTimeSeriesForUserAsync(int userId, DateTime startUtc, DateTime endUtc, string granularity);
        Task<OrderHistoryDto> GetOrderHistoryForUserAsync(int userId);
    }
}
