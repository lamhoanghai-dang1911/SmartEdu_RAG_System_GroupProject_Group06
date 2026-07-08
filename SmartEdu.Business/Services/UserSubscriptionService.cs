using SmartEdu.Business.Interfaces;
using SmartEdu.Data.Repositories;
using SmartEdu.Shared.Entities;
using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Enums;

namespace SmartEdu.Business.Services
{
    public class UserSubscriptionService : IUserSubscriptionService
    {
        private readonly IRepository<UserSubscription> _subscriptionRepo;

        public UserSubscriptionService(IRepository<UserSubscription> subscriptionRepo)
        {
            _subscriptionRepo = subscriptionRepo;
        }

        public async Task<IEnumerable<UserSubscriptionDto>> GetAllByUserIdAsync(int userId)
        {
            var subscriptions = (await _subscriptionRepo.GetAllWithIncludeAsync(
                s => s.UserId == userId && !s.IsDeleted,
                s => s.Package
            )).ToList();

            // Lazy check: sửa lại Status cho đúng thực tế trước khi trả về, không đợi background job
            await ApplyLazyExpireAsync(subscriptions);

            return subscriptions
                .OrderByDescending(s => s.Status == SubscriptionStatus.Active)
                .ThenByDescending(s => s.EndDate)
                .Select(MapToDto)
                .ToList();
        }

        public async Task<UserSubscriptionDto?> GetActiveSubscriptionDtoAsync(int userId)
        {
            var subscriptions = (await _subscriptionRepo.GetAllWithIncludeAsync(
                s => s.UserId == userId && s.Status == SubscriptionStatus.Active && !s.IsDeleted,
                s => s.Package
            )).ToList();

            // Sau bước lazy check, một số bản ghi có thể vừa bị chuyển Expired -> phải lọc lại
            await ApplyLazyExpireAsync(subscriptions);

            var subscription = subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefault();

            return subscription == null ? null : MapToDto(subscription);
        }

        public async Task<bool> HasUsableActiveSubscriptionAsync(int userId)
        {
            var subscriptions = (await _subscriptionRepo.GetAllAsync(s =>
                s.UserId == userId
                && s.Status == SubscriptionStatus.Active
                && !s.IsDeleted)).ToList();

            await ApplyLazyExpireAsync(subscriptions);

            // Sau khi lazy-expire, chỉ những bản ghi vẫn còn Active mới thực sự "usable"
            return subscriptions.Any(s =>
                s.Status == SubscriptionStatus.Active
                && s.EndDate >= DateTime.UtcNow
                && s.RemainingTokenQuota > 0);
        }

        public async Task CancelSubscriptionAsync(int userId, int subscriptionId)
        {
            var subscription = await _subscriptionRepo.GetByIdAsync(subscriptionId);

            if (subscription == null || subscription.IsDeleted)
                throw new InvalidOperationException("Gói dịch vụ không tồn tại.");

            if (subscription.UserId != userId)
                throw new UnauthorizedAccessException("Bạn không có quyền hủy gói này.");

            if (subscription.Status != SubscriptionStatus.Active)
                throw new InvalidOperationException("Chỉ có thể hủy gói đang ở trạng thái hoạt động.");

            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.UpdatedAt = DateTime.UtcNow;

            _subscriptionRepo.Update(subscription);
            await _subscriptionRepo.SaveChangesAsync();
        }

        /// <summary>
        /// Lazy check: quét danh sách subscription đang có trong tay, nếu phát hiện
        /// Status = Active nhưng thực tế đã hết hạn hoặc hết token, tự động sửa thành Expired
        /// và lưu ngay xuống DB. Không đụng tới các bản ghi đã Cancelled/Expired sẵn.
        /// </summary>
        private async Task ApplyLazyExpireAsync(IEnumerable<UserSubscription> subscriptions)
        {
            bool hasChanges = false;

            foreach (var s in subscriptions)
            {
                if (s.Status == SubscriptionStatus.Active
                    && (s.EndDate < DateTime.UtcNow || s.RemainingTokenQuota <= 0))
                {
                    s.Status = SubscriptionStatus.Expired;
                    s.UpdatedAt = DateTime.UtcNow;
                    _subscriptionRepo.Update(s);
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _subscriptionRepo.SaveChangesAsync();
            }
        }

        private static UserSubscriptionDto MapToDto(UserSubscription s) => new UserSubscriptionDto
        {
            Id = s.Id,
            UserId = s.UserId,
            PackageName = s.Package?.Name ?? "N/A",
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            RemainingTokenQuota = s.RemainingTokenQuota,
            Status = s.Status
        };
    }
}