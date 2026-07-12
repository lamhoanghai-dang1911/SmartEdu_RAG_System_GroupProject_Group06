using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEdu.Business.Interfaces;
using SmartEdu.Shared.DTOs;
using SmartEdu.Web.Extensions;

namespace SmartEdu.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FreeTierConfigController : Controller
    {
        private readonly IFreeTierService _service;

        public FreeTierConfigController(IFreeTierService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            var configs = await _service.GetAllConfigsAsync();
            return View(configs);
        }

        [HttpGet]
        public IActionResult Create() => View(new FreeTierConfigSaveDto { TokensPerWindow = 8000, WindowHours = 24 });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FreeTierConfigSaveDto dto)
        {
            try
            {
                await _service.CreateConfigAsync(dto, User.GetUserId());
                TempData["Success"] = "Cập nhật cấu hình token miễn phí thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(dto);
            }
        }
    }
}
