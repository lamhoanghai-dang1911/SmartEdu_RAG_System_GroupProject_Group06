using SmartEdu.Shared.Enums;
using System;
using System.Collections.Generic;

namespace SmartEdu.Shared.DTOs
{
    public enum DuplicateMatchType { None = 0, Exact = 1, Near = 2 }

    /// <summary>
    /// Một tài liệu trùng (hoặc gần giống) được phát hiện trong môn học.
    /// </summary>
    public class DuplicateDocumentItemDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DuplicateMatchType MatchType { get; set; }
        public double SimilarityPercent { get; set; }   // Exact = 100
        public bool IsEmbeddingReady { get; set; }
    }

    public class DuplicateCheckDto
    {
        public bool HasDuplicate { get; set; }

        /// <summary>
        /// Exact nếu có ÍT NHẤT một tài liệu trùng 100%, ngược lại là Near.
        /// </summary>
        public DuplicateMatchType MatchType { get; set; } = DuplicateMatchType.None;

        /// <summary>
        /// TẤT CẢ tài liệu trùng/gần giống trong môn học, sắp xếp giảm dần theo % trùng.
        /// </summary>
        public List<DuplicateDocumentItemDto> Duplicates { get; set; } = new();

        // ===== Các field legacy (giữ lại để không phá code cũ đang dùng) =====
        // Luôn được gán bằng tài liệu trùng cao nhất (phần tử đầu của Duplicates).
        public int? DuplicateDocumentId { get; set; }
        public string? DuplicateTitle { get; set; }
        public DateTime? DuplicateCreatedAt { get; set; }
        public bool IsEmbeddingReady { get; set; }
        public double? SimilarityPercent { get; set; }
    }

    public class DuplicateHandleDto
    {
        public int NewDocumentId { get; set; }
        public int OldDocumentId { get; set; }
        public DocumentDuplicateAction Action { get; set; }
    }
}