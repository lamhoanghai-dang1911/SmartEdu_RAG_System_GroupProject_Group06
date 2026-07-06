using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.DTOs
{
    public class PaymentWebhookDto
    {
        public string TransactionCode { get; set; } = string.Empty;
        public int Status { get; set; } // 1 = success, 0 = failed
        public string? Message { get; set; }
    }
}
