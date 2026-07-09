using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartEdu.Business.Interfaces;
using SmartEdu.Shared.DTOs;
using SmartEdu.Web.Extensions;

namespace SmartEdu.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UploadConfigController : Controller
    {
        private readonly IUploadConfigService _service;
        private readonly ISubjectService _subjectService;

        public UploadConfigController(IUploadConfigService service, ISubjectService subjectService)
        {
            _service = service;
            _subjectService = subjectService;
        }

        public async Task<IActionResult> Index()
        {
            var configs = await _service.GetAllAsync();
            return View(configs);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var subjects = await _subjectService.GetAllAsync();
            ViewBag.Subjects = new SelectList(subjects, "Id", "Name");
            return View(new UploadConfigSaveDto { MaxFileSizeMB = 10 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UploadConfigSaveDto dto)
        {
            try
            {
                await _service.CreateAsync(dto, User.GetUserId());
                TempData["Success"] = "Cập nhật giới hạn upload thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                var subjects = await _subjectService.GetAllAsync();
                ViewBag.Subjects = new SelectList(subjects, "Id", "Name");
                return View(dto);
            }
        }
    }
}