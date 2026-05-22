using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartEdu.Business.Interfaces;
using SmartEdu.Shared.DTOs;

namespace SmartEdu.Web.Controllers
{
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;
        private readonly ISubjectService _subjectService;

        public ChatController(IChatService chatService, ISubjectService subjectService)
        {
            _chatService = chatService;
            _subjectService = subjectService;
        }

        // GET: /Chat
        public async Task<IActionResult> Index(string? sessionId)
        {
            var subjects = await _subjectService.GetAllAsync();
            var sessions = await _chatService.GetAllSessionsAsync();

            // Nếu có sessionId, cố gắng tìm SubjectId tương ứng để chọn trong dropdown
            int? selectedSubjectId = null;
            if (!string.IsNullOrEmpty(sessionId))
            {
                var sess = sessions.FirstOrDefault(x => x.SessionId == sessionId);
                if (sess != null) selectedSubjectId = sess.SubjectId;
            }

            ViewBag.Subjects = new SelectList(subjects, "Id", "Name", selectedSubjectId);
            ViewBag.Sessions = sessions;
            ViewBag.CurrentSessionId = sessionId ?? Guid.NewGuid().ToString();

            // Load lịch sử nếu có sessionId
            IEnumerable<ChatMessageDto> history = Enumerable.Empty<ChatMessageDto>();
            if (!string.IsNullOrEmpty(sessionId))
                history = await _chatService.GetHistoryAsync(sessionId);

            ViewBag.History = history;
            return View();
        }

        // POST: /Chat/Ask (AJAX)
        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest(new { error = "Câu hỏi không được để trống." });

            try
            {
                var response = await _chatService.AskAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: /Chat/History/{sessionId} (AJAX)
        [HttpGet]
        public async Task<IActionResult> History(string sessionId)
        {
            var messages = await _chatService.GetHistoryAsync(sessionId);
            return Ok(messages);
        }

        // POST: /Chat/DeleteSession
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSession(string sessionId)
        {
            await _chatService.DeleteSessionAsync(sessionId);
            TempData["Success"] = "Đã xóa phiên chat!";
            return RedirectToAction(nameof(Index));
        }
    }
}
