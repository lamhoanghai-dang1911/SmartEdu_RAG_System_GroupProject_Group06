using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartEdu.Business.Interfaces;
using SmartEdu.Data.Repositories;
using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Entities;
using SmartEdu.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using SmartEdu.Web.Extensions;

namespace SmartEdu.Web.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;
        private readonly ISubjectService _subjectService;
        private readonly IPermissionService _permissionService;
        private readonly IDocumentService _documentService;
        private readonly IFreeTierService _freeTierService;
        public ChatController(
            IChatService chatService,
            ISubjectService subjectService,
            IPermissionService permissionService,
            IDocumentService documentService,
            IFreeTierService freeTierService)
        {
            _chatService = chatService;
            _subjectService = subjectService;
            _permissionService = permissionService;
            _documentService = documentService;
            _freeTierService = freeTierService;
        }

        public async Task<IActionResult> Index(string? sessionId)
        {
            int userId = User.GetUserId();
            if (userId == 0) return Unauthorized();

            var sessions = await _chatService.GetSessionsByUserIdAsync(userId.ToString());
            var enrolledSubjects = await _subjectService.GetSubjectsByUserIdAsync(userId);

            var currentSession = sessions.FirstOrDefault(x => x.SessionId == sessionId);
            int? selectedSubjectId = currentSession?.SubjectId;

            ViewBag.Subjects = new SelectList(enrolledSubjects, "Id", "Name", selectedSubjectId);

            ViewBag.CurrentSessionTitle = currentSession?.Title ?? "Phiên mới";
            ViewBag.Sessions = sessions;
            ViewBag.CurrentSessionId = sessionId ?? Guid.NewGuid().ToString();

            IEnumerable<ChatMessageDto> history = Enumerable.Empty<ChatMessageDto>();
            if (!string.IsNullOrEmpty(sessionId))
            {
                history = await _chatService.GetHistoryAsync(sessionId, userId.ToString());
            }

            var activeSub = await _chatService.GetActiveSubscriptionAsync(userId);
            ViewBag.HasActiveSubscription = activeSub != null;

            if (activeSub != null)
            {
                ViewBag.RemainingTokens = activeSub.RemainingTokenQuota;
                ViewBag.IsFreeTier = false;
            }
            else
            {
                ViewBag.RemainingTokens = await _freeTierService.GetRemainingFreeTokensAsync(userId);
                ViewBag.IsFreeTier = true;
            }

            ViewBag.History = history;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequestDto request)
        {
            int userId = User.GetUserId();
            if (userId == 0) return Unauthorized();
            request.UserId = userId;

            // === Validate 1: câu hỏi không rỗng ===
            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest(new { error = "Câu hỏi không được để trống." });

            // === Validate 2 + 3: quyền truy cập Subject + Subject đã có tài liệu Ready ===
            if (request.SubjectId.HasValue)
            {
                bool hasAccess = await _permissionService.CanUserAccessSubject(userId, request.SubjectId.Value);
                if (!hasAccess) return Forbid();

                bool hasDocs = await _documentService.HasReadyDocumentsAsync(request.SubjectId.Value);
                if (!hasDocs)
                {
                    var activeSub = await _chatService.GetActiveSubscriptionAsync(userId);
                    int remainingQuota = activeSub != null
                        ? activeSub.RemainingTokenQuota
                        : await _freeTierService.GetRemainingFreeTokensAsync(userId);

                    return Ok(new
                    {
                        answer = "Môn học này hiện chưa có tài liệu nào được xử lý hoàn tất. Vui lòng đợi giảng viên tải lên và kích hoạt nhé! 📚",
                        sources = new List<object>(),
                        citations = new List<object>(),
                        remainingTokenQuota = remainingQuota
                    });
                }
            }

            try
            {
                // === Validate 4 + billing: subscription check + trừ token nằm bên trong Service ===
                var response = await _chatService.ProcessChatWithBillingAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> History(string sessionId)
        {
            int userId = User.GetUserId();
            if (userId == 0) return Unauthorized();

            var messages = await _chatService.GetHistoryAsync(sessionId, userId.ToString());
            return Ok(messages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSession(string sessionId)
        {
            int userId = User.GetUserId();
            if (userId == 0) return Unauthorized();

            await _chatService.DeleteSessionAsync(sessionId, userId.ToString());
            TempData["Success"] = "Đã xóa phiên chat!";
            return RedirectToAction(nameof(Index));
        }
    }
}