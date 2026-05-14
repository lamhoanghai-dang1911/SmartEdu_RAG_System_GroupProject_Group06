using Microsoft.AspNetCore.Mvc;
using SmartEdu.Business.Interfaces;
using SmartEdu.Shared.Entities;

namespace SmartEdu.Web.Controllers
{
    public class SubjectController : Controller
    {
        private readonly ISubjectService _subjectService;

        public SubjectController(ISubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        // GET: /Subject
        public async Task<IActionResult> Index()
        {
            var subjects = await _subjectService.GetAllAsync();
            return View(subjects);
        }

        // GET: /Subject/Create
        public IActionResult Create() => View();

        // POST: /Subject/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Subject subject)
        {
            if (!ModelState.IsValid) return View(subject);
            await _subjectService.CreateAsync(subject);
            TempData["Success"] = "Tạo môn học thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Subject/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var subject = await _subjectService.GetByIdAsync(id);
            if (subject is null) return NotFound();
            return View(subject);
        }

        // POST: /Subject/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Subject subject)
        {
            if (!ModelState.IsValid) return View(subject);
            await _subjectService.UpdateAsync(subject);
            TempData["Success"] = "Cập nhật thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Subject/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _subjectService.DeleteAsync(id);
            TempData["Success"] = "Xóa môn học thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
