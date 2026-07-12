using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.Entities
{
    public class FreeTierUsage : BaseEntity
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime WindowStartAt { get; set; }
        public int TokensUsedInWindow { get; set; }
    }
}
