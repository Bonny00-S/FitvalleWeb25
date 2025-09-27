using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Fitvalle_25.Models;
using Microsoft.AspNetCore.Mvc;
namespace Fitvalle_25.Services
{
    public class FirebaseDbService
    {
        private readonly HttpClient _httpClient;
        private readonly string _databaseUrl;
        private readonly string _apiKey;
        public FirebaseDbService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _databaseUrl = config["Firebase:DatabaseUrl"];
            _apiKey=config["Firebase:ApiKey"];
        }
		public async Task<User?> GetUserAsync(string path, string idToken)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentException("El path no puede estar vacío.", nameof(path));

			if (string.IsNullOrEmpty(idToken))
				throw new ArgumentException("El idToken no puede estar vacío.", nameof(idToken));

			var url = $"{_databaseUrl}{path}.json?auth={idToken}";
			var response = await _httpClient.GetAsync(url);

			if (!response.IsSuccessStatusCode)
			{
				var error = await response.Content.ReadAsStringAsync();
				throw new Exception($"Error al consultar Firebase: {response.StatusCode} → {error}");
			}

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<User>(json, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});
		}


        public async Task<bool> UpdateDataAsync(string path, object data, string idToken)
        {
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_databaseUrl}{path}.json?auth={idToken}", content);
            return response.IsSuccessStatusCode;
        }


       
        public async Task<Dictionary<string, User>?> GetAllUsersAsync(string idToken)
        {
            var url = $"{_databaseUrl}user.json?auth={idToken}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error al obtener usuarios: {response.StatusCode} → {error}");
            }

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<Dictionary<string, User>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<bool> DeleteDataAsync(string path, string idToken)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("El path no puede estar vacío.", nameof(path));

            if (string.IsNullOrEmpty(idToken))
                throw new ArgumentException("El idToken no puede estar vacío.", nameof(idToken));

            var url = $"{_databaseUrl}{path}.json?auth={idToken}";
            var response = await _httpClient.DeleteAsync(url);

            return response.IsSuccessStatusCode;
        }
        public async Task<FirebaseLoginResponse?> SignUpAsync(User user)
        {
        
            var content = new StringContent(
                JsonSerializer.Serialize(user),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_apiKey}",
                content
            );

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error al registrar: {json}");

            return JsonSerializer.Deserialize<FirebaseLoginResponse>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }

        public async Task<bool> PatchDataAsync(string path, object data, string idToken)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("El path no puede estar vacío.", nameof(path));

            if (string.IsNullOrEmpty(idToken))
                throw new ArgumentException("El idToken no puede estar vacío.", nameof(idToken));

            var url = $"{_databaseUrl}{path}.json?auth={idToken}";
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = content };
            var response = await _httpClient.SendAsync(request);

            return response.IsSuccessStatusCode;
        }


        public async Task<Dictionary<string, Request>?> GetAllRequestsAsync(string idToken)
        {
            var url = $"{_databaseUrl}request.json?auth={idToken}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error al obtener Solicitudes: {response.StatusCode} → {error}");
            }

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<Dictionary<string, Request>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }



    }
}
