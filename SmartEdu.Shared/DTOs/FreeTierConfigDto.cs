using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.DTOs
{
    public class FreeTierConfigDto
    {
        public int Id { get; set; }
        public int TokensPerWindow { get; set; }
        public int WindowHours { get; set; }
        public bool IsActive { get; set; }
        public string UpdatedByUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class FreeTierConfigSaveDto
    {
        public int TokensPerWindow { get; set; }
        public int WindowHours { get; set; }
    }
}
