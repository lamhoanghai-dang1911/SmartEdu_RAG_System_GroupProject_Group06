using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEdu.Business.Interfaces;

namespace SmartEdu.Web.Controllers;

[Authorize(Roles = "Student")]
public class StudentReportsController : Controller
{
    private readonly IReportService _reportService;

    public StudentReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }
    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue("UserId")
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out var userId))
            throw new UnauthorizedAccessException("Không xác định được người dùng hiện tại.");

        return userId;
    }

    private (DateTime startUtc, DateTime endUtc) ResolveRange(DateTime? start, DateTime? end)
    {
        var vietnamOffset = TimeSpan.FromHours(7);
        var todayInVietnam = DateTimeOffset.UtcNow.ToOffset(vietnamOffset).Date;
        var startLocal = DateTime.SpecifyKind((start ?? todayInVietnam.AddDays(-6)).Date, DateTimeKind.Unspecified);
        var endLocalExclusive = DateTime.SpecifyKind((end ?? todayInVietnam).Date.AddDays(1), DateTimeKind.Unspecified);

        var startUtc = new DateTimeOffset(startLocal, vietnamOffset).UtcDateTime;
        var endUtc = new DateTimeOffset(endLocalExclusive, vietnamOffset).UtcDateTime;
        return (startUtc, endUtc);
    }

    public IActionResult Index() => View();

    [HttpGet]
    [AllowAnonymous] // tạm thời để test không bị chặn bởi role check, nhớ xoá sau khi debug xong
    public IActionResult WhoAmI()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return Json(new { authenticated = false });

        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        return Json(new { authenticated = true, claims });
    }

    [HttpGet]
    public async Task<IActionResult> UsageJson(DateTime? start, DateTime? end)
    {
        var userId = GetCurrentUserId();
        var (startUtc, endUtc) = ResolveRange(start, end);
        var usage = await _reportService.GetUserUsageForUserAsync(userId, startUtc, endUtc);
        return Json(usage);
    }

    [HttpGet]
    public async Task<IActionResult> TokenSeriesJson(DateTime? start, DateTime? end, string granularity = "day")
    {
        var userId = GetCurrentUserId();
        var (startUtc, endUtc) = ResolveRange(start, end);
        var series = await _reportService.GetTokenTimeSeriesForUserAsync(userId, startUtc, endUtc, granularity);
        return Json(series);
    }

    [HttpGet]
    public async Task<IActionResult> OrdersJson()
    {
        var userId = GetCurrentUserId();
        var history = await _reportService.GetOrderHistoryForUserAsync(userId);
        return Json(history);
    }
}
