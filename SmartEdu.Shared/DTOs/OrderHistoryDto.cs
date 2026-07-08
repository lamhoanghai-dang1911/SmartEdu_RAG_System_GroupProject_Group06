using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.DTOs
{
    public class OrderHistoryItem
    {
        public int OrderId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OrderHistoryDto
    {
        public List<OrderHistoryItem> Items { get; set; } = new();
        public decimal TotalSpent { get; set; }
    }
}
