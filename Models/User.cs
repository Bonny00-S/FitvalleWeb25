using System.Text.Json.Serialization;

namespace Fitvalle_25.Models
{
	public class User
	{
		[JsonPropertyName("email")]
		public string Email { get; set; }

		[JsonPropertyName("id")]
		public long Id { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("password")]
		public string Password { get; set; }

		[JsonPropertyName("registerDate")]
		public string RegisterDate { get; set; }

		[JsonPropertyName("role")]
		public string Role { get; set; }

		[JsonPropertyName("state")]
		public int State { get; set; }

        [JsonPropertyName("photoUrl")]
        public string? PhotoUrl { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("speciality")]
        public string? Specialty { get; set; }

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }

        [JsonPropertyName("fcmToken")]
        public string? FcmToken { get; set; }

        public string GetAvatarUrl()
        {
            if (!string.IsNullOrEmpty(Avatar))
                return $"/images/avatars/{Avatar}.png";

            return "/images/iconUser.png";
        }


    }
}
