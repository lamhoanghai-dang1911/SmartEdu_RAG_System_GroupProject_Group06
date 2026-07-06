using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Interfaces
{
    public interface IPaymentService
    {
        // Tạo order và redirect URL sang PayOS
        Task<(string PaymentUrl, int OrderId)> CreatePaymentAsync(int userId, int packageId);

        // Webhook từ PayOS callback
        Task HandlePaymentCallbackAsync(string transactionCode, int status);
    }
}
