using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartEdu.Business.Interfaces;
using SmartEdu.Data.Repositories;
using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Entities;
using SmartEdu.Shared.Enums;

namespace SmartEdu.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ChunkingConfigController : Controller
    {
        private readonly IChunkingConfigService _configService;
        private readonly IRepository<Subject> _subjectRepo;

        public ChunkingConfigController(IChunkingConfigService configService, IRepository<Subject> subjectRepo)
        {
            _configService = configService;
            _subjectRepo = subjectRepo;
        }

        public async Task<IActionResult> Index()
        {
            var configs = await _configService.GetAllAsync();
            return View(configs);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var subjects = await _subjectRepo.GetAllAsync(s => !s.IsDeleted);
            ViewBag.Subjects = new SelectList(subjects, "Id", "Name");
            return View(new ChunkingConfigSaveDto
            {
                ChunkSize = 800,
                ChunkOverlap = 80,
                Strategy = SmartEdu.Shared.Enums.ChunkingStrategy.FixedSize
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ChunkingConfigSaveDto dto)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account");
            dto.Strategy = ChunkingStrategy.FixedSize;

            try
            {
                await _configService.SaveAsync(dto, userId);
                TempData["Success"] = "Cập nhật cấu hình chunking thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                var subjects = await _subjectRepo.GetAllAsync(s => !s.IsDeleted);
                ViewBag.Subjects = new SelectList(subjects, "Id", "Name");
                return View(dto);
            }
        }
    }
}
