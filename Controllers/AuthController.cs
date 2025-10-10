using Fitvalle_25.Services;
using Microsoft.AspNetCore.Mvc;
using Fitvalle_25.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

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

                if (user.State== 0)
                {
                    ViewBag.Error = "Esta cuenta esta deshabilitada";
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


        public IActionResult Landing()
        {
       
            return View();  
        }

        public IActionResult ResetPassword()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Por favor, ingresa tu correo electrónico.";
                return View();
            }

            try
            {
                bool sent = await _authService.SendResetPasswordAsync(email);

                if (sent)
                {
                    ViewBag.Success = "Se ha enviado un enlace para restablecer tu contraseña. Revisa tu bandeja de entrada.";
                }
                else
                {
                    ViewBag.Error = "El correo ingresado no está registrado en el sistema.";
                }
            }
            catch (Exception ex)
            {
                // 🔍 Traducimos los mensajes más comunes de Firebase
                string mensaje = ex.Message switch
                {
                    "EMAIL_NOT_FOUND" => "El correo ingresado no está registrado en el sistema.",
                    "INVALID_EMAIL" => "El formato del correo no es válido.",
                    "TOO_MANY_ATTEMPTS_TRY_LATER" => "Has superado el número de intentos. Intenta más tarde.",
                    "INVALID_REQUEST" => "La solicitud no es válida.",
                    _ => "Ocurrió un error al enviar el correo de restablecimiento. Intenta nuevamente más tarde."
                };

                ViewBag.Error = mensaje;
            }

            return View();
        }


        [HttpGet]
        public async Task<IActionResult> ResetPasswordConfirm(string oobCode)
        {
            if (string.IsNullOrEmpty(oobCode))
            {
                ViewBag.Error = "Enlace inválido.";
                return View("ResetPasswordError");
            }

            try
            {
                // ✅ Verificamos el oobCode con Firebase antes de mostrar la vista
                await _authService.VerifyResetCodeAsync(oobCode);
                ViewBag.OobCode = oobCode;
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("ResetPasswordError");
            }
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordConfirm(string oobCode, string newPassword, string confirmPassword)
        {
            // Validaciones del lado del servidor
            if (string.IsNullOrWhiteSpace(oobCode))
            {
                ModelState.AddModelError("", "Código inválido.");
                return View();
            }

            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError("", "Debes ingresar ambas contraseñas.");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Las contraseñas no coinciden.");
                return View();
            }

            // Validación de complejidad (mínimo 6, mayús, minús, número, especial)
            var regex = new System.Text.RegularExpressions.Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{6,}$");
            if (!regex.IsMatch(newPassword))
            {
                ModelState.AddModelError("", "La contraseña debe tener al menos 6 caracteres, incluir una mayúscula, una minúscula, un número y un carácter especial.");
                return View();
            }

            try
            {
                await _authService.ConfirmResetPasswordAsync(oobCode, newPassword);
                ViewBag.Success = "Tu contraseña ha sido restablecida correctamente.";
                return View("ResetPasswordConfirm");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View("ResetPasswordConfirm");
            }
        }



        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetString("UserRole") == "admin")
            {
                ViewBag.Rol = "~/Views/Shared/_LayoutDashboard.cshtml";
            }
            else
            {
                ViewBag.Rol = "~/Views/Shared/_LayoutCoach.cshtml";
            }
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string newPassword, string confirmPassword)
        {
            if (HttpContext.Session.GetString("UserRole") == "admin")
            {
                ViewBag.Rol = "~/Views/Shared/_LayoutDashboard.cshtml";
            }
            else
            {
                ViewBag.Rol = "~/Views/Shared/_LayoutCoach.cshtml";
            }
            // ✅ Recupera el token de sesión
            var idToken = HttpContext.Session.GetString("FirebaseToken");

            if (string.IsNullOrEmpty(idToken))
            {
                ModelState.AddModelError("", "Sesión expirada. Inicia sesión nuevamente.");
                return View();
            }

            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError("", "Debe ingresar y confirmar su nueva contraseña.");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Las contraseñas no coinciden.");
                return View();
            }

            // ✅ Validación de complejidad
            var regex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{6,}$");
            if (!regex.IsMatch(newPassword))
            {
                ModelState.AddModelError("", "La contraseña debe tener al menos 6 caracteres, incluir una mayúscula, una minúscula, un número y un carácter especial.");
                return View();
            }

            try
            {
                await _authService.ChangePasswordAsync(idToken, newPassword);
                ViewBag.Success = "Tu contraseña ha sido cambiada correctamente.";
                ViewBag.RolDashboard= HttpContext.Session.GetString("UserRole");
                return View();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }







    }


}
