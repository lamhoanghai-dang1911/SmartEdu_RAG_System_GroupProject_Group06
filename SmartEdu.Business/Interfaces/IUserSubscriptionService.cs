using SmartEdu.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Interfaces
{
    public interface IUserSubscriptionService
    {
        Task<IEnumerable<UserSubscriptionDto>> GetAllByUserIdAsync(int userId);
        Task<UserSubscriptionDto?> GetActiveSubscriptionDtoAsync(int userId);
        Task<bool> HasUsableActiveSubscriptionAsync(int userId);
        Task CancelSubscriptionAsync(int userId, int subscriptionId);
    }
}
