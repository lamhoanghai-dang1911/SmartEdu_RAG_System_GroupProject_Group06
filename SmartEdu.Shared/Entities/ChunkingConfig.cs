using SmartEdu.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.Entities
{
    public class ChunkingConfig : BaseEntity
    {
        public int ChunkSize { get; set; } = 500;      // số token/ký tự mỗi chunk
        public int ChunkOverlap { get; set; } = 50;

        public ChunkingStrategy Strategy { get; set; } = ChunkingStrategy.FixedSize;
        public ChunkingScope Scope { get; set; } = ChunkingScope.Global;

        public int? SubjectId { get; set; }            // null nếu Scope = Global
        public Subject? Subject { get; set; }

        public bool IsActive { get; set; } = true;      // config đang dùng (cho phép lưu lịch sử config cũ)

        public int UpdatedByUserId { get; set; }
        public User UpdatedByUser { get; set; } = null!;
    }
}
