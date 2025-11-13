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
        private readonly ImgBBService _imgbb;

        public CoachController(FirebaseDbService dbService, FirebaseAuthService authService, IConfiguration config)
        {
            _dbService = dbService;
            _authService = authService;
            var apiKey = config["ImgBB:ApiKey"];
            _imgbb = new ImgBBService(apiKey);
        }

        public async Task<IActionResult> Dashboard()
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            var userId = HttpContext.Session.GetString("FirebaseUid");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Auth");

            var user = await _dbService.GetUserAsync($"user/{userId}", token);
            ViewBag.User = user;

            return View();
        }


        public async Task<IActionResult> RequestCustomers()
        {
            await SetUserInViewBag();
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
            await SetUserInViewBag();
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
            await SetUserInViewBag();
            var token = HttpContext.Session.GetString("FirebaseToken");
            var coachId = HttpContext.Session.GetString("FirebaseUid");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(coachId))
                return RedirectToAction("Login", "Auth");

            var relations = await _dbService.GetAllAsync<object>($"coachCustomers/{coachId}", token);
            var students = new List<(User user, Customer customer, bool hasRoutine, string avatar)>();


            Console.WriteLine("========== 🔍 DEBUG MyStudents() ==========");

            if (relations != null)
            {
                foreach (var kvp in relations)
                {
                    // 🔹 kvp.Key = customerId
                    var customerId = kvp.Key;

                    Console.WriteLine($" Revisando alumno (clave nodo): {customerId}");

                    var (user, avatar, fcmToken) = await GetFullUserDataAsync(customerId, token);
                    var customer = await _dbService.GetCustomerAsync($"customer/{customerId}", token);


                    // 🔍 Verificar existencia de rutina asignada
                    var assignedDict = await _dbService.GetAllAsync<object>($"assignedRoutines/{customerId}", token);

                    if (assignedDict == null)
                    {
                        Console.WriteLine($" No existe nodo assignedRoutines/{customerId}");
                    }
                    else if (assignedDict.Count == 0)
                    {
                        Console.WriteLine($" Nodo vacío assignedRoutines/{customerId}");
                    }
                    else
                    {
                        Console.WriteLine($" Rutina encontrada para {customerId} → {assignedDict.Count} elementos");
                    }

                    bool hasRoutine = assignedDict != null && assignedDict.Count > 0;

                    if (user != null && customer != null)
                        students.Add((user, customer, hasRoutine, avatar ?? user.PhotoUrl ?? "/images/iconUser.png"));

                }

            }
            else
            {
                Console.WriteLine("⚠ No se encontró ninguna relación coachCustomers para este coach.");
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
            await SetUserInViewBag();
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
            double progressPercent;

            if (double.TryParse(customer.Weight, out var parsedWeight))
                currentWeight = parsedWeight;

            if (double.TryParse(customer.GoalWeight, out var parsedGoal))
                goalWeight = parsedGoal;
            if (goalWeight > currentWeight)
            {
                 progressPercent = (goalWeight > 0 && currentWeight > 0)
                ? Math.Round((currentWeight / goalWeight) * 100, 2)
                : 0;
            }
            else
            {
                    progressPercent = (goalWeight > 0 && currentWeight > 0)
                    ? Math.Round((goalWeight / currentWeight) * 100, 2)
                    : 0;
            }
               

            

            ViewBag.User1 = user;
            ViewBag.Customer = customer;
            ViewBag.Routine = routine;
            ViewBag.Sessions = sessionsDict.Values.ToList();
            ViewBag.SessionExercises = sessionExercises;
            ViewBag.CurrentWeight = currentWeight;
            ViewBag.GoalWeight = goalWeight;
            ViewBag.ProgressPercent = progressPercent;

            return View();
        }
        [HttpGet]
        public async Task<IActionResult> ProfileCoach()
        {
            await SetUserInViewBag();
            var token = HttpContext.Session.GetString("FirebaseToken");
            var userId = HttpContext.Session.GetString("FirebaseUid");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Auth");

            // ✅ Obtener datos del coach
            var user = await _dbService.GetUserAsync($"user/{userId}", token);
            if (user == null)
                return NotFound();

            if (user.Role != "coach")
                return RedirectToAction("Dashboard", "Coach");

            // ✅ Obtener las relaciones del coach con sus alumnos
            var coachStudents = await _dbService.GetAllAsync<object>($"coachCustomers/{userId}", token);

            int activeStudents = 0;

            if (coachStudents != null)
            {
                foreach (var relation in coachStudents)
                {
                    var customerId = relation.Key;

                    // Verificar si el alumno tiene tanto un registro en "customer" como en "user"
                    var customer = await _dbService.GetCustomerAsync($"customer/{customerId}", token);
                    var student = await _dbService.GetUserAsync($"user/{customerId}", token);

                    // ✅ Solo cuenta si existe en ambas tablas
                    if (customer != null && student != null)
                    {
                        activeStudents++;
                    }
                }
            }

            ViewBag.ActiveStudents = activeStudents;
            return View(user);
        }



        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            await SetUserInViewBag();
            var token = HttpContext.Session.GetString("FirebaseToken");
            var userId = HttpContext.Session.GetString("FirebaseUid");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Auth");

            var user = await _dbService.GetUserAsync($"user/{userId}", token);
            return View(user);
        }

        //[HttpPost]
        //public async Task<IActionResult> UpdateProfile(User model, IFormFile? photo)
        //{
        //    var token = HttpContext.Session.GetString("FirebaseToken");
        //    var userId = HttpContext.Session.GetString("FirebaseUid");

        //    if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
        //        return RedirectToAction("Login", "Auth");

        //    // Obtener el usuario actual
        //    var currentUser = await _dbService.GetUserAsync($"user/{userId}", token);
        //    if (currentUser == null)
        //        return NotFound();

        //    string? base64Photo = currentUser.PhotoUrl; // Mantener la foto actual si no se cambia

        //    // 📸 Convertir imagen a Base64 si se sube una nueva
        //    if (photo != null && photo.Length > 0)
        //    {
        //        using (var ms = new MemoryStream())
        //        {
        //            await photo.CopyToAsync(ms);
        //            var bytes = ms.ToArray();
        //            var extension = Path.GetExtension(photo.FileName).ToLower();
        //            var mimeType = extension switch
        //            {
        //                ".jpg" or ".jpeg" => "image/jpeg",
        //                ".png" => "image/png",
        //                ".gif" => "image/gif",
        //                _ => "image/jpeg"
        //            };

        //            base64Photo = $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
        //        }
        //    }

        //    // 🔹 Actualizar datos en Firebase
        //    var updateData = new
        //    {
        //        name = string.IsNullOrWhiteSpace(model.Name) ? currentUser.Name : model.Name,
        //        description = model.Description ?? currentUser.Description ?? "",
        //        photoUrl = base64Photo ?? "",
        //        speciality = model.Specialty ?? currentUser.Specialty ?? ""
        //    };

        //    await _dbService.PatchDataAsync($"user/{userId}", updateData, token);

        //    TempData["Message1"] = "Perfil actualizado correctamente.";
        //    return RedirectToAction("ProfileCoach");
        //}
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(User model, IFormFile? photo)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            var userId = HttpContext.Session.GetString("FirebaseUid");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Auth");

            var currentUser = await _dbService.GetUserAsync($"user/{userId}", token);
            if (currentUser == null)
                return NotFound();

            string? photoUrl = currentUser.PhotoUrl; // Mantener la foto actual si no se cambia

            if (photo != null && photo.Length > 0)
            {
                // reemplaza con tu key
                var uploadedUrl = await _imgbb.UploadImageAsync(photo);
                if (!string.IsNullOrEmpty(uploadedUrl))
                    photoUrl = uploadedUrl;
            }

            var updateData = new
            {
                name = string.IsNullOrWhiteSpace(model.Name) ? currentUser.Name : model.Name,
                description = model.Description ?? currentUser.Description ?? "",
                photoUrl = photoUrl ?? "",
                speciality = model.Specialty ?? currentUser.Specialty ?? ""
            };

            await _dbService.PatchDataAsync($"user/{userId}", updateData, token);

            TempData["Message1"] = "Perfil actualizado correctamente.";
            return RedirectToAction("ProfileCoach");
        }

        private async Task SetUserInViewBag()
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            var userId = HttpContext.Session.GetString("FirebaseUid");

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userId))
            {
                var user = await _dbService.GetUserAsync($"user/{userId}", token);

                // dejar el user para compatibilidad con código actual
                ViewBag.User = user;

                // calcular y exponer la URL del avatar (para el layout y las vistas)
                //ViewBag.UserAvatar = UserAvatarHelper.GetAvatarUrl(user);
            }
            else
            {
                ViewBag.User = null;
                ViewBag.UserAvatar = "/images/iconUser.png";
            }
        }
        private async Task<(User user, string avatar, string fcmToken)> GetFullUserDataAsync(string userId, string token)
        {
            var user = await _dbService.GetUserAsync($"user/{userId}", token);
            var userExtra = await _dbService.GetAllAsync<object>($"user/{userId}", token);

            string avatar = null;
            string fcmToken = null;

            if (userExtra != null)
            {
                // Si existe avatar explícito
                if (userExtra.TryGetValue("avatar", out var avatarValue))
                    avatar = avatarValue?.ToString();

                // Si el fcmToken parece ser una URL (ej. empieza con https)
                if (userExtra.TryGetValue("fcmToken", out var tokenValue))
                {
                    fcmToken = tokenValue?.ToString();

                    // ⚠ Si el fcmToken es una URL, lo usamos como avatar real
                    if (!string.IsNullOrEmpty(fcmToken) && fcmToken.StartsWith("https"))
                        avatar = fcmToken;
                }
            }

            return (user, avatar, fcmToken);
        }








    }
}
