using Fitvalle_25.Models;
using Fitvalle_25.Models.Exercise;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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

        public async Task<bool> InitializeDataAsync(string idToken)
        {
            // ✅ 1️⃣ Obtener datos actuales
            var existingMuscles = await GetExistingDataAsync<TargetMuscle>("targetMuscles", idToken);
            var existingTypes = await GetExistingDataAsync<ExerciseType>("exerciseTypes", idToken);

            // ✅ 2️⃣ Listas de inicialización
            var muscles = new List<TargetMuscle>
    {
        new TargetMuscle { Name = "Pecho" },
        new TargetMuscle { Name = "Espalda" },
        new TargetMuscle { Name = "Piernas" },
        new TargetMuscle { Name = "Bíceps" },
        new TargetMuscle { Name = "Tríceps" },
        new TargetMuscle { Name = "Hombros" },
        new TargetMuscle { Name = "Abdomen" }
    };

            var types = new List<ExerciseType>
    {
        new ExerciseType { Name = "Fuerza", Description = "Ejercicios para aumentar la fuerza muscular." },
        new ExerciseType { Name = "Cardio", Description = "Ejercicios para mejorar la resistencia cardiovascular." },
        new ExerciseType { Name = "Flexibilidad", Description = "Ejercicios para mejorar la movilidad y flexibilidad." },
        new ExerciseType { Name = "Equilibrio", Description = "Ejercicios para mejorar la coordinación y balance." },
        new ExerciseType { Name = "Resistencia", Description = "Ejercicios para mejorar la resistencia muscular." }
    };

            // ✅ 3️⃣ Insertar TargetMuscles solo si no existen
            foreach (var muscle in muscles)
            {
                if (existingMuscles.Values.Any(m =>
                    m.Name.Equals(muscle.Name, StringComparison.OrdinalIgnoreCase)))
                    continue; // ya existe, saltar

                var postResponse = await _httpClient.PostAsync(
                    $"{_databaseUrl}targetMuscles.json?auth={idToken}",
                    new StringContent(JsonSerializer.Serialize(muscle), Encoding.UTF8, "application/json")
                );

                if (!postResponse.IsSuccessStatusCode) return false;

                var json = await postResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var key = doc.RootElement.GetProperty("name").GetString();

                muscle.Id = key!;
                await PatchDataAsync($"targetMuscles/{key}", muscle, idToken);
            }

            // ✅ 4️⃣ Insertar ExerciseTypes solo si no existen
            foreach (var type in types)
            {
                if (existingTypes.Values.Any(t =>
                    t.Name.Equals(type.Name, StringComparison.OrdinalIgnoreCase)))
                    continue; // ya existe, saltar

                var postResponse = await _httpClient.PostAsync(
                    $"{_databaseUrl}exerciseTypes.json?auth={idToken}",
                    new StringContent(JsonSerializer.Serialize(type), Encoding.UTF8, "application/json")
                );

                if (!postResponse.IsSuccessStatusCode) return false;

                var json = await postResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var key = doc.RootElement.GetProperty("name").GetString();

                type.Id = key!;
                await PatchDataAsync($"exerciseTypes/{key}", type, idToken);
            }

            return true;
        }


        // 🔧 Método auxiliar para leer los datos existentes
        private async Task<Dictionary<string, T>> GetExistingDataAsync<T>(string path, string idToken)
        {
            var url = $"{_databaseUrl}{path}.json?auth={idToken}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new Dictionary<string, T>();

            var json = await response.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<Dictionary<string, T>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return data ?? new Dictionary<string, T>();
        }
        // 🔧 Método genérico para obtener cualquier colección desde Firebase
        public async Task<Dictionary<string, T>?> GetAllAsync<T>(string path, string idToken)
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
                throw new Exception($"Error al obtener datos de Firebase ({path}): {response.StatusCode} → {error}");
            }

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<Dictionary<string, T>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }


    }
}
