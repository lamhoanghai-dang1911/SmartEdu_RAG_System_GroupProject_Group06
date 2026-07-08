namespace SmartEdu.Shared.DTOs
{
    public class RevenueReportItem
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public int OrderCount { get; set; }
    }

    public class RevenueSummaryDto
    {
        public decimal Total { get; set; }
        public int TotalOrders { get; set; }
        public List<RevenueReportItem> Items { get; set; } = new();
        public List<PackageRevenueItem> ByPackage { get; set; } = new();
    }

    public class PackageRevenueItem
    {
        public string PackageName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int OrderCount { get; set; }
    }

    public class UserUsageItem
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public int ChatTokens { get; set; }
        public int ChunkingTokens { get; set; }
    }

    public class UserUsageDto
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public List<UserUsageItem> Items { get; set; } = new();
    }

    public class TokenTimeSeriesItem
    {
        public DateTime Bucket { get; set; } // đầu ngày/tuần/tháng tùy granularity
        public int TotalTokens { get; set; }
    }

    public class TokenTimeSeriesDto
    {
        public string Granularity { get; set; } = "day"; // day | week | month
        public List<TokenTimeSeriesItem> Items { get; set; } = new();
    }

    public class DashboardSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalTokensUsed { get; set; }
        public int ActiveUserCount { get; set; }
    }
}