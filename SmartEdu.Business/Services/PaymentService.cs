using Microsoft.Extensions.Configuration;
using SmartEdu.Business.Interfaces;
using SmartEdu.Data.Repositories;
using SmartEdu.Shared.Entities;
using SmartEdu.Shared.Enums;

namespace SmartEdu.Business.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IRepository<Order> _orderRepo;
        private readonly IRepository<UserSubscription> _subscriptionRepo;
        private readonly IRepository<Package> _packageRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly IRealtimeNotifier _realtime;

        public PaymentService(
            IRepository<Order> orderRepo,
            IRepository<UserSubscription> subscriptionRepo,
            IRepository<Package> packageRepo,
            IRepository<User> userRepo,
            IConfiguration configuration,
            HttpClient httpClient,
            IRealtimeNotifier realtime)
        {
            _orderRepo = orderRepo;
            _subscriptionRepo = subscriptionRepo;
            _packageRepo = packageRepo;
            _userRepo = userRepo;
            _configuration = configuration;
            _httpClient = httpClient;
            _realtime = realtime;
        }

        public async Task<(string PaymentUrl, int OrderId)> CreatePaymentAsync(int userId, int packageId)
        {
            var package = await _packageRepo.GetByIdAsync(packageId);
            if (package == null || !package.IsActive)
                throw new InvalidOperationException("Gói dịch vụ không tồn tại hoặc đã bị vô hiệu hóa.");

            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User không tồn tại.");

            var order = new Order
            {
                UserId = userId,
                PackageId = packageId,
                Amount = package.Price,
                Method = PaymentMethod.PayOS,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _orderRepo.AddAsync(order);
            await _orderRepo.SaveChangesAsync();

            string paymentUrl = await CreatePaymentLinkAsync(order, package, user);

            return (paymentUrl, order.Id);
        }

        private async Task<string> CreatePaymentLinkAsync(Order order, Package package, User user)
        {
            var clientId = _configuration["PayOS:ClientId"];
            var apiKey = _configuration["PayOS:ApiKey"];
            var checksumKey = _configuration["PayOS:ChecksumKey"];
            var baseUrl = _configuration["PayOS:BaseUrl"];
            var returnUrl = _configuration["PayOS:ReturnUrl"];
            var cancelUrl = _configuration["PayOS:CancelUrl"];

            long orderCode = order.Id * 1000000 + (DateTime.UtcNow.Ticks % 1000000);
            int amountInVND = (int)order.Amount;
            string description = $"Mua gói {package.Name}";

            string signatureData = $"amount={amountInVND}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
            string signature = GenerateSignature(signatureData, checksumKey);

            Console.WriteLine($"[PayOS Debug] OrderCode (số): {orderCode}");
            Console.WriteLine($"[PayOS Debug] Signature data: {signatureData}");
            Console.WriteLine($"[PayOS Debug] Signature: {signature}");

            var paymentPayload = new
            {
                orderCode = orderCode,
                amount = amountInVND,
                description = description,
                returnUrl = returnUrl,
                cancelUrl = cancelUrl,
                buyerName = user.FullName,
                buyerEmail = user.Email ?? "noemail@payos.vn",
                buyerPhone = user.PhoneNumber ?? "0000000000",
                signature = signature
            };

            var jsonPayload = System.Text.Json.JsonSerializer.Serialize(paymentPayload);
            Console.WriteLine($"[PayOS Debug] Payload: {jsonPayload}");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v2/payment-requests");
            request.Headers.Add("x-client-id", clientId);
            request.Headers.Add("x-api-key", apiKey);
            request.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"[PayOS Response] {responseContent}");

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"PayOS API error: {response.StatusCode} - {responseContent}");

            using var doc = System.Text.Json.JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var data) && data.ValueKind != System.Text.Json.JsonValueKind.Null)
            {
                if (data.TryGetProperty("checkoutUrl", out var checkoutUrl))
                {
                    string paymentUrl = checkoutUrl.GetString() ?? throw new InvalidOperationException("PayOS không trả về checkout URL");

                    order.TransactionCode = orderCode.ToString();
                    _orderRepo.Update(order);
                    await _orderRepo.SaveChangesAsync();

                    return paymentUrl;
                }
            }

            throw new InvalidOperationException("PayOS trả về response không hợp lệ.");
        }

        public async Task HandlePaymentCallbackAsync(string transactionCode, int status)
        {
            Console.WriteLine($"[HandleCallback] transactionCode={transactionCode}, status={status}");

            var existingOrder = await _orderRepo.GetAllAsync(o => o.TransactionCode == transactionCode);
            var order = existingOrder.FirstOrDefault();

            Console.WriteLine($"[HandleCallback] Found order: {order?.Id}");

            if (order == null)
                throw new InvalidOperationException($"Order với transactionCode {transactionCode} không tồn tại.");

            if (order.Status == OrderStatus.Success)
            {
                Console.WriteLine($"[HandleCallback] Order đã Success, return");
                return;
            }

            if (status != 1)
            {
                Console.WriteLine($"[HandleCallback] Status != 1, mark as Failed");
                order.Status = OrderStatus.Failed;
                order.PaidAt = null;
                _orderRepo.Update(order);
                await _orderRepo.SaveChangesAsync();
                return;
            }

            Console.WriteLine($"[HandleCallback] Updating order to Success");
            order.Status = OrderStatus.Success;
            order.PaidAt = DateTime.UtcNow;
            _orderRepo.Update(order);
            await _orderRepo.SaveChangesAsync();

            var package = await _packageRepo.GetByIdAsync(order.PackageId);
            if (package == null)
                throw new InvalidOperationException("Package không tồn tại.");

            Console.WriteLine($"[HandleCallback] Creating UserSubscription");
            var subscription = new UserSubscription
            {
                UserId = order.UserId,
                PackageId = order.PackageId,
                OrderId = order.Id,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(package.DurationDays),
                RemainingTokenQuota = package.TokenQuota,
                Status = SubscriptionStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await _subscriptionRepo.AddAsync(subscription);
            await _subscriptionRepo.SaveChangesAsync();

            Console.WriteLine($"[HandleCallback] UserSubscription created successfully");

            try
            {
                await _realtime.SendPaymentCompletedAsync(
                    order.UserId,
                    order.Id,
                    package.Name,
                    subscription.RemainingTokenQuota,
                    subscription.EndDate);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to broadcast payment completed: {ex}");
            }
        }

        private string GenerateSignature(string data, string checksumKey)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(checksumKey));
            var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hashBytes).ToLower();
        }
    }
}