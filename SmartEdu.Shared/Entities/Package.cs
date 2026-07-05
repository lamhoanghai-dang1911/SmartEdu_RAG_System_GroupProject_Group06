using SmartEdu.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.Entities
{
    public class Package : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public int TokenQuota { get; set; }         // quota token cấp khi mua gói
        public bool IsActive { get; set; } = true;
    }

    public class Order : BaseEntity
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int PackageId { get; set; }
        public Package Package { get; set; } = null!;

        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public string? TransactionCode { get; set; }   // mã giao dịch bên gateway trả về
        public string? GatewayResponseRaw { get; set; } // lưu JSON raw để debug/đối soát
        public DateTime? PaidAt { get; set; }
    }

    public class UserSubscription : BaseEntity
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int PackageId { get; set; }
        public Package Package { get; set; } = null!;

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RemainingTokenQuota { get; set; }

        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    }
}
