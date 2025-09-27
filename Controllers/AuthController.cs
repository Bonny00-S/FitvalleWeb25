using Fitvalle_25.Services;
using Microsoft.AspNetCore.Mvc;
using Fitvalle_25.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Fitvalle_25.Controllers
{
    public class AuthController : Controller
    {
        private readonly FirebaseAuthService _authService;

        public AuthController(FirebaseAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login() => View();

        public async Task<IActionResult> Login(string email, string password, [FromServices] FirebaseDbService dbService)
        {
            try
            {
                var loginResponse = await _authService.LoginAsync(email, password);

                if (loginResponse == null || string.IsNullOrEmpty(loginResponse.IdToken))
                {
                    ViewBag.Error = "Credenciales inválidas.";
                    return View();
                }

                // Guardamos sesión
                HttpContext.Session.SetString("FirebaseToken", loginResponse.IdToken);
                HttpContext.Session.SetString("FirebaseUid", loginResponse.LocalId ?? "");
                HttpContext.Session.SetString("FirebaseEmail", loginResponse.Email ?? "");

                // 👇 Ahora consultamos en la DB el rol del usuario
                var user = await dbService.GetUserAsync($"user/{loginResponse.LocalId}", loginResponse.IdToken);

                if (user == null)
                {
                    ViewBag.Error = "No se encontró el perfil en la base de datos.";
                    return View();
                }

                // Guardamos el rol en sesión
                HttpContext.Session.SetString("UserRole", user.Role ?? "none");

                // 🚀 Redirigir según el rol
                if (user.Role == "admin")
                {
                    return RedirectToAction("Dashboard", "admin");
                }
                else if (user.Role == "coach")
                {
                    return RedirectToAction("Dashboard", "coach");
                }
                else
                {
                    // Rol desconocido → página genérica
                    return RedirectToAction("Perfil", "Auth");
                }
            }
            catch (Exception ex)
            {
                string msg = ex.Message switch
                {
                    "EMAIL_NOT_FOUND" => "El correo no está registrado.",
                    "INVALID_PASSWORD" => "La contraseña es incorrecta.",
                    "INVALID_LOGIN_CREDENTIALS" => "Email o contraseña incorrecta.",
                    _ => "Error al iniciar sesión."
                };

                ViewBag.Error = msg;
                return View();
            }
        }

        public IActionResult Logout()
        {
            // Limpia toda la sesión
            HttpContext.Session.Clear();

            // Redirige al login
            return RedirectToAction("Login", "Auth");
        }





        public async Task<IActionResult> Perfil([FromServices] FirebaseDbService dbService)
		{
			var token = HttpContext.Session.GetString("FirebaseToken");
			var uid = HttpContext.Session.GetString("FirebaseUid");

			if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(uid))
				return RedirectToAction("Login");

			// 🚀 Ahora obtenemos directamente un objeto User
			var user = await dbService.GetUserAsync($"user/{uid}", token);

			if (user == null)
			{
				ViewBag.Error = "No se encontró el usuario en la base de datos";
				return View();
			}

			return View(user);
		}







	}


}
