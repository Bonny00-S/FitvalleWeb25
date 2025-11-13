using Microsoft.AspNetCore.Mvc;
using Fitvalle_25.Services;

namespace Fitvalle_25.Controllers
{
    public class FirebaseSetupController : Controller
    {
        private readonly FirebaseDbService _dbService;

        public FirebaseSetupController(FirebaseDbService dbService)
        {
            _dbService = dbService;
        }

        public async Task<IActionResult> InitializeData()
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
            {
                return Content("❌ Debes iniciar sesión para ejecutar esta acción.");
            }

            bool success = await _dbService.InitializeDataAsync(token);
            return Content(success ? "✅ Datos iniciales insertados correctamente en Firebase." : "⚠️ Error al insertar datos.");
        }

    }
}
