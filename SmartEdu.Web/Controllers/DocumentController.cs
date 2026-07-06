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
    private readonly IWebHostEnvironment _env;


    public DocumentController(
        IDocumentService documentService,
        ISubjectService subjectService,
        IPermissionService permissionService,
        IWebHostEnvironment env)
    {
        _documentService = documentService;
        _subjectService = subjectService;
        _permissionService = permissionService;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetDocumentChunks(int documentId)
    {
        var doc = await _documentService.GetByIdAsync(documentId);
        if (doc == null) return NotFound();

        int userId = User.GetUserId();
        bool isStaff = User.IsInRole("Lecturer") || User.IsInRole("Admin");
        if (!isStaff)
        {
            var canAccess = await _permissionService.CanUserAccessSubject(userId, doc.SubjectId);
            if (!canAccess) return Forbid();
        }

        // use service to map to DTOs
        var chunks = await _documentService.GetChunksByDocumentIdAsync(documentId);
        return Json(chunks);
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
        // Only allow users who can access the document to read logs
        var doc = await _documentService.GetByIdAsync(documentId);
        if (doc == null) return NotFound();

        int userId = User.GetUserId();
        bool isStaff = User.IsInRole("Lecturer") || User.IsInRole("Admin");
        if (!isStaff)
        {
            var canAccess = await _permissionService.CanUserAccessSubject(userId, doc.SubjectId);
            if (!canAccess) return Forbid();
        }

        // Use UnitOfWork repository to fetch logs
        var uow = HttpContext.RequestServices.GetService(typeof(IUnitOfWork)) as IUnitOfWork;
        if (uow == null) return StatusCode(500);

        var logs = await uow.DocumentLogs.GetAllWithIncludeAsync(l => l.DocumentId == documentId, l => l.Document);
        var ordered = logs.OrderBy(l => l.Timestamp).Select(l => new { l.Id, l.LogMessage, Timestamp = l.Timestamp, l.Status });
        return Json(ordered);
    }

    public async Task<IActionResult> Index(int? subjectId)
    {
        int userId = User.GetUserId();
        bool isStaff = User.IsInRole("Lecturer") || User.IsInRole("Admin");
        var subjects = isStaff
            ? await _subjectService.GetAllAsync()
            : await _subjectService.GetSubjectsByUserIdAsync(userId);

        var docs = await _documentService.GetAllByUserIdAsync(userId, isStaff, subjectId);

        ViewBag.Subjects = new SelectList(subjects, "Id", "Name", subjectId);
        ViewBag.SelectedSubjectId = subjectId;

        return View(docs);
    }


    public async Task<IActionResult> Details(int id)
    {
        var doc = await _documentService.GetByIdAsync(id);
        if (doc is null) return NotFound();

        bool hasAccess = await _permissionService.CanUserAccessSubject(User.GetUserId(), doc.SubjectId);
        if (!hasAccess) return Forbid();

        return View(doc);
    }

    [Authorize(Roles = "Lecturer, Admin")]
    [RequestSizeLimit(10 * 1024 * 1024)]
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
        {
            TempData["Error"] = "Vui lòng chọn file.";
            return RedirectToAction(nameof(Index));
        }

        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return RedirectToAction("Login", "Account");

        // Check quyền truy cập subject
        bool canAccess = await _permissionService.CanUserAccessSubject(userId, subjectId);
        if (!canAccess)
            return Forbid();

        try
        {
            var webRootPath = _env.WebRootPath;

            var dto = await _documentService.UploadAsync(file, title, subjectId, webRootPath);

            // **Thêm: kiểm tra duplicate**
            string fileHash = await ComputeFileHashAsync(file);
            var dupCheck = await _documentService.CheckDuplicateAsync(fileHash, subjectId);

            if (dupCheck.HasDuplicate)
            {
                // Lưu vào session để modal handle
                HttpContext.Session.SetInt32("NewDocumentId", (int)dto.Id);
                HttpContext.Session.SetInt32("OldDocumentId", dupCheck.DuplicateDocumentId.Value);

                TempData["DuplicateWarning"] = $"Phát hiện tài liệu trùng: '{dupCheck.DuplicateTitle}' (upload lúc {dupCheck.DuplicateCreatedAt:dd/MM/yyyy HH:mm}). Vui lòng chọn hành động.";
                return RedirectToAction(nameof(HandleDuplicate));
            }

            TempData["Success"] = "Upload thành công! Nhấn nút ⚡ để bắt đầu embedding.";
            return RedirectToAction(nameof(Index), new { subjectId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
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
        var newDocId = HttpContext.Session.GetInt32("NewDocumentId");
        var oldDocId = HttpContext.Session.GetInt32("OldDocumentId");

        if (!newDocId.HasValue || !oldDocId.HasValue)
            return RedirectToAction(nameof(Index));

        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return RedirectToAction("Login", "Account");

        try
        {
            var dto = new DuplicateHandleDto
            {
                NewDocumentId = newDocId.Value,
                OldDocumentId = oldDocId.Value,
                Action = (DocumentDuplicateAction)action
            };

            await _documentService.HandleDuplicateAsync(dto, userId);

            HttpContext.Session.Remove("NewDocumentId");
            HttpContext.Session.Remove("OldDocumentId");

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
        bool isStaff = User.IsInRole("Lecturer") || User.IsInRole("Admin");

        if (!isStaff)
        {
            bool hasAccess = await _permissionService.CanUserAccessSubject(userId, doc.SubjectId);
            if (!hasAccess) return Forbid();
        }

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
}