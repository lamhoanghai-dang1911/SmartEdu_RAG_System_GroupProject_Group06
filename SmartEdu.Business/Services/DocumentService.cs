using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SmartEdu.Business.Interfaces;
using SmartEdu.Data.Repositories;
using SmartEdu.Shared.Entities;
using SmartEdu.Shared.Enums;

namespace SmartEdu.Business.Services;

public class DocumentService : IDocumentService
{
    private readonly IRepository<Document> _docRepo;
    private readonly IWebHostEnvironment _env;

    public DocumentService(IRepository<Document> docRepo, IWebHostEnvironment env)
    {
        _docRepo = docRepo;
        _env = env;
    }

    public async Task<IEnumerable<Document>> GetAllAsync(int? subjectId = null)
    {
        var docs = await _docRepo.GetAllWithIncludeAsync(d => d.Subject);
        if (subjectId.HasValue)
            return docs.Where(d => d.SubjectId == subjectId.Value);
        return docs;
    }

    public async Task<Document?> GetByIdAsync(int id)
        => await _docRepo.GetByIdAsync(id);

    public async Task<Document> UploadAsync(IFormFile file, string title, int subjectId)
    {
        var ext = Path.GetExtension(file.FileName).ToLower();
        if (ext is not ".pdf" and not ".docx")
            throw new InvalidOperationException("Chỉ hỗ trợ PDF và DOCX.");

        var uploadRoot = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadRoot);

        var savedName = $"{Guid.NewGuid()}{ext}";
        var savedPath = Path.Combine(uploadRoot, savedName);

        await using var stream = File.Create(savedPath);
        await file.CopyToAsync(stream);

        var doc = new Document
        {
            Title = title,
            FileName = file.FileName,
            FilePath = savedPath,
            FileType = ext.TrimStart('.'),
            FileSize = file.Length,
            SubjectId = subjectId,
            Status = DocumentStatus.Pending
        };

        await _docRepo.AddAsync(doc);
        await _docRepo.SaveChangesAsync();
        return doc;
    }

    public async Task DeleteAsync(int id)
    {
        var doc = await _docRepo.GetByIdAsync(id);
        if (doc is null) return;
        doc.IsDeleted = true;
        doc.UpdatedAt = DateTime.UtcNow;
        _docRepo.Update(doc);
        await _docRepo.SaveChangesAsync();
    }

    public async Task TriggerEmbeddingAsync(int documentId)
    {
        var doc = await _docRepo.GetByIdAsync(documentId);
        if (doc is null) return;
        doc.Status = DocumentStatus.Processing;
        doc.UpdatedAt = DateTime.UtcNow;
        _docRepo.Update(doc);
        await _docRepo.SaveChangesAsync();
    }
}