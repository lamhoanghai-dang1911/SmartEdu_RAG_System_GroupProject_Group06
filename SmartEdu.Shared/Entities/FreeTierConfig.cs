using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.Entities
{
    public class FreeTierConfig : BaseEntity
    {
        public int TokensPerWindow { get; set; } = 8000;
        public int WindowHours { get; set; } = 24;
        public bool IsActive { get; set; } = true;

        public int UpdatedByUserId { get; set; }
        public User UpdatedByUser { get; set; } = null!;
    }
}
