using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Text;
using System.Text.Json;

namespace QL_TapChi_WinForms.Services
{
    internal static class JournalApiClient
    {
        private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(15) };
        private static readonly HttpClient TransferClient = new() { Timeout = TimeSpan.FromMinutes(2) };
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        internal static string? Token { get; set; }
        internal static string? LastError { get; private set; }

        internal static JsonElement? Post(string path, object payload, bool requireToken = true)
            => Send(HttpMethod.Post, path, payload, requireToken);

        internal static JsonElement? Get(string path, bool requireToken = true)
            => Send(HttpMethod.Get, path, null, requireToken);

        internal static T? Read<T>(string path) where T : class
        {
            var json = Get(path);
            if (json is null || json.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return null;
            try { return JsonSerializer.Deserialize<T>(json.Value.GetRawText(), JsonOptions); }
            catch (JsonException ex)
            {
                LastError = $"Dữ liệu API không đúng định dạng: {ex.Message}";
                return null;
            }
        }

        internal static async System.Threading.Tasks.Task<bool> UploadFileAsync(string path, string filePath)
        {
            LastError = null;
            if (string.IsNullOrWhiteSpace(Token)) { LastError = "Vui lòng đăng nhập lại."; return false; }
            try
            {
                await using var stream = File.OpenRead(filePath);
                using var form = new MultipartFormDataContent();
                using var content = new StreamContent(stream);
                form.Add(content, "file", Path.GetFileName(filePath));
                using var request = new HttpRequestMessage(HttpMethod.Post, AppConfig.ApiBaseUrl + path);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                request.Content = form;
                using var timeout = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(2));
                using var response = await TransferClient.SendAsync(request, timeout.Token);
                var body = await response.Content.ReadAsStringAsync(timeout.Token);
                if (response.IsSuccessStatusCode) return true;
                try { LastError = ReadMessage(JsonDocument.Parse(body).RootElement); }
                catch (JsonException) { }
                LastError ??= $"API trả về lỗi {(int)response.StatusCode}.";
                return false;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
            { LastError = $"Không thể tải tệp lên: {ex.Message}"; return false; }
        }

        internal static async System.Threading.Tasks.Task<bool> DownloadFileAsync(string path, string targetPath)
        {
            LastError = null;
            if (string.IsNullOrWhiteSpace(Token)) { LastError = "Vui lòng đăng nhập lại."; return false; }
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, AppConfig.ApiBaseUrl + path);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                using var timeout = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(2));
                using var response = await TransferClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(timeout.Token);
                    try { LastError = ReadMessage(JsonDocument.Parse(body).RootElement); }
                    catch (JsonException) { }
                    LastError ??= $"API trả về lỗi {(int)response.StatusCode}.";
                    return false;
                }
                await using var source = await response.Content.ReadAsStreamAsync(timeout.Token);
                await using var target = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await source.CopyToAsync(target, timeout.Token);
                return true;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
            { LastError = $"Không thể tải tệp về: {ex.Message}"; return false; }
        }

        private static JsonElement? Send(HttpMethod method, string path, object? payload, bool requireToken)
        {
            LastError = null;
            if (requireToken && string.IsNullOrWhiteSpace(Token))
            {
                LastError = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.";
                return null;
            }

            try
            {
                using var request = new HttpRequestMessage(method, AppConfig.ApiBaseUrl + path);
                if (payload != null)
                    request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
                if (requireToken) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                using var response = Client.Send(request);
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                JsonElement json;
                try { json = JsonDocument.Parse(body).RootElement.Clone(); }
                catch { json = default; }

                if (!response.IsSuccessStatusCode)
                {
                    LastError = ReadMessage(json) ?? (response.StatusCode == System.Net.HttpStatusCode.Forbidden
                        ? "Tài khoản hiện tại không có quyền thực hiện thao tác này."
                        : $"API trả về lỗi {(int)response.StatusCode}.");
                    return null;
                }
                return json;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                LastError = $"Không kết nối được Backend API tại {AppConfig.ApiBaseUrl}: {ex.Message}";
                return null;
            }
        }

        internal static string? ReadMessage(JsonElement json)
        {
            if (json.ValueKind != JsonValueKind.Object) return null;
            if (json.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                return message.GetString();
            if (json.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                return title.GetString();
            return null;
        }
    }
}
