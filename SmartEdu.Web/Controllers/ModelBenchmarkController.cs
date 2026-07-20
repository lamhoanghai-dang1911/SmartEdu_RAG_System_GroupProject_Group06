using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEdu.Business.Interfaces;
using SmartEdu.Shared.DTOs;
using SmartEdu.Web.Extensions;

namespace SmartEdu.Web.Controllers
{
    [Authorize(Roles = "Admin, Lecturer")]
    public class ModelBenchmarkController : Controller
    {
        private readonly IModelBenchmarkService _benchmarkService;
        private readonly ISubjectService _subjectService;

        public ModelBenchmarkController(
            IModelBenchmarkService benchmarkService,
            ISubjectService subjectService)
        {
            _benchmarkService = benchmarkService;
            _subjectService = subjectService;
        }

        // Trang giao diện benchmark
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Admin"))
            {
                var allSubjects = await _subjectService.GetAllAsync();
                return View(allSubjects);
            }

            var lecturerId = User.GetUserId();

            var lecturerSubjects =
                await _subjectService.GetSubjectsByLecturerIdAsync(
                    lecturerId);

            return View(lecturerSubjects);
        }

        // Benchmark 2 embedding model
        [HttpPost]
        public async Task<IActionResult> CompareEmbedding(
            [FromBody] EmbeddingBenchmarkRequestDto request)
        {
            try
            {
                var result =
                    await _benchmarkService
                        .CompareEmbeddingModelsAsync(request);

                return Json(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // Benchmark Gemini, Groq và OpenRouter
        [HttpPost]
        public async Task<IActionResult> CompareChat(
            [FromBody] ChatModelBenchmarkRequestDto request)
        {
            try
            {
                var result =
                    await _benchmarkService
                        .CompareChatModelsAsync(request);

                return Json(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}
