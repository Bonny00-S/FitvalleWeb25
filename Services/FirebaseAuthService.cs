using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fitvalle_25.Controllers;
using Microsoft.Extensions.Configuration;
using Fitvalle_25.Models;
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
                email = email
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

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonSerializer.Deserialize<FirebaseErrorResponse>(json);
                var errorMessage = errorResponse?.Error?.Message ?? "Error desconocido";
                throw new Exception(errorMessage);
            }

            return true;
        }



    }

}
