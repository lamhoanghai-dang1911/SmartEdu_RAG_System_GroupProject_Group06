using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.DTOs
{
    public class UploadConfigDto
    {
        public int Id { get; set; }
        public int MaxFileSizeMB { get; set; }
        public string? FileType { get; set; }
        public int? SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public bool IsActive { get; set; }
        public string UpdatedByUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class UploadConfigSaveDto
    {
        public int MaxFileSizeMB { get; set; }
        public string? FileType { get; set; }
        public int? SubjectId { get; set; }
    }
}
