using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.Entities
{
    public class UploadConfig : BaseEntity
    {
        public int MaxFileSizeMB { get; set; } = 10;

        public string? FileType { get; set; }

        public int? SubjectId { get; set; }
        public Subject? Subject { get; set; }

        public bool IsActive { get; set; } = true;

        public double? NearDuplicateThreshold { get; set; }

        public int UpdatedByUserId { get; set; }
        public User UpdatedByUser { get; set; } = null!;
    }
}
