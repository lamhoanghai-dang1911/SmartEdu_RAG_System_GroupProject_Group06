using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Interfaces
{
    public interface IOrderService
    {
        Task<(int OrderId, string? TransactionCode)?> GetLatestNonSuccessOrderAsync(int userId);
    }
}
