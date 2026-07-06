using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEdu.Business.Interfaces;
using SmartEdu.Data.Repositories;
using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Entities;
using System.Text.Json;

namespace SmartEdu.Web.Controllers
{
    [Authorize(Roles = "Student")]
    public class PackageController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IRepository<Package> _packageRepo;
        private readonly IRepository<UserSubscription> _subscriptionRepo;
        private readonly IRepository<Order> _orderRepo;
        private readonly IConfiguration _configuration;

        public PackageController(
            IPaymentService paymentService,
            IRepository<Package> packageRepo,
            IRepository<UserSubscription> subscriptionRepo,
            IRepository<Order> orderRepo,
            IConfiguration configuration)
        {
            _paymentService = paymentService;
            _packageRepo = packageRepo;
            _subscriptionRepo = subscriptionRepo;
            _orderRepo = orderRepo;
            _configuration = configuration;
        }

        // Danh sách gói - không cần Auth (ai cũng xem được)
        [AllowAnonymous]
        public async Task<IActionResult> List()
        {
            var packages = await _packageRepo.GetAllAsync(p => p.IsActive && !p.IsDeleted);
            var dtos = packages.Select(p => new PackageDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                DurationDays = p.DurationDays,
                TokenQuota = p.TokenQuota,
                IsActive = p.IsActive
            }).ToList();

            return View(dtos);
        }

        // Trang mua gói
        [HttpGet]
        public async Task<IActionResult> Buy(int packageId)
        {
            var package = await _packageRepo.GetByIdAsync(packageId);
            if (package == null || !package.IsActive)
                return NotFound("Gói dịch vụ không tồn tại.");

            var dto = new PackageDto
            {
                Id = package.Id,
                Name = package.Name,
                Description = package.Description,
                Price = package.Price,
                DurationDays = package.DurationDays,
                TokenQuota = package.TokenQuota
            };

            return View(dto);
        }

        // Xác nhận thanh toán - redirect sang PayOS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int packageId)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account");

            try
            {
                var (paymentUrl, orderId) = await _paymentService.CreatePaymentAsync(userId, packageId);

                // Lưu orderId vào session để xác minh sau callback
                HttpContext.Session.SetInt32("PendingOrderId", orderId);

                return Redirect(paymentUrl);
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Buy), new { packageId });
            }
        }

        [HttpPost("api/payment/webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentWebhook()
        {
            try
            {
                var request = HttpContext.Request;
                request.EnableBuffering();

                string rawBody;
                using (var reader = new StreamReader(request.Body, leaveOpen: true))
                {
                    rawBody = await reader.ReadToEndAsync();
                    request.Body.Position = 0;
                }

                Console.WriteLine($"[PayOS Webhook] Received: {rawBody}");

                using var doc = JsonDocument.Parse(rawBody);
                var root = doc.RootElement;

                // PayOS gửi request test lúc confirm webhook, data = null -> chỉ cần trả 200
                if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
                {
                    return Ok(new { success = true });
                }

                long orderCode = data.TryGetProperty("orderCode", out var orderCodeProp)
                    ? orderCodeProp.GetInt64()
                    : 0;

                if (orderCode == 0)
                    return Ok(new { success = true }); // tránh 400/500 khiến PayOS coi webhook lỗi

                string topCode = root.TryGetProperty("code", out var codeProp) ? codeProp.GetString() ?? "" : "";
                int status = topCode == "00" ? 1 : 0;

                await _paymentService.HandlePaymentCallbackAsync(orderCode.ToString(), status);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayOS Webhook Error] {ex.Message}");
                // Vẫn nên trả 200 để không làm hỏng lượt confirm/retry của PayOS,
                // log lỗi để bạn tự điều tra thay vì để PayOS thấy webhook "chết"
                return Ok(new { success = false, error = ex.Message });
            }
        }

        private bool VerifyPayOSSignature(string rawBody, string signatureFromHeader, string checksumKey)
        {
            try
            {
                using (var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(checksumKey)))
                {
                    var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawBody));
                    string calculatedSignature = Convert.ToHexString(hashBytes).ToLower();

                    // So sánh (remove "Bearer " prefix nếu có)
                    string signatureToCheck = signatureFromHeader.Replace("Bearer ", "").Trim();

                    return calculatedSignature == signatureToCheck;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Signature verification error: {ex.Message}");
                return false;
            }
        }

        // Trang success - người dùng quay lại sau thanh toán
        [HttpGet]
        public async Task<IActionResult> Success()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account");

            // Lấy subscription hiện tại của user
            var subscriptions = await _subscriptionRepo.GetAllAsync(s =>
                s.UserId == userId && s.Status == Shared.Enums.SubscriptionStatus.Active && !s.IsDeleted);

            var subscription = subscriptions
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefault();

            if (subscription == null)
            {
                TempData["Info"] = "Thanh toán đang được xử lý, vui lòng đợi...";
                return RedirectToAction(nameof(MySubscription));
            }

            var dto = new UserSubscriptionDto
            {
                Id = subscription.Id,
                UserId = subscription.UserId,
                PackageName = subscription.Package?.Name ?? "N/A",
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                RemainingTokenQuota = subscription.RemainingTokenQuota,
                Status = subscription.Status
            };

            return View(dto);
        }

        // Trang failed
        [HttpGet]
        public IActionResult Failed()
        {
            TempData["Error"] = "Thanh toán thất bại hoặc bị hủy. Vui lòng thử lại.";
            return RedirectToAction(nameof(Buy));
        }

        // Xem subscription hiện tại của user
        [HttpGet]
        public async Task<IActionResult> MySubscription()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account");

            var subscriptions = await _subscriptionRepo.GetAllWithIncludeAsync(
                s => s.UserId == userId && !s.IsDeleted,
                s => s.Package
            );

            var dtos = subscriptions
                .OrderByDescending(s => s.Status == Shared.Enums.SubscriptionStatus.Active)
                .ThenByDescending(s => s.EndDate)
                .Select(s => new UserSubscriptionDto
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    PackageName = s.Package?.Name ?? "N/A",
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    RemainingTokenQuota = s.RemainingTokenQuota,
                    Status = s.Status
                })
                .ToList();

            return View(dtos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPaymentManual()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account");

            try
            {
                // Lấy Order cuối cùng của user (chưa Success)
                var orders = await _orderRepo.GetAllAsync(o =>
                    o.UserId == userId && o.Status != Shared.Enums.OrderStatus.Success);

                var order = orders.OrderByDescending(o => o.CreatedAt).FirstOrDefault();

                if (order == null)
                    throw new InvalidOperationException("Không tìm thấy order");

                // Gọi HandlePaymentCallbackAsync manually (mô phỏng webhook)
                await _paymentService.HandlePaymentCallbackAsync(order.TransactionCode, 1);

                return RedirectToAction(nameof(MySubscription));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Success));
            }
        }
    }
}
