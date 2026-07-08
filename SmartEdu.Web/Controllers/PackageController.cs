using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEdu.Business.Interfaces;
using System.Text.Json;

namespace SmartEdu.Web.Controllers
{
    [Authorize(Roles = "Student")]
    public class PackageController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IPackageService _packageService;
        private readonly IUserSubscriptionService _subscriptionService;
        private readonly IOrderService _orderService;

        public PackageController(
            IPaymentService paymentService,
            IPackageService packageService,
            IUserSubscriptionService subscriptionService,
            IOrderService orderService)
        {
            _paymentService = paymentService;
            _packageService = packageService;
            _subscriptionService = subscriptionService;
            _orderService = orderService;
        }

        // Danh sách gói - không cần Auth (ai cũng xem được)
        [AllowAnonymous]
        public async Task<IActionResult> List()
        {
            var dtos = await _packageService.GetActivePackagesAsync();
            return View(dtos);
        }

        // Trang mua gói
        [HttpGet]
        public async Task<IActionResult> Buy(int packageId)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account");

            // === Chặn mua gói mới khi đang có gói còn hạn + còn token (Phương án A) ===
            bool hasUsable = await _subscriptionService.HasUsableActiveSubscriptionAsync(userId);
            if (hasUsable)
            {
                TempData["Error"] = "Bạn đang có gói dịch vụ còn hiệu lực (chưa hết hạn và vẫn còn token). " +
                                     "Vui lòng sử dụng hết gói hiện tại hoặc đợi hết hạn trước khi mua gói mới.";
                return RedirectToAction(nameof(MySubscription));
            }

            var dto = await _packageService.GetByIdAsync(packageId);
            if (dto == null)
                return NotFound("Gói dịch vụ không tồn tại.");

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

            // === Chặn lần nữa ở bước xác nhận, phòng trường hợp user mở 2 tab
            // hoặc bấm nút quá nhanh trước khi trang Buy kịp redirect ===
            bool hasUsable = await _subscriptionService.HasUsableActiveSubscriptionAsync(userId);
            if (hasUsable)
            {
                TempData["Error"] = "Bạn đang có gói dịch vụ còn hiệu lực, không thể mua thêm gói mới lúc này.";
                return RedirectToAction(nameof(MySubscription));
            }

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

        // Trang success - người dùng quay lại sau thanh toán
        [HttpGet]
        public async Task<IActionResult> Success()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account");

            var dto = await _subscriptionService.GetActiveSubscriptionDtoAsync(userId);

            if (dto == null)
            {
                TempData["Info"] = "Thanh toán đang được xử lý, vui lòng đợi...";
                return RedirectToAction(nameof(MySubscription));
            }

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

            var dtos = await _subscriptionService.GetAllByUserIdAsync(userId);
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
                var pendingOrder = await _orderService.GetLatestNonSuccessOrderAsync(userId);

                if (pendingOrder == null || string.IsNullOrWhiteSpace(pendingOrder.Value.TransactionCode))
                    throw new InvalidOperationException("Không tìm thấy order");

                // Gọi HandlePaymentCallbackAsync manually (mô phỏng webhook)
                await _paymentService.HandlePaymentCallbackAsync(pendingOrder.Value.TransactionCode!, 1);

                return RedirectToAction(nameof(MySubscription));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Success));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account");

            try
            {
                await _subscriptionService.CancelSubscriptionAsync(userId, id);
                TempData["Success"] = "Đã hủy gói dịch vụ thành công.";
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Bạn không có quyền hủy gói này.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(MySubscription));
        }
    }
}