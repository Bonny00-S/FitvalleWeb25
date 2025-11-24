using Fitvalle_25.Models;
using Fitvalle_25.Models.Exercise;
using Fitvalle_25.Models.Viewmodels;
using Fitvalle_25.Services;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;


namespace Fitvalle_25.Controllers
{
    public class CoachController : Controller
    {
        private readonly FirebaseDbService _dbService;
        private readonly FirebaseAuthService _authService;
        private readonly ImgBBService _imgbb;
        private readonly FirebasePushService _firebasePushService;

        public CoachController(FirebaseDbService dbService, FirebaseAuthService authService, IConfiguration config, FirebasePushService firebasePushService)
        {
            _dbService = dbService;
            _authService = authService;
            var apiKey = config["ImgBB:ApiKey"];
            _imgbb = new ImgBBService(apiKey);
            _firebasePushService = firebasePushService;
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


        //public async Task<IActionResult> RequestCustomers()
        //{
        //    await SetUserInViewBag();
        //    var token = HttpContext.Session.GetString("FirebaseToken");
        //    if (string.IsNullOrEmpty(token))
        //        return RedirectToAction("Dashboard", "Coach");

        //    var requests = await _dbService.GetAllRequestsAsync(token);
        //    var solicitudesConCliente = new List<(Request solicitud, User cliente)>();

        //    if (requests != null)
        //    {
        //        foreach (var req in requests.Values)
        //        {
        //            if (string.Equals(req.State, "pending", StringComparison.OrdinalIgnoreCase))
        //            {
        //                var cliente = await _dbService.GetUserAsync($"user/{req.CustomerId}", token);
        //                if (cliente != null)
        //                    solicitudesConCliente.Add((req, cliente));
        //            }
        //        }
        //    }

        //    return View(solicitudesConCliente);
        //}
        public async Task<IActionResult> RequestCustomers()
        {
            await SetUserInViewBag();

            var token = HttpContext.Session.GetString("FirebaseToken");
            var coachId = HttpContext.Session.GetString("FirebaseUid");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(coachId))
                return RedirectToAction("Dashboard", "Coach");

            var unifiedList = new List<UnifiedRequestVM>();

            // -----------------------------------------------------------
            //      1️⃣ Solicitudes generales (Request)
            // -----------------------------------------------------------
            var generalRequests = await _dbService.GetAllRequestsAsync(token);

            if (generalRequests != null)
            {
                foreach (var req in generalRequests.Values)
                {
                    if (req.State?.ToLower() == "pending")
                    {
                        var student = await _dbService.GetUserAsync($"user/{req.CustomerId}", token);
                        if (student == null) continue;

                        unifiedList.Add(new UnifiedRequestVM
                        {
                            Id = req.Id,
                            Type = "general",
                            StudentId = req.CustomerId,
                            StudentName = student.Name,
                            Email = student.Email,                     // 👈 email agregado
                            Message = req.Description,
                            State = "Pendiente"                        // 👈 normalizado
                        });
                    }
                }
            }

            // -----------------------------------------------------------
            //      2️⃣ Solicitudes de tutoría (TutoringRequest)
            // -----------------------------------------------------------
            var tutoringRequests = await _dbService.GetAllAsync<TutoringRequest>("tutoringRequests", token);

            if (tutoringRequests != null)
            {
                foreach (var req in tutoringRequests.Values)
                {
                    if (req.CoachId == coachId && req.Status == "pending")
                    {
                        unifiedList.Add(new UnifiedRequestVM
                        {
                            Id = req.Id,
                            Type = "tutoring",
                            StudentId = req.StudentId,
                            StudentName = req.StudentName,
                            Email = req.StudentEmail,                  // 👈 email directo
                            Message = req.Message,
                            State = "Pendiente"                        // 👈 normalizado
                        });
                    }
                }
            }

            return View(unifiedList);
        }



        //public async Task<IActionResult> RequestDetail(string id)
        //{
        //    await SetUserInViewBag();
        //    var token = HttpContext.Session.GetString("FirebaseToken");
        //    if (string.IsNullOrEmpty(token))
        //        return RedirectToAction("Dashboard", "Coach");

        //    var requests = await _dbService.GetAllRequestsAsync(token);

        //    if (requests != null && requests.ContainsKey(id))
        //    {
        //        var solicitud = requests[id];
        //        var cliente = await _dbService.GetUserAsync($"user/{solicitud.CustomerId}", token);
        //        var customerData = await _dbService.GetCustomerAsync($"customer/{solicitud.CustomerId}", token);

        //        if (cliente != null && customerData != null)
        //        {
        //            int edad = 0;
        //            if (DateTime.TryParseExact(customerData.Birthdate, "dd/MM/yyyy", null,
        //                System.Globalization.DateTimeStyles.None, out DateTime nacimiento))
        //            {
        //                edad = DateTime.Now.Year - nacimiento.Year;
        //                if (DateTime.Now < nacimiento.AddYears(edad)) edad--;
        //            }

        //            return View((solicitud, cliente, customerData, edad));
        //        }
        //    }

        //    return NotFound();
        //}
        public async Task<IActionResult> RequestDetail(string id)
        {
            await SetUserInViewBag();
            var token = HttpContext.Session.GetString("FirebaseToken");

            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Dashboard", "Coach");

            // Buscar solicitud en tutoringRequests primero
            var tutoringRequests = await _dbService.GetAllAsync<TutoringRequest>("tutoringRequests", token);
            if (tutoringRequests != null && tutoringRequests.ContainsKey(id))
            {
                var req = tutoringRequests[id];

                var vm = new UnifiedRequestVM
                {
                    Id = req.Id,
                    Type = "tutoring",
                    StudentId = req.StudentId,
                    StudentName = req.StudentName,
                    Email = req.StudentEmail,
                    Message = req.Message,
                    State = req.Status
                };

                return View(vm);
            }

            // Si no está en tutoringRequests, buscar en Requests generales
            var generalRequests = await _dbService.GetAllRequestsAsync(token);
            if (generalRequests != null && generalRequests.ContainsKey(id))
            {
                var req = generalRequests[id];
                var student = await _dbService.GetUserAsync($"user/{req.CustomerId}", token);

                if (student != null)
                {
                    var vm = new UnifiedRequestVM
                    {
                        Id = req.Id,
                        Type = "general",
                        StudentId = req.CustomerId,
                        StudentName = student.Name,
                        Email = student.Email,
                        Message = req.Description,
                        State = req.State
                    };

                    return View(vm);
                }
            }

            return NotFound();
        }



        //[HttpPost]
        //public async Task<IActionResult> AcceptRequest(string requestId, string customerId)
        //{
        //    var token = HttpContext.Session.GetString("FirebaseToken");
        //    var coachId = HttpContext.Session.GetString("FirebaseUid");

        //    if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(coachId))
        //        return RedirectToAction("Login", "Auth");

        //    var relation = new
        //    {
        //        coachId,
        //        customerId,
        //        assignedDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        //    };

        //    await _dbService.PatchDataAsync($"coachCustomers/{coachId}/{customerId}", relation, token);
        //    await _dbService.PatchDataAsync($"request/{requestId}", new { state = "accepted" }, token);

        //    TempData["Message"] = "Cliente asignado correctamente.";
        //    return RedirectToAction("RequestCustomers");
        //}

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
                    var customerId = kvp.Key;
                    Console.WriteLine($" Revisando alumno: {customerId}");

                    // 1️⃣ Obtener user básico
                    var (userBasic, _, _) = await GetFullUserDataAsync(customerId, token);
                    if (userBasic == null) continue;

                    // 2️⃣ Obtener user completo EN LA TABLA CORRECTA (users/{id})
                    var userExtended = await _dbService.GetDataAsync<UserExtended>($"users/{customerId}", token);

                    // 3️⃣ Obtener el customer
                    var customer = await _dbService.GetCustomerAsync($"customer/{customerId}", token);
                    if (customer == null) continue;

                    // 4️⃣ Resolver el avatar REAL
                    string avatarFinal = "/images/iconUser.png";

                    if (userExtended != null && !string.IsNullOrEmpty(userExtended.Avatar))
                    {
                        var base64 = await _dbService.GetAvatarBase64Async(userExtended.Avatar, token);

                        if (!string.IsNullOrEmpty(base64))
                        {
                            avatarFinal = base64;
                            Console.WriteLine($"Avatar de {userBasic.Name}: OK ({base64.Substring(0, 30)}...)");
                        }
                        else
                        {
                            Console.WriteLine($"Avatar de {userBasic.Name} es null.");
                        }
                    }
                    else if (!string.IsNullOrEmpty(userBasic.PhotoUrl))
                    {
                        avatarFinal = userBasic.PhotoUrl; // Fallback Google/Facebook
                    }

                    // 5️⃣ Ver rutina
                    var assignedDict = await _dbService.GetAllAsync<object>($"assignedRoutines/{customerId}", token);
                    bool hasRoutine = assignedDict != null && assignedDict.Count > 0;

                    students.Add((userBasic, customer, hasRoutine, avatarFinal));
                }
            }

            Console.WriteLine("========== ✅ FIN DEBUG ==========");

            return View(students);
        }





        //[HttpPost]
        //public async Task<IActionResult> RemoveStudent(string customerId)
        //{
        //    var token = HttpContext.Session.GetString("FirebaseToken");
        //    var coachId = HttpContext.Session.GetString("FirebaseUid");

        //    if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(coachId))
        //        return RedirectToAction("Login", "Auth");

        //    await _dbService.DeleteDataAsync($"coachCustomers/{coachId}/{customerId}", token);
        //    await _dbService.DeleteDataAsync($"assignedRoutines/{customerId}", token);

        //    TempData["Message"] = "Has dejado de asesorar al alumno.";
        //    return RedirectToAction("MyStudents");
        //}
        [HttpPost]
        public async Task<IActionResult> RemoveStudent(string customerId, string reason)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            var coachId = HttpContext.Session.GetString("FirebaseUid");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(coachId))
                return RedirectToAction("Login", "Auth");

            // 1️⃣ Guardar el motivo en Firebase
            var stopData = new
            {
                coachId,
                customerId,
                reason = reason,
                date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                seen = false   // 👈 nuevo atributo
            };


            await _dbService.PatchDataAsync(
                $"coachStops/{coachId}/{customerId}",
                stopData,
                token
            );

            // 2️⃣ Eliminar relación
            await _dbService.DeleteDataAsync($"coachCustomers/{coachId}/{customerId}", token);

            // 3️⃣ Eliminar rutina
            await _dbService.DeleteDataAsync($"assignedRoutines/{customerId}", token);

            // 4️⃣ Notificar al alumno (si tienes FCM token)
            var extras = await _dbService.GetAllAsync<object>($"user/{customerId}", token);
            if (extras != null && extras.TryGetValue("fcmToken", out var fcmObj))
            {
                var fcmToken = fcmObj?.ToString();
                if (!string.IsNullOrEmpty(fcmToken))
                {
                    await _firebasePushService.SendNotificationAsync(
                        fcmToken,
                        "Tu coach dejó de asesorarte",
                        reason
                    );
                }
            }

            TempData["MessageTutor"] = "Has dejado de asesorar al alumno.";
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

            /////NUEVO
            // 🔹 Datos para el gráfico (peso actual vs meta)
            double currentWeight = 0;
            double goalWeight = 0;
            double progressPercent = 0;
            
            // Convertir peso actual
            // Convertir peso actual
            if (double.TryParse(customer.Weight, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedWeight))
                currentWeight = parsedWeight;

            // Convertir peso meta
            if (double.TryParse(customer.GoalWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedGoal))
                goalWeight = parsedGoal;


            // Porcentaje simple hacia meta
            if (goalWeight > 0 && currentWeight > 0)
            {
                if (goalWeight > currentWeight)
                    progressPercent = Math.Round((currentWeight / goalWeight) * 100, 2);
                else
                    progressPercent = Math.Round((goalWeight / currentWeight) * 100, 2);
            }



            // 🔥 Obtener completedSessions desde Firebase
            var completedSessionsDict = await _dbService.GetAllAsync<CompletedSession>("completedSessions", token);

            var completed = completedSessionsDict?
                .Values
                .Where(x => x.CustomerId == customer.Id)
                .OrderBy(x => x.DateFinished)
                .ToList()
                ?? new List<CompletedSession>();


            // 🔥 Peso inicial para simular evolución
            double simWeight = currentWeight;
            List<double> simulatedWeights = new();
            List<string> dates = new();

            foreach (var session in completed)
            {
                simWeight += 0.3; // tu fórmula temporal
                simulatedWeights.Add(Math.Round(simWeight, 1));

                dates.Add(session.DateFinished.ToString("dd/MM"));
            }


            // ViewBags para la gráfica
            ViewBag.WeightValues = simulatedWeights;
            ViewBag.WeightDates = dates;
            ViewBag.CurrentWeight = currentWeight;
            ViewBag.GoalWeight = goalWeight; // ya es double



            // 🔥 Cálculo REAL del progreso
            double progress = 0;

            if (goalWeight > 0)
            {
                progress = ((ViewBag.CurrentWeight - currentWeight) / (goalWeight - currentWeight)) * 100;
            }

            //ViewBag.ProgressPercent = Math.Round(progress, 2);


            ViewBag.User1 = user;
            ViewBag.Customer = customer;
            ViewBag.Routine = routine;
            ViewBag.Sessions = sessionsDict.Values.ToList();
            ViewBag.SessionExercises = sessionExercises;
       
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

        [HttpPost]
        public async Task<IActionResult> AcceptRequest(string requestId, string studentId)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            var coachId = HttpContext.Session.GetString("FirebaseUid");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(coachId))
                return RedirectToAction("Login", "Auth");

            // 1️⃣ Cambiar estado de la solicitud
            await _dbService.PatchDataAsync(
                $"tutoringRequests/{requestId}",
                new
                {
                    status = "accepted",
                    responseDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                },
                token
            );

            // 2️⃣ Crear relación coach → alumno
            var relation = new
            {
                coachId,
                studentId,
                assignedDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            await _dbService.PatchDataAsync(
                $"coachCustomers/{coachId}/{studentId}",
                relation,
                token
            );

            // 3️⃣ Notificación Push
            var extras = await _dbService.GetAllAsync<object>($"user/{studentId}", token);

            if (extras != null && extras.TryGetValue("fcmToken", out var fcmObj))
            {
                string fcmToken = fcmObj?.ToString();

                if (!string.IsNullOrEmpty(fcmToken))
                {
                    await _firebasePushService.SendNotificationAsync(
                        fcmToken,
                        "Solicitud aceptada",
                        "Tu coach ha aceptado entrenarte 🎉"
                    );
                }
            }

            TempData["MessageReq"] = "Solicitud aceptada correctamente.";

            return RedirectToAction("RequestCustomers");
        }

        [HttpPost]
        public async Task<IActionResult> RejectRequest(string requestId, string studentId)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            var coachId = HttpContext.Session.GetString("FirebaseUid");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(coachId))
                return RedirectToAction("Login", "Auth");

            // 1️⃣ Cambiar estado
            await _dbService.PatchDataAsync(
                $"tutoringRequests/{requestId}",
                new
                {
                    status = "rejected",
                    responseDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                },
                token
            );

            // 2️⃣ Notificación Push
            var extras = await _dbService.GetAllAsync<object>($"user/{studentId}", token);

            if (extras != null && extras.TryGetValue("fcmToken", out var fcmObj))
            {
                string fcmToken = fcmObj?.ToString();

                if (!string.IsNullOrEmpty(fcmToken))
                {
                    await _firebasePushService.SendNotificationAsync(
                        fcmToken,
                        "Solicitud rechazada",
                        "El coach no puede asesorarte por el momento."
                    );
                }
            }

            TempData["MessageReq"] = "Solicitud rechazada.";
            return RedirectToAction("RequestCustomers");
        }






    }
}
