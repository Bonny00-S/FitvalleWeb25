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

        // Crear rutina y redirigir a Edit
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

        // Editar rutina (mostrar sesiones y ejercicios dentro)
        public async Task<IActionResult> Edit(string id)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var routine = await _dbService.GetDataAsync<Routine>($"routine/{id}", token);
            var sessionsDict = await _dbService.GetAllAsync<Session>($"routine/{id}/sessions", token);
            var sessions = sessionsDict?.Values.ToList() ?? new List<Session>();

            // Obtener ejercicios de cada sesión
            var sessionExercises = new Dictionary<string, List<SessionExerciseViewModel>>();
            var allExercises = await _dbService.GetAllAsync<Exercise>("exercise", token);

            foreach (var session in sessions)
            {
                var exDict = await _dbService.GetAllAsync<SessionExercise>($"sessionExercises/{session.Id}", token);
                if (exDict == null || allExercises == null)
                {
                    sessionExercises[session.Id] = new List<SessionExerciseViewModel>();
                    continue;
                }

                var joined = exDict.Values
                    .Select(se =>
                    {
                        var exercise = allExercises.Values.FirstOrDefault(e => e.Id == se.ExerciseId);
                        if (exercise == null) return null;
                        return new SessionExerciseViewModel
                        {
                            Exercise = exercise,
                            Data = se
                        };
                    })
                    .Where(x => x != null)
                    .ToList()!;

                sessionExercises[session.Id] = joined;
            }

            ViewBag.RoutineId = id;
            ViewBag.Sessions = sessions;
            ViewBag.SessionExercises = sessionExercises;
            return View(routine);
        }

        // Agregar nueva sesión
        [HttpPost]
        public async Task<IActionResult> AddSession(string routineId)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var sessionId = Guid.NewGuid().ToString();
            var session = new Session
            {
                Id = sessionId,
                RoutineId = routineId,
                RegisterDate = DateTime.UtcNow
            };

            await _dbService.PatchDataAsync($"routine/{routineId}/sessions/{sessionId}", session, token);
            return RedirectToAction("Edit", new { id = routineId });
        }

        // Eliminar sesión (y ejercicios de ella)
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

        // Agregar ejercicio a sesión
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

        // Actualizar valores (sets, reps, weight, etc.)
        [HttpPost]
        public async Task<IActionResult> UpdateExerciseValues(string sessionId, string exerciseId, int? sets, int? reps, double? weight, double? speed, double? duration)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            var update = new SessionExercise
            {
                SessionId = sessionId,
                ExerciseId = exerciseId,
                Sets = sets,
                Reps = reps,
                Weight = weight,
                Speed = speed,
                Duration = duration
            };

            await _dbService.PatchDataAsync($"sessionExercises/{sessionId}/{exerciseId}", update, token);
            return Ok();
        }

        // Eliminar ejercicio
        [HttpPost]
        public async Task<IActionResult> RemoveExercise(string sessionId, string exerciseId)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            await _dbService.DeleteDataAsync($"sessionExercises/{sessionId}/{exerciseId}", token);
            return Ok();
        }
        [HttpPost]
        public async Task<IActionResult> AssignRoutine(string routineId)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            // 1) Obtener la rutina
            var routine = await _dbService.GetDataAsync<Routine>($"routine/{routineId}", token);
            if (routine == null)
            {
                TempData["Error"] = "No se encontró la rutina.";
                return RedirectToAction("Edit", new { id = routineId });
            }

            // 2) Obtener las sesiones de la rutina
            var sessionsDict = await _dbService.GetAllAsync<Session>($"routine/{routineId}/sessions", token);
            if (sessionsDict == null || sessionsDict.Count == 0)
            {
                TempData["Error"] = "La rutina no tiene sesiones.";
                return RedirectToAction("Edit", new { id = routineId });
            }

            // 3) Construir el payload con sesiones y sus ejercicios
            var sessionsMap = new Dictionary<string, object>();

            foreach (var sessionKvp in sessionsDict)
            {
                var sessionId = sessionKvp.Key;
                var session = sessionKvp.Value;

                var exercisesDict = await _dbService.GetAllAsync<SessionExercise>($"sessionExercises/{sessionId}", token);
                var exercisesList = exercisesDict?.Values.ToList() ?? new List<SessionExercise>(); // 👈 convertir a List

                sessionsMap[sessionId] = new
                {
                    id = session.Id,
                    registerDate = session.RegisterDate,
                    exercises = exercisesList
                };
            }

            // 4) Payload final a guardar en assignedRoutines/{customerId}/{routineId}
            var routinePayload = new
            {
                id = routine.Id,
                customerId = routine.CustomerId,
                coachId = routine.CoachId,
                registerDate = routine.RegisterDate,
                sessions = sessionsMap
            };

            await _dbService.PatchDataAsync(
                $"assignedRoutines/{routine.CustomerId}/{routine.Id}",
                routinePayload,
                token
            );

            // 5) (Opcional) marcar rutina como asignada
            await _dbService.PatchDataAsync($"routine/{routine.Id}", new { state = "assigned" }, token);

            TempData["Message"] = "Rutina asignada correctamente ✅";
            return RedirectToAction("MyStudents", "Coach");
        }



        // Cargar ejercicios para el modal (filtros)
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
