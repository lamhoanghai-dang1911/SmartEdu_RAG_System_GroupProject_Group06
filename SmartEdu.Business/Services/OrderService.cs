using SmartEdu.Business.Interfaces;
using SmartEdu.Data.Repositories;
using SmartEdu.Shared.Entities;
using SmartEdu.Shared.Enums;

namespace SmartEdu.Business.Services
{
    public class OrderService : IOrderService
    {
        private readonly IRepository<Order> _orderRepo;

        public OrderService(IRepository<Order> orderRepo)
        {
            _orderRepo = orderRepo;
        }

        public async Task<(int OrderId, string? TransactionCode)?> GetLatestNonSuccessOrderAsync(int userId)
        {
            var orders = await _orderRepo.GetAllAsync(o =>
                o.UserId == userId && o.Status != OrderStatus.Success);

            var order = orders.OrderByDescending(o => o.CreatedAt).FirstOrDefault();
            if (order == null) return null;

            return (order.Id, order.TransactionCode);
        }
    }
}
