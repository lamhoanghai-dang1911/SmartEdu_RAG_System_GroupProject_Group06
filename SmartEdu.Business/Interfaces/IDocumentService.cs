using Microsoft.AspNetCore.Http;
using SmartEdu.Shared.DTOs;

namespace SmartEdu.Business.Interfaces
{
    public interface IDocumentService
    {
        Task<IEnumerable<DocumentDto>> GetAllAsync(int? subjectId = null);
        Task<DocumentDto?> GetByIdAsync(int id);
        Task<DocumentDto> UploadAsync(IFormFile file, string title, int subjectId, string webRootPath);
        Task<IEnumerable<DocumentDto>> GetAllByUserIdAsync(int userId, bool isStaff, int? subjectId = null);
        Task DeleteAsync(int id);
        Task TriggerEmbeddingAsync(int documentId);
        Task UpdateTitleAsync(int id, string newTitle);
        Task<DocumentDownloadDto?> GetFileForDownloadAsync(int id);
        Task<bool> HasReadyDocumentsAsync(int subjectId);
        Task<IEnumerable<SmartEdu.Shared.DTOs.DocumentChunkDto>> GetChunksByDocumentIdAsync(int documentId);
        Task HandleDuplicateAsync(DuplicateHandleDto dto, int currentUserId);
        // Create document DB record from an already-saved temporary file. The temp file will be moved to uploads folder.
        Task<DocumentDto> CreateFromTempAsync(string tempFilePath, string originalFileName, string title, int subjectId, string fileHash, long fileSize, string webRootPath);
        Task<DocumentChunkDetailDto?> GetChunkDetailAsync(int chunkId);
        Task<IEnumerable<DocumentDto>> GetAllByUserIdAsync(int userId, bool isAdmin, bool isLecturer, int? subjectId = null);
        //Task<DocumentSourcePanelDto?> GetChunksAroundAsync(int documentId, int centerChunkIndex, int range = 10);
        //Task<DocumentSourcePanelDto?> GetChunksRangeAsync(int documentId, int fromIndex, int toIndex);
        //Task<int?> GetChunkIndexByIdAsync(int chunkId);
        Task<int?> GetChunkIndexByIdAsync(int chunkId);
        Task<DocumentSourcePanelDto?> GetChunksAroundCitationAsync(int documentId, int chunkId, int range = 10);
        Task<DocumentSourcePanelDto?> GetChunksRangeAsync(int documentId, int fromIndex, int toIndex);

        Task<DuplicateCheckDto> CheckDuplicateAsync(string filePath, string fileExt, string fileHash, int subjectId, int excludeDocumentId = 0);
    }
}
