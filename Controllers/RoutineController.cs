using Fitvalle_25.Models;
using Fitvalle_25.Models.Exercise;
using Fitvalle_25.Models.Exercise.Viewmodels;
using Fitvalle_25.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Fitvalle_25.Controllers
{
    public class RoutineController : Controller
    {
        private readonly FirebaseDbService _dbService;

        public RoutineController(FirebaseDbService dbService)
        {
            _dbService = dbService;
        }

        // ✅ Crear rutina
        public async Task<IActionResult> Create(string customerId)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            var coachId = HttpContext.Session.GetString("FirebaseUid");
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(coachId))
                return RedirectToAction("Login", "Auth");

            var routine = new Routine
            {
                Id = Guid.NewGuid().ToString(),
                CustomerId = customerId,
                CoachId = coachId,
                RegisterDate = DateTime.UtcNow
            };

            await _dbService.PatchDataAsync($"routine/{routine.Id}", routine, token);
            return RedirectToAction("Edit", new { id = routine.Id });
        }

        // ✅ Editar rutina
        public async Task<IActionResult> Edit(string id)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var routine = await _dbService.GetDataAsync<Routine>($"routine/{id}", token);
            var sessionsDict = await _dbService.GetAllAsync<Session>($"routine/{id}/sessions", token);
            var sessions = sessionsDict?.Values.ToList() ?? new List<Session>();

            var allExercises = await _dbService.GetAllAsync<Exercise>("exercise", token);
            var sessionExercises = new Dictionary<string, List<SessionExerciseViewModel>>();

            foreach (var session in sessions)
            {
                var exDict = await _dbService.GetAllAsync<SessionExercise>($"sessionExercises/{session.Id}", token);
                if (exDict == null || allExercises == null)
                {
                    sessionExercises[session.Id] = new List<SessionExerciseViewModel>();
                    continue;
                }

                sessionExercises[session.Id] = exDict.Values
                    .Select(se =>
                    {
                        var ex = allExercises.Values.FirstOrDefault(e => e.Id == se.ExerciseId);
                        if (ex == null) return null;
                        return new SessionExerciseViewModel { Exercise = ex, Data = se };
                    })
                    .Where(x => x != null).ToList()!;
            }

            var muscles = await _dbService.GetAllAsync<TargetMuscle>("targetMuscles", token);
            var types = await _dbService.GetAllAsync<ExerciseType>("exerciseTypes", token);

            ViewBag.Muscles = muscles?.Values.ToList() ?? new List<TargetMuscle>();
            ViewBag.Types = types?.Values.ToList() ?? new List<ExerciseType>();
            ViewBag.RoutineId = id;
            ViewBag.Sessions = sessions;
            ViewBag.SessionExercises = sessionExercises;
            ViewBag.IsAssignedRoutine = false;

            return View(routine);
        }

        // ✅ Agregar sesión
        [HttpPost]
        public async Task<IActionResult> AddSession(string routineId)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var session = new Session
            {
                Id = Guid.NewGuid().ToString(),
                RoutineId = routineId,
                RegisterDate = DateTime.UtcNow
            };

            await _dbService.PatchDataAsync($"routine/{routineId}/sessions/{session.Id}", session, token);
            return RedirectToAction("Edit", new { id = routineId });
        }

        // ✅ Eliminar sesión
        [HttpPost]
        public async Task<IActionResult> DeleteSession(string routineId, string sessionId)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            await _dbService.DeleteDataAsync($"routine/{routineId}/sessions/{sessionId}", token);
            await _dbService.DeleteDataAsync($"sessionExercises/{sessionId}", token);
            return RedirectToAction("Edit", new { id = routineId });
        }

        // ✅ Agregar ejercicio
        [HttpPost]
        public async Task<IActionResult> AddExercise(string routineId, string sessionId, string exerciseId)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            var newExercise = new SessionExercise
            {
                SessionId = sessionId,
                ExerciseId = exerciseId,
                Sets = 3,
                Reps = 12
            };

            await _dbService.PatchDataAsync($"sessionExercises/{sessionId}/{exerciseId}", newExercise, token);
            return Ok();
        }

        // ✅ Actualizar valores
        [HttpPost]
        public async Task<IActionResult> UpdateExerciseValues(
            string sessionId, string exerciseId,
            string sets, string reps, string weight, string speed, string duration)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            Console.WriteLine($"📩 UpdateExerciseValues → session: {sessionId}, ex: {exerciseId}");

            var existing = await _dbService.GetDataAsync<SessionExercise>($"sessionExercises/{sessionId}/{exerciseId}", token);
            if (existing == null)
            {
                Console.WriteLine($" Ejercicio {exerciseId} no encontrado");
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(sets) && int.TryParse(sets, out var s)) existing.Sets = s;
            if (!string.IsNullOrWhiteSpace(reps) && int.TryParse(reps, out var r)) existing.Reps = r;
            if (!string.IsNullOrWhiteSpace(weight) && double.TryParse(weight, out var w)) existing.Weight = w;
            if (!string.IsNullOrWhiteSpace(speed) && double.TryParse(speed, out var sp)) existing.Speed = sp;
            if (!string.IsNullOrWhiteSpace(duration) && double.TryParse(duration, out var d)) existing.Duration = d;

            await _dbService.PatchDataAsync($"sessionExercises/{sessionId}/{exerciseId}", existing, token);
            Console.WriteLine($" Actualizado {exerciseId} correctamente");
            return Ok();
        }

        // ✅ Eliminar ejercicio
        [HttpPost]
        public async Task<IActionResult> RemoveExercise(string sessionId, string exerciseId)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            await _dbService.DeleteDataAsync($"sessionExercises/{sessionId}/{exerciseId}", token);
            return Ok();
        }

        // ✅ Editar rutina asignada
        public async Task<IActionResult> EditAssignedRoutine(string customerId)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var assignedDict = await _dbService.GetAllAsync<Routine>($"assignedRoutines/{customerId}", token);
            if (assignedDict == null || assignedDict.Count == 0)
            {
                TempData["Error"] = "El alumno no tiene rutina asignada.";
                return RedirectToAction("MyStudents", "Coach");
            }

            var routine = assignedDict.Values.First();
            var routineId = assignedDict.Keys.First();

            var sessionsDict = await _dbService.GetAllAsync<Session>($"assignedRoutines/{customerId}/{routineId}/sessions", token);
            var sessions = sessionsDict?.Values.ToList() ?? new List<Session>();
            var allExercises = await _dbService.GetAllAsync<Exercise>("exercise", token);
            var sessionExercises = new Dictionary<string, List<SessionExerciseViewModel>>();

            foreach (var session in sessions)
            {
                var exDict = await _dbService.GetAllAsync<SessionExercise>($"sessionExercises/{session.Id}", token);
                if (exDict == null)
                {
                    sessionExercises[session.Id] = new List<SessionExerciseViewModel>();
                    continue;
                }

                sessionExercises[session.Id] = exDict.Values
                    .Select(se =>
                    {
                        var e = allExercises.Values.FirstOrDefault(x => x.Id == se.ExerciseId);
                        return e == null ? null : new SessionExerciseViewModel { Exercise = e, Data = se };
                    })
                    .Where(x => x != null).ToList()!;
            }

            var muscles = await _dbService.GetAllAsync<TargetMuscle>("targetMuscles", token);
            var types = await _dbService.GetAllAsync<ExerciseType>("exerciseTypes", token);

            ViewBag.Muscles = muscles?.Values.ToList() ?? new List<TargetMuscle>();
            ViewBag.Types = types?.Values.ToList() ?? new List<ExerciseType>();
            ViewBag.RoutineId = routineId;
            ViewBag.CustomerId = customerId;
            ViewBag.Sessions = sessions;
            ViewBag.SessionExercises = sessionExercises;
            ViewBag.IsAssignedRoutine = true;

            return View("Edit", routine);
        }
        // ✅ Asignar rutina al alumno (como estaba antes)
        // ✅ Asignar rutina al alumno (con validación)
        [HttpPost]
        public async Task<IActionResult> AssignRoutine(string routineId)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var routine = await _dbService.GetDataAsync<Routine>($"routine/{routineId}", token);
            if (routine == null)
            {
                TempData["Error"] = "No se encontró la rutina.";
                return RedirectToAction("Edit", new { id = routineId });
            }

            var sessionsDict = await _dbService.GetAllAsync<Session>($"routine/{routineId}/sessions", token);
            if (sessionsDict == null || sessionsDict.Count < 2)
            {
                TempData["Error"] = " La rutina debe tener al menos 2 sesiones antes de asignarla.";
                return RedirectToAction("Edit", new { id = routineId });
            }

            // 🔹 Validar ejercicios dentro de cada sesión
            foreach (var session in sessionsDict.Values)
            {
                var exercisesDict = await _dbService.GetAllAsync<SessionExercise>($"sessionExercises/{session.Id}", token);
                int count = exercisesDict?.Count ?? 0;

                if (count < 4)
                {
                    TempData["Error"] = $" La sesión creada el {session.RegisterDate:dd/MM/yyyy} debe tener al menos 4 ejercicios.";
                    return RedirectToAction("Edit", new { id = routineId });
                }
            }

            // 🔹 Si todo está bien, armar el payload
            var sessionsMap = new Dictionary<string, object>();
            foreach (var s in sessionsDict)
            {
                var exercisesDict = await _dbService.GetAllAsync<SessionExercise>($"sessionExercises/{s.Key}", token);
                var exercisesList = exercisesDict?.Values.ToList() ?? new List<SessionExercise>();

                sessionsMap[s.Key] = new
                {
                    id = s.Value.Id,
                    registerDate = s.Value.RegisterDate,
                    exercises = exercisesList
                };
            }

            var routinePayload = new
            {
                id = routine.Id,
                customerId = routine.CustomerId,
                coachId = routine.CoachId,
                registerDate = routine.RegisterDate,
                sessions = sessionsMap
            };

            await _dbService.PatchDataAsync($"assignedRoutines/{routine.CustomerId}/{routine.Id}", routinePayload, token);
            await _dbService.PatchDataAsync($"routine/{routine.Id}", new { state = "assigned" }, token);

            TempData["Message"] = " Rutina asignada correctamente.";
            return RedirectToAction("MyStudents", "Coach");
        }



        // ✅ Guardar cambios finales
        [HttpPost]
        public async Task<IActionResult> UpdateAssignedRoutine(string customerId, string routineId)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var sessionsDict = await _dbService.GetAllAsync<Session>($"assignedRoutines/{customerId}/{routineId}/sessions", token);
            if (sessionsDict == null || sessionsDict.Count < 2)
            {
                TempData["Error"] = " La rutina debe tener al menos 2 sesiones antes de guardar.";
                return RedirectToAction("EditAssignedRoutine", new { customerId });
            }

            // 🔹 Validar ejercicios por sesión
            foreach (var session in sessionsDict.Values)
            {
                var exercisesDict = await _dbService.GetAllAsync<SessionExercise>($"sessionExercises/{session.Id}", token);
                int count = exercisesDict?.Count ?? 0;

                if (count < 4)
                {
                    TempData["Error"] = $" La sesión ({session.Id}) tiene solo {count} ejercicios. Debe tener al menos 4.";
                    return RedirectToAction("EditAssignedRoutine", new { customerId });
                }
            }

            // 🔹 Actualizar los ejercicios dentro de la rutina asignada
            foreach (var s in sessionsDict)
            {
                var exDict = await _dbService.GetAllAsync<SessionExercise>($"sessionExercises/{s.Key}", token);
                if (exDict == null) continue;

                await _dbService.PatchDataAsync(
                    $"assignedRoutines/{customerId}/{routineId}/sessions/{s.Key}/exercises",
                    exDict, token);
            }

            TempData["Message"] = " Cambios guardados correctamente.";
            return RedirectToAction("MyStudents", "Coach");
        }


        // ✅ Catálogo
        [HttpGet]
        public async Task<IActionResult> GetExercises()
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            var exercises = await _dbService.GetAllAsync<Exercise>("exercise", token);
            var muscles = await _dbService.GetAllAsync<TargetMuscle>("targetMuscles", token);
            var types = await _dbService.GetAllAsync<ExerciseType>("exerciseTypes", token);

            if (exercises == null) return Json(new List<object>());

            var result = exercises.Values.Select(e => new
            {
                id = e.Id,
                name = e.Name,
                description = e.Description,
                imageUrl = e.ImageUrl,
                muscle = muscles?.Values.FirstOrDefault(m => m.Id == e.MuscleID)?.Name ?? "",
                type = types?.Values.FirstOrDefault(t => t.Id == e.TypeID)?.Name ?? ""
            });

            return Json(result);
        }
    }
}
