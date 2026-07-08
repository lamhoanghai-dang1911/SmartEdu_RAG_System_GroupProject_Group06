using SmartEdu.Business.Interfaces;
using SmartEdu.Data.Repositories;
using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Entities;
using SmartEdu.Shared.Enums;

namespace SmartEdu.Business.Services;

public class ReportService : IReportService
{
    private readonly IRepository<Order> _orderRepo;
    private readonly IRepository<UsageLog> _usageLogRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Package> _packageRepo;

    public ReportService(
        IRepository<Order> orderRepo,
        IRepository<UsageLog> usageLogRepo,
        IRepository<User> userRepo,
        IRepository<Package> packageRepo)
    {
        _orderRepo = orderRepo;
        _usageLogRepo = usageLogRepo;
        _userRepo = userRepo;
        _packageRepo = packageRepo;
    }

    public async Task<RevenueSummaryDto> GetRevenueAsync(DateTime startUtc, DateTime endUtc)
    {
        var orders = await _orderRepo.GetAllWithIncludeAsync(
            o => o.Status == OrderStatus.Success && o.PaidAt != null && o.PaidAt >= startUtc && o.PaidAt < endUtc,
            o => o.Package
        );

        var ordersList = orders.ToList();

        var items = ordersList
            .GroupBy(o => o.PaidAt!.Value.Date)
            .Select(g => new RevenueReportItem
            {
                Date = g.Key,
                Amount = g.Sum(x => x.Amount),
                OrderCount = g.Count()
            })
            .OrderBy(i => i.Date)
            .ToList();

        var byPackage = ordersList
            .GroupBy(o => o.Package?.Name ?? "Không xác định")
            .Select(g => new PackageRevenueItem
            {
                PackageName = g.Key,
                Amount = g.Sum(x => x.Amount),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        return new RevenueSummaryDto
        {
            Total = items.Sum(i => i.Amount),
            TotalOrders = ordersList.Count,
            Items = items,
            ByPackage = byPackage
        };
    }

    public async Task<UserUsageDto> GetUserUsageAsync(DateTime startUtc, DateTime endUtc)
    {
        var logs = await _usageLogRepo.GetAllWithIncludeAsync(
            l => l.CreatedAt >= startUtc && l.CreatedAt < endUtc,
            l => l.User
        );

        var byUser = logs.GroupBy(l => l.UserId)
            .Select(g => new UserUsageItem
            {
                UserId = g.Key,
                UserName = g.FirstOrDefault()?.User?.FullName ?? ("User " + g.Key),
                PromptTokens = g.Sum(x => x.PromptTokens),
                CompletionTokens = g.Sum(x => x.CompletionTokens),
                TotalTokens = g.Sum(x => x.PromptTokens + x.CompletionTokens),
                ChatTokens = g.Where(x => x.Feature == FeatureType.Chat).Sum(x => x.PromptTokens + x.CompletionTokens),
                ChunkingTokens = g.Where(x => x.Feature == FeatureType.Chunking).Sum(x => x.PromptTokens + x.CompletionTokens)
            })
            .OrderByDescending(x => x.TotalTokens)
            .ToList();

        return new UserUsageDto
        {
            PeriodStart = startUtc,
            PeriodEnd = endUtc,
            Items = byUser
        };
    }

    public async Task<UserUsageDto> GetUserUsageForUserAsync(int userId, DateTime startUtc, DateTime endUtc)
    {
        var logs = await _usageLogRepo.GetAllWithIncludeAsync(
            l => l.CreatedAt >= startUtc && l.CreatedAt < endUtc && l.UserId == userId,
            l => l.User
        );

        var logsList = logs.ToList();
        var totalPrompt = logsList.Sum(x => x.PromptTokens);
        var totalCompletion = logsList.Sum(x => x.CompletionTokens);

        var item = new UserUsageItem
        {
            UserId = userId,
            UserName = logsList.FirstOrDefault()?.User?.FullName ?? $"User {userId}",
            PromptTokens = totalPrompt,
            CompletionTokens = totalCompletion,
            TotalTokens = totalPrompt + totalCompletion,
            ChatTokens = logsList.Where(x => x.Feature == FeatureType.Chat).Sum(x => x.PromptTokens + x.CompletionTokens),
            ChunkingTokens = logsList.Where(x => x.Feature == FeatureType.Chunking).Sum(x => x.PromptTokens + x.CompletionTokens)
        };

        return new UserUsageDto
        {
            PeriodStart = startUtc,
            PeriodEnd = endUtc,
            Items = new List<UserUsageItem> { item }
        };
    }

    public async Task<TokenTimeSeriesDto> GetTokenTimeSeriesAsync(DateTime startUtc, DateTime endUtc, string granularity)
    {
        var logs = await _usageLogRepo.GetAllAsync(l => l.CreatedAt >= startUtc && l.CreatedAt < endUtc);
        var logsList = logs.ToList();

        Func<DateTime, DateTime> bucketFn = granularity switch
        {
            "month" => d => new DateTime(d.Year, d.Month, 1),
            "week" => d => d.Date.AddDays(-(int)d.DayOfWeek), // đầu tuần (Chủ Nhật)
            _ => d => d.Date // "day" mặc định
        };

        var items = logsList
            .GroupBy(l => bucketFn(l.CreatedAt))
            .Select(g => new TokenTimeSeriesItem
            {
                Bucket = g.Key,
                TotalTokens = g.Sum(x => x.PromptTokens + x.CompletionTokens)
            })
            .OrderBy(i => i.Bucket)
            .ToList();

        return new TokenTimeSeriesDto
        {
            Granularity = granularity,
            Items = items
        };
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(DateTime startUtc, DateTime endUtc)
    {
        var orders = await _orderRepo.GetAllAsync(
            o => o.Status == OrderStatus.Success && o.PaidAt != null && o.PaidAt >= startUtc && o.PaidAt < endUtc
        );
        var ordersList = orders.ToList();

        var logs = await _usageLogRepo.GetAllAsync(l => l.CreatedAt >= startUtc && l.CreatedAt < endUtc);
        var logsList = logs.ToList();

        return new DashboardSummaryDto
        {
            TotalRevenue = ordersList.Sum(o => o.Amount),
            TotalOrders = ordersList.Count,
            TotalTokensUsed = logsList.Sum(l => l.PromptTokens + l.CompletionTokens),
            ActiveUserCount = logsList.Select(l => l.UserId).Distinct().Count()
        };
    }

    public async Task<TokenTimeSeriesDto> GetTokenTimeSeriesForUserAsync(int userId, DateTime startUtc, DateTime endUtc, string granularity)
    {
        var logs = await _usageLogRepo.GetAllAsync(
            l => l.UserId == userId && l.CreatedAt >= startUtc && l.CreatedAt < endUtc
        );
        var logsList = logs.ToList();

        Func<DateTime, DateTime> bucketFn = granularity switch
        {
            "month" => d => new DateTime(d.Year, d.Month, 1),
            "week" => d => d.Date.AddDays(-(int)d.DayOfWeek),
            _ => d => d.Date
        };

        var items = logsList
            .GroupBy(l => bucketFn(l.CreatedAt))
            .Select(g => new TokenTimeSeriesItem
            {
                Bucket = g.Key,
                TotalTokens = g.Sum(x => x.PromptTokens + x.CompletionTokens)
            })
            .OrderBy(i => i.Bucket)
            .ToList();

        return new TokenTimeSeriesDto { Granularity = granularity, Items = items };
    }

    public async Task<OrderHistoryDto> GetOrderHistoryForUserAsync(int userId)
    {
        var orders = await _orderRepo.GetAllWithIncludeAsync(
            o => o.UserId == userId,
            o => o.Package
        );

        var items = orders
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderHistoryItem
            {
                OrderId = o.Id,
                PackageName = o.Package?.Name ?? "Không xác định",
                Amount = o.Amount,
                Status = o.Status.ToString(),
                PaidAt = o.PaidAt,
                CreatedAt = o.CreatedAt
            })
            .ToList();

        return new OrderHistoryDto
        {
            Items = items,
            TotalSpent = items.Where(x => x.Status == OrderStatus.Success.ToString()).Sum(x => x.Amount)
        };
    }
}