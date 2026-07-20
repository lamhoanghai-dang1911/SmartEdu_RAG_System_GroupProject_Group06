using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartEdu.Business.Interfaces;
using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Enums;
using SmartEdu.Web.Extensions;

namespace SmartEdu.Web.Controllers;

[Authorize]
public class DocumentController : Controller
{
    private readonly IDocumentService _documentService;
    private readonly ISubjectService _subjectService;
    private readonly IPermissionService _permissionService;
    private readonly IChunkingConfigService _chunkingConfigService;
    private readonly IWebHostEnvironment _env;
    private readonly IUploadConfigService _uploadConfigService;


    public DocumentController(
        IDocumentService documentService,
        ISubjectService subjectService,
        IPermissionService permissionService,
        IChunkingConfigService chunkingConfigService,
        IWebHostEnvironment env,
        IUploadConfigService uploadConfigService)
    {
        _documentService = documentService;
        _subjectService = subjectService;
        _permissionService = permissionService;
        _chunkingConfigService = chunkingConfigService;
        _env = env;
        _uploadConfigService = uploadConfigService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDocumentChunks(int documentId)
    {
        var doc = await _documentService.GetByIdAsync(documentId);
        if (doc == null) return NotFound();

        int userId = User.GetUserId();
        bool hasAccess = await HasDocumentAccessAsync(userId, doc.SubjectId);
        if (!hasAccess) return Forbid();

        var chunks = await _documentService.GetChunksByDocumentIdAsync(documentId);
        return Json(chunks);
    }

    [HttpGet]
    public async Task<IActionResult> GetChunkDetail(int chunkId)
    {
        var dto = await _documentService.GetChunkDetailAsync(chunkId);
        if (dto == null) return NotFound();
        return Json(dto);
    }

    [HttpGet]
    public async Task<IActionResult> CanUpload(int subjectId)
    {
        if (subjectId <= 0) return Json(new { canUpload = false, message = "Vui lòng chọn môn học." });
        // Only roles allowed to access Upload page are Lecturer and Admin (controller Upload GET is protected)
        if (User.IsInRole("Admin"))
        {
            return Json(new { canUpload = true, userId = User.GetUserId() });
        }

        if (!User.IsInRole("Lecturer"))
        {
            return Json(new { canUpload = false, message = "Bạn không có quyền upload tài liệu cho môn này. Chỉ trưởng môn được phép.", userId = User.GetUserId() });
        }

        int userId = User.GetUserId();
        var can = await _subject_service_canupload(userId, subjectId);
        if (!can)
            return Json(new { canUpload = false, message = "Bạn không có quyền upload tài liệu cho môn này. Chỉ trưởng môn được phép.", userId });

        return Json(new { canUpload = true, userId });
    }

    // wrapper to call subject service CanUploadDocument with defensive null checks
    private async Task<bool> _subject_service_canupload(int userId, int subjectId)
    {
        try
        {
            return await _subjectService.CanUploadDocument(userId, subjectId);
        }
        catch
        {
            return false;
        }
    }

    [HttpGet]
    [Authorize(Roles = "Lecturer, Admin")]
    public IActionResult TriggerEmbeddingGet(int id)
    {
        try
        {
            var scopeFactory = HttpContext.RequestServices.GetService(typeof(IServiceScopeFactory)) as IServiceScopeFactory;
            if (scopeFactory != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        using var scope = scopeFactory.CreateScope();
                        var svc = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                        await svc.TriggerEmbeddingAsync(id);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Background embedding failed (GET): {ex}");
                    }
                });
            }
            else
            {
                _ = Task.Run(() => _documentService.TriggerEmbeddingAsync(id));
            }

            TempData["Success"] = "Đã bắt đầu xử lý embedding (background). Mở Log để theo dõi tiến trình.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            var baseMsg = ex.GetBaseException()?.Message ?? ex.Message;
            TempData["Error"] = $"Lỗi khi khởi tạo embedding: {baseMsg}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs(int documentId)
    {
        var doc = await _documentService.GetByIdAsync(documentId);
        if (doc == null) return NotFound();

        int userId = User.GetUserId();
        bool hasAccess = await HasDocumentAccessAsync(userId, doc.SubjectId);
        if (!hasAccess) return Forbid();

        // Use UnitOfWork repository to fetch logs
        var uow = HttpContext.RequestServices.GetService(typeof(IUnitOfWork)) as IUnitOfWork;
        if (uow == null) return StatusCode(500);

        var logs = await uow.DocumentLogs.GetAllWithIncludeAsync(l => l.DocumentId == documentId, l => l.Document);
        var ordered = logs.OrderBy(l => l.Timestamp).Select(l => new { l.Id, l.LogMessage, Timestamp = l.Timestamp, l.Status });

        // Resolve active chunking config for this document's subject (per-subject overrides global)
        object configObj = null;
        try
        {
            var cfg = await _chunkingConfigService.ResolveActiveConfigAsync(doc.SubjectId);
            if (cfg != null)
            {
                string subjectName = null;
                if (cfg.Scope == SmartEdu.Shared.Enums.ChunkingScope.PerSubject && cfg.SubjectId.HasValue)
                {
                    var subject = await _subjectService.GetByIdAsync(cfg.SubjectId.Value);
                    subjectName = subject?.Name;
                }

                configObj = new
                {
                    cfg.ChunkSize,
                    cfg.ChunkOverlap,
                    Strategy = cfg.Strategy.ToString(),
                    Scope = cfg.Scope.ToString(),
                    cfg.SubjectId,
                    SubjectName = subjectName
                };
            }
        }
        catch
        {
            configObj = null;
        }

        return Json(new { logs = ordered, chunkingConfig = configObj });
    }

    public async Task<IActionResult> Index(int? subjectId)
    {
        int userId = User.GetUserId();
        bool isAdmin = User.IsInRole("Admin");
        bool isLecturer = User.IsInRole("Lecturer");

        var subjects = isAdmin
            ? await _subjectService.GetAllAsync()
            : isLecturer
                ? await _subjectService.GetSubjectsByLecturerIdAsync(userId)
                : await _subjectService.GetSubjectsByUserIdAsync(userId);

        var docs = await _documentService.GetAllByUserIdAsync(userId, isAdmin, isLecturer, subjectId);

        // Luôn khởi tạo, KHÔNG để null lọt xuống View trong bất kỳ trường hợp nào (Admin/Student/Lecturer)
        HashSet<int> leaderSubjectIds = new HashSet<int>();
        if (isLecturer && !isAdmin)
        {
            leaderSubjectIds = await _subjectService.GetLeaderSubjectIdsAsync(userId);
        }

        ViewBag.Subjects = new SelectList(subjects, "Id", "Name", subjectId);
        ViewBag.SelectedSubjectId = subjectId;
        ViewBag.LeaderSubjectIds = leaderSubjectIds;   // luôn có giá trị, không bao giờ null

        return View(docs);
    }


    public async Task<IActionResult> Details(int id)
    {
        var doc = await _documentService.GetByIdAsync(id);
        if (doc is null) return NotFound();

        int userId = User.GetUserId();
        bool hasAccess = await HasDocumentAccessAsync(userId, doc.SubjectId);
        if (!hasAccess) return Forbid();

        return View(doc);
    }

    [Authorize(Roles = "Lecturer, Admin")]
    public async Task<IActionResult> Upload()
    {
            var subjects = User.IsInRole("Admin")
                ? await _subjectService.GetAllAsync()
                : await _subjectService.GetSubjectsByLecturerIdAsync(User.GetUserId());
            ViewBag.Subjects = new SelectList(subjects, "Id", "Name");
        return View();
    }

    [HttpGet]
    [Authorize(Roles = "Lecturer, Admin")]
    public async Task<IActionResult> GetAssignedSubjects()
    {
        int userId = User.GetUserId();
        bool isAdmin = User.IsInRole("Admin");

        IEnumerable<SmartEdu.Shared.DTOs.SubjectDto> subjects;
        if (isAdmin)
            subjects = await _subjectService.GetAllAsync();
        else
            subjects = await _subjectService.GetSubjectsByLecturerIdAsync(userId);

        return Json(subjects);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file, string title, int subjectId)
    {
        if (file == null || file.Length == 0)
            return Json(new { success = false, error = "Vui lòng chọn file." });

        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Json(new { success = false, error = "Vui lòng đăng nhập lại." });

        bool canAccess = await _permissionService.CanUserAccessSubject(userId, subjectId);
        if (!canAccess)
            return Json(new { success = false, error = "Bạn không có quyền upload tài liệu cho môn học này." });

        try
        {
            var webRootPath = _env.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
                webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            var fileTypeKey = ext.TrimStart('.');
            var maxSizeMB = await _uploadConfigService.ResolveMaxFileSizeMBAsync(subjectId, fileTypeKey);
            var maxSizeBytes = (long)maxSizeMB * 1024 * 1024;
            if (file.Length > maxSizeBytes)
                return Json(new { success = false, error = $"File vượt quá kích thước cho phép ({maxSizeMB}MB)." });

            string fileHash = await ComputeFileHashAsync(file);

            var tempRoot = Path.Combine(webRootPath, "uploads", "temp");
            Directory.CreateDirectory(tempRoot);
            var tempName = $"{Guid.NewGuid()}{ext}";
            var tempPath = Path.Combine(tempRoot, tempName);
            await using (var stream = System.IO.File.Create(tempPath))
            {
                await file.CopyToAsync(stream);
            }

            var dupCheck = await _documentService.CheckDuplicateAsync(tempPath, ext, fileHash, subjectId);

            if (dupCheck.HasDuplicate)
            {
                HttpContext.Session.SetString("TempFilePath", tempPath);
                HttpContext.Session.SetString("TempFileName", file.FileName);
                HttpContext.Session.SetString("TempFileHash", fileHash);
                HttpContext.Session.SetString("TempTitle", title ?? string.Empty);
                HttpContext.Session.SetInt32("TempSubjectId", subjectId);
                HttpContext.Session.SetString("TempFileSize", file.Length.ToString());
                HttpContext.Session.SetInt32("OldDocumentId", dupCheck.DuplicateDocumentId ?? 0);
                HttpContext.Session.SetInt32("DuplicateMatchType", (int)dupCheck.MatchType);

                return Json(new
                {
                    isDuplicate = true,
                    matchType = dupCheck.MatchType.ToString(),
                    similarityPercent = dupCheck.SimilarityPercent,
                    oldDocumentId = dupCheck.DuplicateDocumentId,
                    isEmbeddingReady = dupCheck.IsEmbeddingReady,
                    message = dupCheck.MatchType == DuplicateMatchType.Exact
                        ? $"Tài liệu đã tồn tại: {dupCheck.DuplicateTitle}"
                        : $"Tài liệu này có vẻ giống {dupCheck.SimilarityPercent}% với tài liệu \"{dupCheck.DuplicateTitle}\" đã tải lúc {dupCheck.DuplicateCreatedAt:dd/MM/yyyy}."
                });
            }

            // Không trùng -> tạo document ngay từ file temp
            var dto = await _documentService.CreateFromTempAsync(tempPath, file.FileName, title, subjectId, fileHash, file.Length, webRootPath);
            return Json(new { success = true, message = "Upload thành công! Nhấn nút ⚡ để bắt đầu embedding." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> HandleDuplicate()
    {
        var newDocId = HttpContext.Session.GetInt32("NewDocumentId");
        var oldDocId = HttpContext.Session.GetInt32("OldDocumentId");

        if (!newDocId.HasValue || !oldDocId.HasValue)
            return RedirectToAction(nameof(Index));

        var newDoc = await _documentService.GetByIdAsync(newDocId.Value);
        var oldDoc = await _documentService.GetByIdAsync(oldDocId.Value);

        if (newDoc == null || oldDoc == null)
            return RedirectToAction(nameof(Index));

        ViewBag.NewDocument = newDoc;
        ViewBag.OldDocument = oldDoc;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmDuplicate(int action)
    {
        var oldDocId = HttpContext.Session.GetInt32("OldDocumentId");
        var matchTypeInt = HttpContext.Session.GetInt32("DuplicateMatchType") ?? 0;
        var matchType = (DuplicateMatchType)matchTypeInt;

        if (!oldDocId.HasValue)
            return RedirectToAction(nameof(Index));

        var chosenAction = (DocumentDuplicateAction)action;

        if (matchType == DuplicateMatchType.Exact && chosenAction == DocumentDuplicateAction.KeptBoth)
        {
            TempData["Error"] = "Không thể giữ cả hai khi tài liệu trùng khớp hoàn toàn. Vui lòng chọn Thay thế hoặc Hủy.";
            return RedirectToAction(nameof(Index));
        }

        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return RedirectToAction("Login", "Account");

        try
        {
            var tempPath = HttpContext.Session.GetString("TempFilePath");
            DocumentDto newDocDto = null;
            if (!string.IsNullOrWhiteSpace(tempPath))
            {
                var tempFileName = HttpContext.Session.GetString("TempFileName") ?? string.Empty;
                var tempFileHash = HttpContext.Session.GetString("TempFileHash") ?? string.Empty;
                var tempTitle = HttpContext.Session.GetString("TempTitle") ?? string.Empty;
                var tempSubjectId = HttpContext.Session.GetInt32("TempSubjectId") ?? 0;
                var tempFileSizeStr = HttpContext.Session.GetString("TempFileSize") ?? "0";
                long.TryParse(tempFileSizeStr, out var tempFileSize);

                newDocDto = await _documentService.CreateFromTempAsync(tempPath, tempFileName, tempTitle, tempSubjectId, tempFileHash, tempFileSize, _env.WebRootPath);
            }

            if (newDocDto == null)
            {
                TempData["Error"] = "Không có tài liệu tạm để xử lý.";
                return RedirectToAction(nameof(Index));
            }

            var dto = new DuplicateHandleDto
            {
                NewDocumentId = newDocDto.Id,
                OldDocumentId = oldDocId.Value,
                Action = chosenAction
            };

            await _documentService.HandleDuplicateAsync(dto, userId);

            HttpContext.Session.Remove("TempFilePath");
            HttpContext.Session.Remove("TempFileName");
            HttpContext.Session.Remove("TempFileHash");
            HttpContext.Session.Remove("TempTitle");
            HttpContext.Session.Remove("TempSubjectId");
            HttpContext.Session.Remove("TempFileSize");
            HttpContext.Session.Remove("OldDocumentId");
            HttpContext.Session.Remove("DuplicateMatchType");

            TempData["Success"] = "Xử lý tài liệu trùng thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (UnauthorizedAccessException)
        {
            TempData["Error"] = "Bạn không có quyền xử lý tài liệu trùng. Chỉ trưởng môn học mới có quyền này.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    // Helper: tính hash file (dùng lại logic từ DocumentService)
    private async Task<string> ComputeFileHashAsync(IFormFile file)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        using var stream = file.OpenReadStream();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hashBytes);
    }

    // helper wrapper to avoid repeating code and protect against exceptions
    private async Task<IEnumerable<SmartEdu.Shared.DTOs.SubjectDto>> _subject_service_getsubjects_for_current_user()
    {
        try
        {
            return await _subjectService.GetSubjectsByLecturerIdAsync(User.GetUserId());
        }
        catch
        {
            return Enumerable.Empty<SmartEdu.Shared.DTOs.SubjectDto>();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Lecturer, Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _documentService.DeleteAsync(id);
        TempData["Success"] = "Xóa tài liệu thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Lecturer, Admin")]
    public IActionResult TriggerEmbedding(int id)
    {
        try
        {
            // run embedding in a background scope so we can return immediately
            var scopeFactory = HttpContext.RequestServices.GetService(typeof(IServiceScopeFactory)) as IServiceScopeFactory;
            if (scopeFactory != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        using var scope = scopeFactory.CreateScope();
                        var svc = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                        await svc.TriggerEmbeddingAsync(id);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Background embedding failed: {ex}");
                    }
                });
            }
            else
            {
                // fallback: call directly (will run on request scope)
                _ = Task.Run(() => _documentService.TriggerEmbeddingAsync(id));
            }

            // Return Accepted immediately; client will poll logs
            return Accepted();
        }
        catch (Exception ex)
        {
            var baseMsg = ex.GetBaseException()?.Message ?? ex.Message;
            return StatusCode(500, baseMsg);
        }
    }

    [HttpGet]
    [Authorize(Roles = "Lecturer, Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var doc = await _documentService.GetByIdAsync(id);
        if (doc == null) return NotFound();

        return View(doc);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Lecturer, Admin")]
    public async Task<IActionResult> Edit(int id, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            ModelState.AddModelError("title", "Tiêu đề không được để trống.");
            var doc = await _documentService.GetByIdAsync(id);
            return View(doc);
        }

        try
        {
            await _documentService.UpdateTitleAsync(id, title);
            TempData["Success"] = "Cập nhật tiêu đề thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi cập nhật: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var doc = await _documentService.GetByIdAsync(id);
        if (doc == null) return NotFound();
        if (doc.Status == SmartEdu.Shared.Enums.DocumentStatus.Pending)
        {
            TempData["Error"] = "Tài liệu này đang chờ xử lý, chưa thể tải xuống lúc này.";
            return RedirectToAction(nameof(Index));
        }

        int userId = User.GetUserId();
        bool hasAccess = await HasDocumentAccessAsync(userId, doc.SubjectId);
        if (!hasAccess) return Forbid();

        try
        {
            var fileDto = await _documentService.GetFileForDownloadAsync(id);
            if (fileDto == null) return NotFound();

            return PhysicalFile(fileDto.FilePath, fileDto.ContentType, fileDto.FileName);
        }
        catch (FileNotFoundException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetUploadLimits(int subjectId)
    {
        if (subjectId <= 0) return Json(new { });

        var types = new[] { "pdf", "docx", "pptx" };
        var limits = new Dictionary<string, int>();

        foreach (var t in types)
        {
            limits[t] = await _uploadConfigService.ResolveMaxFileSizeMBAsync(subjectId, t);
        }

        return Json(limits);
    }

    [HttpGet]
    public async Task<IActionResult> GetChunksRange(int documentId, int fromIndex, int toIndex)
    {
        var doc = await _documentService.GetByIdAsync(documentId);
        if (doc == null) return NotFound();

        int userId = User.GetUserId();
        bool hasAccess = await HasDocumentAccessAsync(userId, doc.SubjectId);
        if (!hasAccess) return Forbid();

        if (toIndex - fromIndex > 200)
            return BadRequest("Phạm vi yêu cầu quá lớn (tối đa 200 chunk/lần).");

        var result = await _documentService.GetChunksRangeAsync(documentId, fromIndex, toIndex);
        if (result == null) return NotFound();

        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetChunksAroundCitation(int documentId, int chunkId, int range = 10)
    {
        var doc = await _documentService.GetByIdAsync(documentId);
        if (doc == null) return NotFound();

        int userId = User.GetUserId();
        bool hasAccess = await HasDocumentAccessAsync(userId, doc.SubjectId);
        if (!hasAccess) return Forbid();

        var result = await _documentService.GetChunksAroundCitationAsync(documentId, chunkId, range);
        if (result == null) return NotFound();

        return Json(result);
    }

    private async Task<bool> HasDocumentAccessAsync(int userId, int subjectId)
    {
        if (User.IsInRole("Admin")) return true;

        if (User.IsInRole("Lecturer"))
            return await _subjectService.IsLecturerAssignedToSubject(userId, subjectId);

        return await _permissionService.CanUserAccessSubject(userId, subjectId);
    }
}