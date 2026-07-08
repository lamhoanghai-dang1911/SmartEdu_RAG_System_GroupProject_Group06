using Microsoft.AspNetCore.Mvc;
using SmartEdu.Business.Interfaces;

namespace SmartEdu.Web.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    public IActionResult Index()
    {
        return View();
    }

    private (DateTime startUtc, DateTime endUtc) ResolveRange(DateTime? start, DateTime? end)
    {
        DateTime startUtc = DateTime.SpecifyKind(start ?? DateTime.UtcNow.Date.AddDays(-6), DateTimeKind.Utc);
        DateTime endUtc = DateTime.SpecifyKind((end ?? DateTime.UtcNow.Date).AddDays(1), DateTimeKind.Utc);
        return (startUtc, endUtc);
    }

    [HttpGet]
    public async Task<IActionResult> RevenueJson(DateTime? start, DateTime? end)
    {
        var (startUtc, endUtc) = ResolveRange(start, end);
        var summary = await _reportService.GetRevenueAsync(startUtc, endUtc);
        return Json(summary);
    }

    [HttpGet]
    public async Task<IActionResult> UsageJson(DateTime? start, DateTime? end)
    {
        var (startUtc, endUtc) = ResolveRange(start, end);
        var usage = await _reportService.GetUserUsageAsync(startUtc, endUtc);
        return Json(usage);
    }

    [HttpGet]
    public async Task<IActionResult> TokenSeriesJson(DateTime? start, DateTime? end, string granularity = "day")
    {
        var (startUtc, endUtc) = ResolveRange(start, end);
        var series = await _reportService.GetTokenTimeSeriesAsync(startUtc, endUtc, granularity);
        return Json(series);
    }

    [HttpGet]
    public async Task<IActionResult> SummaryJson(DateTime? start, DateTime? end)
    {
        var (startUtc, endUtc) = ResolveRange(start, end);
        var summary = await _reportService.GetDashboardSummaryAsync(startUtc, endUtc);
        return Json(summary);
    }
}