using Fitvalle_25.Models;
using Fitvalle_25.Models.Exercise;
using Fitvalle_25.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fitvalle_25.Controllers
{
    public class ExerciseController : Controller
    {
        private readonly FirebaseDbService _dbService;

        public ExerciseController(FirebaseDbService dbService)
        {
            _dbService = dbService;
        }

        // 📄 LISTAR EJERCICIOS
        public async Task<IActionResult> Index(string searchString = "", int page = 1, int pageSize = 10)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var exercises = await _dbService.GetAllAsync<Exercise>("exercise", token);
            var muscles = await _dbService.GetAllAsync<TargetMuscle>("targetMuscles", token);
            var types = await _dbService.GetAllAsync<ExerciseType>("exerciseTypes", token);

            if (exercises != null)
            {
                foreach (var ex in exercises.Values)
                {
                    ex.Muscle = muscles?.Values.FirstOrDefault(m => m.Id == ex.MuscleID);
                    ex.Type = types?.Values.FirstOrDefault(t => t.Id == ex.TypeID);
                }

                // 🔍 FILTRAR por nombre o descripción
                if (!string.IsNullOrWhiteSpace(searchString))
                {
                    exercises = exercises
                        .Where(x =>
                            (x.Value.Name?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (x.Value.Description?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (x.Value.Muscle?.Name?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (x.Value.Type?.Name?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false))
                        .ToDictionary(x => x.Key, x => x.Value);
                }

                // 📄 PAGINACIÓN
                int totalItems = exercises.Count;
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var pagedExercises = exercises
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToDictionary(x => x.Key, x => x.Value);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.SearchString = searchString;

                return View(pagedExercises);
            }

            return View(new Dictionary<string, Exercise>());
        }


        // 📄 CREAR EJERCICIO (GET)
        public async Task<IActionResult> Create()
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            ViewBag.Muscles = (await _dbService.GetAllAsync<TargetMuscle>("targetMuscles", token))?.Values.ToList() ?? new();
            ViewBag.Types = (await _dbService.GetAllAsync<ExerciseType>("exerciseTypes", token))?.Values.ToList() ?? new();

            return View();
        }

        // 📩 CREAR EJERCICIO (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Exercise exercise)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                ViewBag.Muscles = (await _dbService.GetAllAsync<TargetMuscle>("targetMuscles", token))?.Values.ToList() ?? new();
                ViewBag.Types = (await _dbService.GetAllAsync<ExerciseType>("exerciseTypes", token))?.Values.ToList() ?? new();
                return View(exercise);
            }
            if (!string.IsNullOrWhiteSpace(exercise.ImageUrl))
            {
                exercise.ImageUrl = exercise.ImageUrl.Trim();

                if (!Uri.IsWellFormedUriString(exercise.ImageUrl, UriKind.Absolute))
                {
                    ModelState.AddModelError("ImageUrl", "La URL de la imagen no es válida. Debe comenzar con http:// o https://");
                    ViewBag.Muscles = (await _dbService.GetAllAsync<TargetMuscle>("targetMuscles", token))?.Values.ToList() ?? new();
                    ViewBag.Types = (await _dbService.GetAllAsync<ExerciseType>("exerciseTypes", token))?.Values.ToList() ?? new();
                    return View(exercise);
                }
            }

            exercise.Id = Guid.NewGuid().ToString();
            exercise.RegisterDate = DateTime.UtcNow;

            bool created = await _dbService.PatchDataAsync($"exercise/{exercise.Id}", exercise, token);

            if (created)
                return RedirectToAction(nameof(Index));

            ViewBag.Error = "Error al guardar el ejercicio.";
            return View(exercise);
        }

        // 📄 EDITAR EJERCICIO (GET)
        public async Task<IActionResult> Edit(string id)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var exerciseDict = await _dbService.GetAllAsync<Exercise>("exercise", token);
            var exercise = exerciseDict?.Values.FirstOrDefault(e => e.Id == id);
            if (exercise == null)
                return RedirectToAction(nameof(Index));

            ViewBag.Muscles = (await _dbService.GetAllAsync<TargetMuscle>("targetMuscles", token))?.Values.ToList() ?? new();
            ViewBag.Types = (await _dbService.GetAllAsync<ExerciseType>("exerciseTypes", token))?.Values.ToList() ?? new();

            return View(exercise);
        }

        // 📩 EDITAR EJERCICIO (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Exercise exercise)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                ViewBag.Muscles = (await _dbService.GetAllAsync<TargetMuscle>("targetMuscles", token))?.Values.ToList() ?? new();
                ViewBag.Types = (await _dbService.GetAllAsync<ExerciseType>("exerciseTypes", token))?.Values.ToList() ?? new();
                return View(exercise);
            }
            if (!string.IsNullOrWhiteSpace(exercise.ImageUrl))
            {
                exercise.ImageUrl = exercise.ImageUrl.Trim();

                if (!Uri.IsWellFormedUriString(exercise.ImageUrl, UriKind.Absolute))
                {
                    ModelState.AddModelError("ImageUrl", "La URL de la imagen no es válida. Debe comenzar con http:// o https://");
                    ViewBag.Muscles = (await _dbService.GetAllAsync<TargetMuscle>("targetMuscles", token))?.Values.ToList() ?? new();
                    ViewBag.Types = (await _dbService.GetAllAsync<ExerciseType>("exerciseTypes", token))?.Values.ToList() ?? new();
                    return View(exercise);
                }
            }

            exercise.Id = id;
            bool updated = await _dbService.PatchDataAsync($"exercise/{id}", exercise, token);

            if (updated)
                return RedirectToAction(nameof(Index));

            ViewBag.Error = "Error al actualizar el ejercicio.";
            return View(exercise);
        }

        // 🗑️ ELIMINAR EJERCICIO
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            await _dbService.DeleteDataAsync($"exercise/{id}", token);
            return RedirectToAction(nameof(Index));
        }
    }
}
