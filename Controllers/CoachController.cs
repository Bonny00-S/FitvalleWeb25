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
                foreach (var req in requests)
                {
                    var solicitud = req.Value; // el objeto Request
                    var cliente = await _dbService.GetUserAsync($"user/{solicitud.CustomerId}", token);

                    if (cliente != null)
                        solicitudesConCliente.Add((solicitud, cliente));
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

                if (cliente != null)
                {
                    var detalle = (solicitud, cliente);
                    return View(detalle);
                }
            }

            return NotFound();
        }
    }
}
