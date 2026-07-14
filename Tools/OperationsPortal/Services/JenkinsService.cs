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
    private const string PlayerBuildMode = "PLAYER_BUILD";
    private const string ContentUpdateBuildMode = "CONTENT_UPDATE";

    private readonly HttpClient httpClient;
    private readonly OperationsPortalOptions options;
    private readonly JenkinsCredentialService credentialService;

    public JenkinsService(
        HttpClient _httpClient,
        IOptions<OperationsPortalOptions> _options,
        JenkinsCredentialService _credentialService)
    {
        httpClient = _httpClient;
        options = _options.Value;
        credentialService = _credentialService;
        httpClient.BaseAddress = new Uri(NormalizeBaseUrl(options.JenkinsBaseUrl));
    }

    public async Task<ServiceStatus> GetStatusAsync(CancellationToken _cancellationToken)
    {
        try
        {
            using HttpRequestMessage request = CreateRequest(HttpMethod.Get, "api/json");
            using HttpResponseMessage response = await httpClient.SendAsync(request, _cancellationToken);
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
            ["BUILD_MODE"] = ContentUpdateBuildMode,
            ["CONTENT_STATE_PATH"] = contentStatePath,
            ["CONTENT_PUBLISH_PATH"] = options.DefaultPublishPath,
            ["LOCAL_CONTENT_SERVER_PATH"] = options.LocalContentRoot,
            ["CONTENT_BASE_URL"] = options.ContentBaseUrl,
            ["REQUIRE_REMOTE_CONTENT"] = _request.RequireRemoteContent.ToString().ToLowerInvariant()
        };
        return await TriggerBuildAsync(parameterDictionary, "콘텐츠 업데이트", _cancellationToken);
    }

    /// <summary>
    /// Jenkins에 Windows 플레이어 빌드를 등록한다.
    /// </summary>
    public async Task<JenkinsTriggerResult> TriggerPlayerBuildAsync(
        JenkinsPlayerBuildRequest _request,
        CancellationToken _cancellationToken)
    {
        string gameVersion = string.IsNullOrWhiteSpace(_request.GameVersion)
            ? options.DefaultGameVersion
            : _request.GameVersion.Trim();

        if (System.Text.RegularExpressions.Regex.IsMatch(gameVersion, @"^\d+\.\d+\.\d+$") == false)
        {
            return new JenkinsTriggerResult(false, "게임 버전은 0.0.01 형식으로 입력해야 합니다.", null);
        }

        string buildOutputPath = string.IsNullOrWhiteSpace(_request.BuildOutputPath)
            ? options.DefaultBuildOutputPath
            : _request.BuildOutputPath.Trim();
        Dictionary<string, string> parameterDictionary = new()
        {
            ["BUILD_MODE"] = PlayerBuildMode,
            ["GAME_VERSION"] = gameVersion,
            ["BUILD_OUTPUT_PATH"] = buildOutputPath,
            ["CONTENT_PUBLISH_PATH"] = options.DefaultPublishPath,
            ["LOCAL_CONTENT_SERVER_PATH"] = options.LocalContentRoot,
            ["CONTENT_BASE_URL"] = options.ContentBaseUrl,
            ["REQUIRE_REMOTE_CONTENT"] = _request.RequireRemoteContent.ToString().ToLowerInvariant()
        };
        return await TriggerBuildAsync(parameterDictionary, "플레이어 빌드", _cancellationToken);
    }

    /// <summary>
    /// Jenkins 작업의 대기열과 최근 빌드 상태를 조회한다.
    /// </summary>
    public async Task<JenkinsBuildStatus> GetBuildStatusAsync(CancellationToken _cancellationToken)
    {
        try
        {
            string encodedJobName = Uri.EscapeDataString(options.JenkinsJobName);
            const string treeQuery = "inQueue,queueItem[url,why],lastBuild[number,url,building,result,displayName]";
            using HttpRequestMessage request = CreateRequest(HttpMethod.Get, $"job/{encodedJobName}/api/json?tree={treeQuery}");
            using HttpResponseMessage response = await httpClient.SendAsync(request, _cancellationToken);

            if (response.IsSuccessStatusCode == false)
            {
                return new JenkinsBuildStatus(false, false, false, null, "OFFLINE", $"HTTP {(int)response.StatusCode}", null);
            }

            await using Stream responseStream = await response.Content.ReadAsStreamAsync(_cancellationToken);
            JenkinsJobState? jobState = await JsonSerializer.DeserializeAsync<JenkinsJobState>(responseStream, cancellationToken: _cancellationToken);

            if (jobState == null)
            {
                return new JenkinsBuildStatus(false, false, false, null, "UNKNOWN", "빌드 상태를 읽을 수 없습니다.", null);
            }

            if (jobState.InQueue)
            {
                string queueDetail = string.IsNullOrWhiteSpace(jobState.QueueItem?.Why)
                    ? "Jenkins 대기열에서 실행을 기다리는 중입니다."
                    : jobState.QueueItem.Why;
                return new JenkinsBuildStatus(true, true, false, jobState.LastBuild?.Number, "QUEUED", queueDetail, jobState.QueueItem?.Url);
            }

            JenkinsBuildState? lastBuild = jobState.LastBuild;

            if (lastBuild == null)
            {
                return new JenkinsBuildStatus(true, false, false, null, "IDLE", "아직 실행된 빌드가 없습니다.", null);
            }

            if (lastBuild.Building)
            {
                return new JenkinsBuildStatus(true, false, true, lastBuild.Number, "BUILDING", $"{lastBuild.DisplayName} 실행 중", lastBuild.Url);
            }

            string result = string.IsNullOrWhiteSpace(lastBuild.Result) ? "UNKNOWN" : lastBuild.Result;
            return new JenkinsBuildStatus(true, false, false, lastBuild.Number, result, $"{lastBuild.DisplayName} · {result}", lastBuild.Url);
        }
        catch (Exception exception)
        {
            return new JenkinsBuildStatus(false, false, false, null, "OFFLINE", exception.Message, null);
        }
    }

    private async Task<JenkinsTriggerResult> TriggerBuildAsync(
        Dictionary<string, string> _parameterDictionary,
        string _buildLabel,
        CancellationToken _cancellationToken)
    {
        try
        {
            await AddCrumbAsync(_cancellationToken);
            string encodedJobName = Uri.EscapeDataString(options.JenkinsJobName);
            using HttpRequestMessage request = CreateRequest(HttpMethod.Post, $"job/{encodedJobName}/buildWithParameters");
            request.Content = new FormUrlEncodedContent(_parameterDictionary);
            using HttpResponseMessage response = await httpClient.SendAsync(request, _cancellationToken);

            if (response.IsSuccessStatusCode == false)
            {
                string responseText = await response.Content.ReadAsStringAsync(_cancellationToken);
                string detail = string.IsNullOrWhiteSpace(responseText) ? response.ReasonPhrase ?? "Unknown error" : responseText;
                return new JenkinsTriggerResult(false, $"Jenkins 실행 실패: {detail}", null);
            }

            string? queueUrl = response.Headers.Location?.ToString();
            return new JenkinsTriggerResult(true, $"{_buildLabel}가 Jenkins 대기열에 등록되었습니다.", queueUrl);
        }
        catch (Exception exception)
        {
            return new JenkinsTriggerResult(false, $"Jenkins 연결 실패: {exception.Message}", null);
        }
    }

    private async Task AddCrumbAsync(CancellationToken _cancellationToken)
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, "crumbIssuer/api/json");
        using HttpResponseMessage response = await httpClient.SendAsync(request, _cancellationToken);

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

    /// <summary>
    /// 현재 저장된 인증 정보를 포함한 Jenkins HTTP 요청을 생성한다.
    /// </summary>
    private HttpRequestMessage CreateRequest(HttpMethod _method, string _relativeUrl)
    {
        HttpRequestMessage request = new(_method, _relativeUrl);
        JenkinsCredential? credential = credentialService.GetCredential();

        if (credential != null)
        {
            string credentialText = $"{credential.UserName}:{credential.ApiToken}";
            string encodedCredential = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentialText));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encodedCredential);
        }

        return request;
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

    private sealed class JenkinsJobState
    {
        [JsonPropertyName("inQueue")]
        public bool InQueue { get; set; }

        [JsonPropertyName("queueItem")]
        public JenkinsQueueState? QueueItem { get; set; }

        [JsonPropertyName("lastBuild")]
        public JenkinsBuildState? LastBuild { get; set; }
    }

    private sealed class JenkinsQueueState
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("why")]
        public string? Why { get; set; }
    }

    private sealed class JenkinsBuildState
    {
        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("building")]
        public bool Building { get; set; }

        [JsonPropertyName("result")]
        public string? Result { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;
    }
}
