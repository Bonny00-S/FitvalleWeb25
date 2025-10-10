using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fitvalle_25.Controllers;
using Microsoft.Extensions.Configuration;
using Fitvalle_25.Models;
using System.Security.Cryptography;

namespace Fitvalle_25.Services
{
    public class FirebaseAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public FirebaseAuthService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["Firebase:ApiKey"];
        }

        public async Task<FirebaseLoginResponse?> LoginAsync(string email, string password)
        {
            var requestBody = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}",
                content
            );

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // 👇 Deserializamos el error para poder leer el mensaje
                var errorResponse = JsonSerializer.Deserialize<FirebaseErrorResponse>(json);
                var errorMessage = errorResponse?.Error?.Message ?? "Error desconocido";

                // Lanzamos excepción con mensaje amigable
                throw new Exception(errorMessage);
            }

            return JsonSerializer.Deserialize<FirebaseLoginResponse>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }

        public async Task<bool> SendEmailVerificationAsync(string idToken)
        {
            var requestBody = new
            {
                requestType = "VERIFY_EMAIL",
                idToken = idToken
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={_apiKey}",
                content
            );

            return response.IsSuccessStatusCode;
        }


        public async Task<bool> SendResetPasswordAsync(string email)
        {
            var requestBody = new
            {
                requestType = "PASSWORD_RESET",
                email = email,
                continueUrl = "https://fitvalle-web.onrender.com/auth/confirmreset"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={_apiKey}",
                content
            );

            var json = await response.Content.ReadAsStringAsync();

            // Si la respuesta es exitosa
            if (response.IsSuccessStatusCode)
                return true;

            // Si hay error, procesamos el JSON devuelto
            var errorResponse = JsonSerializer.Deserialize<FirebaseErrorResponse>(json);

            var errorMessage = errorResponse?.Error?.Message ?? "ERROR_DESCONOCIDO";

            if (errorMessage == "EMAIL_NOT_FOUND")
            {
                // ❌ El correo no existe en Firebase
                return false;
            }

            // ⚠️ Otro error distinto, lanzamos excepción
            throw new Exception(errorMessage);
        }


        /// <summary>
        /// Verifica si el código oobCode del enlace de restablecimiento es válido.
        /// </summary>
        public async Task<bool> VerifyResetCodeAsync(string oobCode)
        {
            if (string.IsNullOrWhiteSpace(oobCode))
                throw new ArgumentException("El código de verificación es inválido.");

            var requestBody = new { oobCode = oobCode };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:resetPassword?key={_apiKey}",
                content
            );

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonSerializer.Deserialize<FirebaseErrorResponse>(json);
                var errorMessage = errorResponse?.Error?.Message ?? "Código inválido o expirado.";
                throw new Exception(errorMessage);
            }

            return true;
        }

        /// <summary>
        /// Confirma el cambio de contraseña con el código recibido en el correo.
        /// </summary>
        public async Task<bool> ConfirmResetPasswordAsync(string oobCode, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(oobCode))
                throw new ArgumentException("El código de restablecimiento es inválido.");

            if (string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentException("La nueva contraseña no puede estar vacía.");

            var requestBody = new
            {
                oobCode = oobCode,
                newPassword = newPassword
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:resetPassword?key={_apiKey}",
                content
            );

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonSerializer.Deserialize<FirebaseErrorResponse>(json);
                var errorMessage = errorResponse?.Error?.Message ?? "Error desconocido al restablecer la contraseña.";

                errorMessage = errorMessage switch
                {
                    "INVALID_OOB_CODE" => "El enlace de restablecimiento no es válido o ya ha sido usado.",
                    "EXPIRED_OOB_CODE" => "El enlace de restablecimiento ha expirado. Solicita uno nuevo.",
                    "WEAK_PASSWORD : Password should be at least 6 characters" => "La contraseña es demasiado débil (mínimo 6 caracteres).",
                    _ => errorMessage
                };

                throw new Exception(errorMessage);
            }

            return true;
        }


    public async Task<bool> ChangePasswordAsync(string idToken, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            throw new ArgumentException("Token inválido o sesión expirada.");

        if (string.IsNullOrWhiteSpace(newPassword))
            throw new ArgumentException("La nueva contraseña no puede estar vacía.");

        // 1️⃣ Actualiza la contraseña en Firebase Authentication
        var requestBody = new
        {
            idToken = idToken,
            password = newPassword,
            returnSecureToken = true
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(
            $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={_apiKey}",
            content
        );

        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = JsonSerializer.Deserialize<FirebaseErrorResponse>(json);
            var errorMessage = errorResponse?.Error?.Message ?? "Error al cambiar la contraseña.";
            throw new Exception(errorMessage);
        }


        return true;
    }




}

}
