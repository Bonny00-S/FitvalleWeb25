using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Fitvalle_25.Services
{
    public class FirebasePushService
    {
        private readonly HttpClient _httpClient;
        private readonly string _serverKey;

        public FirebasePushService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _serverKey = config["Firebase:ServerKey"]; // AGREGA TU CLAVE A appsettings.json
        }

        public async Task<bool> SendNotificationAsync(string fcmToken, string title, string body)
        {
            if (string.IsNullOrEmpty(fcmToken))
                return false;

            var message = new
            {
                to = fcmToken,
                notification = new
                {
                    title = title,
                    body = body
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(message),
                Encoding.UTF8,
                "application/json"
            );

            var request = new HttpRequestMessage(HttpMethod.Post,
                "https://fcm.googleapis.com/fcm/send");

            request.Headers.TryAddWithoutValidation("Authorization", $"key={_serverKey}");
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
    }
}
