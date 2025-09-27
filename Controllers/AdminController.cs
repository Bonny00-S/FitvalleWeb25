using Fitvalle_25.Models;
using Fitvalle_25.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fitvalle_25.Controllers
{
    public class AdminController : Controller
    {
        private readonly FirebaseDbService _dbService;
        private readonly FirebaseAuthService _authService;

        public AdminController(FirebaseDbService dbService, FirebaseAuthService authService)
        {
            _dbService = dbService;
            _authService = authService;
        }



        public IActionResult Dashboard()
        {
            ViewBag.FechaActual = DateTime.Now.ToString("dd/MM/yyyy");
            ViewBag.Anio = DateTime.Now.Year;

            // El nombre ya lo tienes con User.Identity?.Name en la vista
            return View();
        }

        public async Task<IActionResult> ManageUsers()
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var users = await _dbService.GetAllUsersAsync(token);
            return View(users);
        }

        //// 🗑️ Eliminar usuario
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string uid)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            await _dbService.DeleteDataAsync($"user/{uid}", token);
            return RedirectToAction("ManageUsers");
        }

        // ➕ Crear usuario (vista)
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string email, string password, string name, string role)
        {
            try
            {
                // 1. Preparar objeto usuario con datos básicos
                var s = new User
                {
                    Id = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Email = email,
                    Password = password, // plano para Auth
                    Name = name,
                    Role = role
                };

                // 2. Registrar en Firebase Auth
                var signupResponse = await _dbService.SignUpAsync(s);

                if (signupResponse == null || string.IsNullOrEmpty(signupResponse.LocalId))
                {
                    ViewBag.Error = "Error al registrar usuario en Firebase.";
                    return View();
                }

                // 3. Hashear el password para guardar en tu DB
                string hashedPassword;
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                    var hash = sha256.ComputeHash(bytes);
                    var sb = new System.Text.StringBuilder();
                    foreach (var b in hash)
                        sb.Append(b.ToString("x2"));
                    hashedPassword = sb.ToString();
                }

                // 4. Guardar datos en tu Realtime Database
                var newUser = new User
                {
                    Email = signupResponse.Email,
                    Name = name,
                    Role = role,
                    Password = hashedPassword,
                    RegisterDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    State = 1
                };

                await _dbService.UpdateDataAsync($"user/{signupResponse.LocalId}", newUser, signupResponse.IdToken);

                // 5. Enviar correo de verificación
                bool emailSent = await _authService.SendEmailVerificationAsync(signupResponse.IdToken);

                ViewBag.Message = emailSent
                    ? "Usuario registrado. Se envió un correo de verificación."
                    : "Usuario registrado, pero no se pudo enviar el correo.";

                return RedirectToAction("ManageUsers");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View();
            }
        }



        // GET: mostrar formulario de edición
        public async Task<IActionResult> Edit(string uid)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var user = await _dbService.GetUserAsync($"user/{uid}", token);
            if (user == null)
            {
                ViewBag.Error = "No se encontró el usuario.";
                return RedirectToAction("ManageUsers");
            }

            ViewBag.Uid = uid;
            return View(user);
        }

        // POST: guardar cambios
        [HttpPost]
        public async Task<IActionResult> Edit(string uid, string name, string role, int state)
        {
            var token = HttpContext.Session.GetString("FirebaseToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var updates = new
            {
                name,
                role,
                state
            };

            await _dbService.PatchDataAsync($"user/{uid}", updates, token);

            return RedirectToAction("ManageUsers");
        }




    }
}
