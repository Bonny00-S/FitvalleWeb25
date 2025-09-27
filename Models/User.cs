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
	}
}
