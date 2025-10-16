using Fitvalle_25.Models;
using Fitvalle_25.Services;
using Microsoft.AspNetCore.Mvc;

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
                    var solicitud = req;

                    // ✅ Solo mostrar las solicitudes en estado "pending"
                    if (string.Equals(solicitud.State, "pending", StringComparison.OrdinalIgnoreCase))
                    {
                        var cliente = await _dbService.GetUserAsync($"user/{solicitud.CustomerId}", token);

                        if (cliente != null)
                            solicitudesConCliente.Add((solicitud, cliente));
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
                    // Calcular edad
                    int edad = 0;
                    if (DateTime.TryParseExact(customerData.Birthdate, "dd/MM/yyyy", null,
                        System.Globalization.DateTimeStyles.None, out DateTime nacimiento))
                    {
                        edad = DateTime.Now.Year - nacimiento.Year;
                        if (DateTime.Now < nacimiento.AddYears(edad)) edad--;
                    }

                    // 👇 Devuelve una tupla real, no un tipo anónimo
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

            // 🔹 Crear relación coach-cliente en Firebase
            var relation = new
            {
                coachId,
                customerId,
                assignedDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            await _dbService.PatchDataAsync($"coachCustomers/{coachId}/{customerId}", relation, token);

            // 🔹 Cambiar estado de la solicitud
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

            // 🔹 Obtener todas las relaciones del coach con sus alumnos
            var relations = await _dbService.GetAllAsync<CoachCustomer>($"coachCustomers/{coachId}", token);

            // 🔹 Lista que combina datos de User + Customer
            var students = new List<(User user, Customer customer)>();

            if (relations != null)
            {
                foreach (var rel in relations.Values)
                {
                    // Datos de la cuenta (nombre, email, rol, etc.)
                    var user = await _dbService.GetUserAsync($"user/{rel.CustomerId}", token);

                    // Datos físicos (altura, peso, etc.)
                    var customer = await _dbService.GetCustomerAsync($"customer/{rel.CustomerId}", token);

                    if (user != null && customer != null)
                        students.Add((user, customer));
                }
            }

            return View(students);
        }



    }
}
