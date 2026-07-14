using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TinyHero.OperationsPortal.Configuration;
using TinyHero.OperationsPortal.Models;

namespace TinyHero.OperationsPortal.Services;

public sealed class JenkinsService
{
    private readonly HttpClient httpClient;
    private readonly OperationsPortalOptions options;

    public JenkinsService(HttpClient _httpClient, IOptions<OperationsPortalOptions> _options)
    {
        httpClient = _httpClient;
        options = _options.Value;
        httpClient.BaseAddress = new Uri(NormalizeBaseUrl(options.JenkinsBaseUrl));

        string? userName = Environment.GetEnvironmentVariable("TINYHERO_JENKINS_USER");
        string? apiToken = Environment.GetEnvironmentVariable("TINYHERO_JENKINS_TOKEN");

        if (string.IsNullOrWhiteSpace(userName) == false && string.IsNullOrWhiteSpace(apiToken) == false)
        {
            string credential = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{apiToken}"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credential);
        }
    }

    public async Task<ServiceStatus> GetStatusAsync(CancellationToken _cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync("api/json", _cancellationToken);
            bool isOnline = response.IsSuccessStatusCode;
            string detail = response.StatusCode == System.Net.HttpStatusCode.Forbidden
                ? "인증 정보가 필요합니다."
                : isOnline
                    ? $"{options.JenkinsJobName} 연결됨"
                    : $"HTTP {(int)response.StatusCode}";
            ServiceStatus result = new(isOnline, "Jenkins", detail);
            return result;
        }
        catch (Exception exception)
        {
            ServiceStatus result = new(false, "Jenkins", exception.Message);
            return result;
        }
    }

    public async Task<JenkinsTriggerResult> TriggerContentUpdateAsync(
        JenkinsBuildRequest _request,
        CancellationToken _cancellationToken)
    {
        string contentStatePath = string.IsNullOrWhiteSpace(_request.ContentStatePath)
            ? options.DefaultContentStatePath
            : _request.ContentStatePath.Trim();
        Dictionary<string, string> parameterDictionary = new()
        {
            ["BUILD_MODE"] = "CONTENT_UPDATE",
            ["CONTENT_STATE_PATH"] = contentStatePath,
            ["CONTENT_PUBLISH_PATH"] = options.DefaultPublishPath,
            ["LOCAL_CONTENT_SERVER_PATH"] = options.LocalContentRoot,
            ["CONTENT_BASE_URL"] = options.ContentBaseUrl,
            ["REQUIRE_REMOTE_CONTENT"] = _request.RequireRemoteContent.ToString().ToLowerInvariant()
        };
        return await TriggerBuildAsync(parameterDictionary, _cancellationToken);
    }

    private async Task<JenkinsTriggerResult> TriggerBuildAsync(
        Dictionary<string, string> _parameterDictionary,
        CancellationToken _cancellationToken)
    {
        try
        {
            await AddCrumbAsync(_cancellationToken);
            string encodedJobName = Uri.EscapeDataString(options.JenkinsJobName);
            using FormUrlEncodedContent formContent = new(_parameterDictionary);
            using HttpResponseMessage response = await httpClient.PostAsync(
                $"job/{encodedJobName}/buildWithParameters",
                formContent,
                _cancellationToken);

            if (response.IsSuccessStatusCode == false)
            {
                string responseText = await response.Content.ReadAsStringAsync(_cancellationToken);
                string detail = string.IsNullOrWhiteSpace(responseText) ? response.ReasonPhrase ?? "Unknown error" : responseText;
                return new JenkinsTriggerResult(false, $"Jenkins 실행 실패: {detail}", null);
            }

            string? queueUrl = response.Headers.Location?.ToString();
            return new JenkinsTriggerResult(true, "콘텐츠 업데이트 빌드가 Jenkins 대기열에 등록되었습니다.", queueUrl);
        }
        catch (Exception exception)
        {
            return new JenkinsTriggerResult(false, $"Jenkins 연결 실패: {exception.Message}", null);
        }
    }

    private async Task AddCrumbAsync(CancellationToken _cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync("crumbIssuer/api/json", _cancellationToken);

        if (response.IsSuccessStatusCode == false)
        {
            return;
        }

        await using Stream responseStream = await response.Content.ReadAsStreamAsync(_cancellationToken);
        JenkinsCrumb? crumb = await JsonSerializer.DeserializeAsync<JenkinsCrumb>(responseStream, cancellationToken: _cancellationToken);

        if (crumb == null || string.IsNullOrWhiteSpace(crumb.CrumbRequestField) || string.IsNullOrWhiteSpace(crumb.Crumb))
        {
            return;
        }

        httpClient.DefaultRequestHeaders.Remove(crumb.CrumbRequestField);
        httpClient.DefaultRequestHeaders.Add(crumb.CrumbRequestField, crumb.Crumb);
    }

    private static string NormalizeBaseUrl(string _url)
    {
        string result = _url.Trim().TrimEnd('/') + "/";
        return result;
    }

    private sealed class JenkinsCrumb
    {
        [JsonPropertyName("crumb")]
        public string Crumb { get; set; } = string.Empty;

        [JsonPropertyName("crumbRequestField")]
        public string CrumbRequestField { get; set; } = string.Empty;
    }
}
