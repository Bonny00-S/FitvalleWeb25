using System.Net.Http.Headers;
using System.Text.Json;

public class ImgBBService
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;

    public ImgBBService(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient();
    }

    public async Task<string?> UploadImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return null;

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();
        var base64 = Convert.ToBase64String(bytes);

        var content = new MultipartFormDataContent();
        content.Add(new StringContent(_apiKey), "key");
        content.Add(new StringContent(base64), "image");

        var response = await _httpClient.PostAsync("https://api.imgbb.com/1/upload", content);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.GetProperty("success").GetBoolean())
        {
            return doc.RootElement.GetProperty("data").GetProperty("url").GetString();
        }

        return null;
    }
}
