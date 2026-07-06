using SmartEdu.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.DTOs
{
    public class PackageDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public int TokenQuota { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateOrderDto
    {
        public int PackageId { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PackageId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public OrderStatus Status { get; set; }
        public string? TransactionCode { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserSubscriptionDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RemainingTokenQuota { get; set; }
        public SubscriptionStatus Status { get; set; }
    }
}
