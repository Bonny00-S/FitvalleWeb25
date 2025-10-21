using Fitvalle_25.Models;
using Fitvalle_25.Models.Exercise;
using Fitvalle_25.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Fitvalle_25.Controllers
{
    public class CoachController : Controller
    {
        private readonly FirebaseDbService _dbService;
        private readonly FirebaseAuthService _authService;

        public CoachController(FirebaseDbService dbService, FirebaseAuthService authService)
        {
            _dbService = dbService;
            _authService = authService;
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public async Task<IActionResult> RequestCustomers()
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Dashboard", "Coach");

            var requests = await _dbService.GetAllRequestsAsync(token);
            var solicitudesConCliente = new List<(Request solicitud, User cliente)>();

            if (requests != null)
            {
                foreach (var req in requests.Values)
                {
                    if (string.Equals(req.State, "pending", StringComparison.OrdinalIgnoreCase))
                    {
                        var cliente = await _dbService.GetUserAsync($"user/{req.CustomerId}", token);
                        if (cliente != null)
                            solicitudesConCliente.Add((req, cliente));
                    }
                }
            }

            return View(solicitudesConCliente);
        }

        public async Task<IActionResult> RequestDetail(string id)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Dashboard", "Coach");

            var requests = await _dbService.GetAllRequestsAsync(token);

            if (requests != null && requests.ContainsKey(id))
            {
                var solicitud = requests[id];
                var cliente = await _dbService.GetUserAsync($"user/{solicitud.CustomerId}", token);
                var customerData = await _dbService.GetCustomerAsync($"customer/{solicitud.CustomerId}", token);

                if (cliente != null && customerData != null)
                {
                    int edad = 0;
                    if (DateTime.TryParseExact(customerData.Birthdate, "dd/MM/yyyy", null,
                        System.Globalization.DateTimeStyles.None, out DateTime nacimiento))
                    {
                        edad = DateTime.Now.Year - nacimiento.Year;
                        if (DateTime.Now < nacimiento.AddYears(edad)) edad--;
                    }

                    return View((solicitud, cliente, customerData, edad));
                }
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> AcceptRequest(string requestId, string customerId)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            var coachId = HttpContext.Session.GetString("FirebaseUid");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(coachId))
                return RedirectToAction("Login", "Auth");

            var relation = new
            {
                coachId,
                customerId,
                assignedDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            await _dbService.PatchDataAsync($"coachCustomers/{coachId}/{customerId}", relation, token);
            await _dbService.PatchDataAsync($"request/{requestId}", new { state = "accepted" }, token);

            TempData["Message"] = "Cliente asignado correctamente.";
            return RedirectToAction("RequestCustomers");
        }

        public async Task<IActionResult> MyStudents()
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            var coachId = HttpContext.Session.GetString("FirebaseUid");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(coachId))
                return RedirectToAction("Login", "Auth");

            var relations = await _dbService.GetAllAsync<object>($"coachCustomers/{coachId}", token);
            var students = new List<(User user, Customer customer, bool hasRoutine)>();

            Console.WriteLine("========== 🔍 DEBUG MyStudents() ==========");

            if (relations != null)
            {
                foreach (var kvp in relations)
                {
                    // 🔹 kvp.Key = customerId
                    var customerId = kvp.Key;

                    Console.WriteLine($"🧩 Revisando alumno (clave nodo): {customerId}");

                    var user = await _dbService.GetUserAsync($"user/{customerId}", token);
                    var customer = await _dbService.GetCustomerAsync($"customer/{customerId}", token);

                    // 🔍 Verificar existencia de rutina asignada
                    var assignedDict = await _dbService.GetAllAsync<object>($"assignedRoutines/{customerId}", token);

                    if (assignedDict == null)
                    {
                        Console.WriteLine($"❌ No existe nodo assignedRoutines/{customerId}");
                    }
                    else if (assignedDict.Count == 0)
                    {
                        Console.WriteLine($"⚠️ Nodo vacío assignedRoutines/{customerId}");
                    }
                    else
                    {
                        Console.WriteLine($"✅ Rutina encontrada para {customerId} → {assignedDict.Count} elementos");
                    }

                    bool hasRoutine = assignedDict != null && assignedDict.Count > 0;

                    if (user != null && customer != null)
                        students.Add((user, customer, hasRoutine));
                }

            }
            else
            {
                Console.WriteLine("⚠️ No se encontró ninguna relación coachCustomers para este coach.");
            }

            Console.WriteLine("========== ✅ FIN DEBUG ==========");

            return View(students);
        }


        [HttpPost]
        public async Task<IActionResult> RemoveStudent(string customerId)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            var coachId = HttpContext.Session.GetString("FirebaseUid");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(coachId))
                return RedirectToAction("Login", "Auth");

            await _dbService.DeleteDataAsync($"coachCustomers/{coachId}/{customerId}", token);
            await _dbService.DeleteDataAsync($"assignedRoutines/{customerId}", token);

            TempData["Message"] = "Has dejado de asesorar al alumno.";
            return RedirectToAction("MyStudents");
        }
        [HttpGet]
        public async Task<IActionResult> StudentProgress(string customerId)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            // ✅ Obtener datos básicos del alumno
            var user = await _dbService.GetUserAsync($"user/{customerId}", token);
            var customer = await _dbService.GetCustomerAsync($"customer/{customerId}", token);

            // ✅ Buscar rutina asignada
            var assignedDict = await _dbService.GetAllAsync<Routine>($"assignedRoutines/{customerId}", token);
            if (assignedDict == null || assignedDict.Count == 0)
            {
                TempData["Error"] = "El alumno aún no tiene rutina asignada.";
                return RedirectToAction("MyStudents");
            }

            var routine = assignedDict.Values.First();
            var routineId = assignedDict.Keys.First();

            // ✅ Obtener sesiones y ejercicios
            var sessionsDict = await _dbService.GetAllAsync<Session>($"assignedRoutines/{customerId}/{routineId}/sessions", token);
            var allExercises = await _dbService.GetAllAsync<Exercise>("exercise", token);

            var sessionExercises = new Dictionary<string, List<(Exercise exercise, SessionExercise data)>>();

            foreach (var s in sessionsDict.Values)
            {
                var exDict = await _dbService.GetAllAsync<SessionExercise>($"sessionExercises/{s.Id}", token);
                var list = new List<(Exercise, SessionExercise)>();

                if (exDict != null)
                {
                    foreach (var ex in exDict.Values)
                    {
                        var exInfo = allExercises?.Values.FirstOrDefault(e => e.Id == ex.ExerciseId);
                        if (exInfo != null)
                            list.Add((exInfo, ex));
                    }
                }

                sessionExercises[s.Id] = list;
            }

            // 🔹 Datos para el gráfico (peso actual vs meta)
            double currentWeight = 0;
            double goalWeight = 0;

            if (double.TryParse(customer.Weight, out var parsedWeight))
                currentWeight = parsedWeight;

            if (double.TryParse(customer.GoalWeight, out var parsedGoal))
                goalWeight = parsedGoal;

            double progressPercent = (goalWeight > 0 && currentWeight > 0)
                ? Math.Round((goalWeight / currentWeight) * 100, 2)
                : 0;

            

            ViewBag.User = user;
            ViewBag.Customer = customer;
            ViewBag.Routine = routine;
            ViewBag.Sessions = sessionsDict.Values.ToList();
            ViewBag.SessionExercises = sessionExercises;
            ViewBag.CurrentWeight = currentWeight;
            ViewBag.GoalWeight = goalWeight;
            ViewBag.ProgressPercent = progressPercent;

            return View();
        }

    }
}
