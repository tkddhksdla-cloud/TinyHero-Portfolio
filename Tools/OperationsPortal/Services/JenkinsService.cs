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
    private const string GameVersionParameterName = "GAME_VERSION";
    private const string BuildModeParameterName = "BUILD_MODE";
    private const string BuildPlatformParameterName = "BUILD_PLATFORM";
    private const string AndroidContentStatePath = "Assets/AddressableAssetsData/Android/addressables_content_state.bin";
    private const string IosContentStatePath = "Assets/AddressableAssetsData/iOS/addressables_content_state.bin";

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
        string contentStatePath = ResolveContentStatePath( _request.ContentStatePath, _request.Platform );
        Dictionary<string, string> parameterDictionary = new()
        {
            ["BUILD_MODE"] = ContentUpdateBuildMode,
            ["CONTENT_STATE_PATH"] = contentStatePath,
            ["CONTENT_PUBLISH_PATH"] = options.DefaultPublishPath,
            ["LOCAL_CONTENT_SERVER_PATH"] = options.LocalContentRoot,
            ["CONTENT_BASE_URL"] = options.ContentBaseUrl,
            ["REQUIRE_REMOTE_CONTENT"] = _request.RequireRemoteContent.ToString().ToLowerInvariant()
        };
        parameterDictionary[ "BUILD_PLATFORM" ] = _request.Platform.ToString();
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
        parameterDictionary[ "BUILD_PLATFORM" ] = _request.Platform.ToString();
        parameterDictionary[ "ANDROID_ARTIFACT_TYPE" ] = _request.AndroidArtifactType.ToString();
        return await TriggerBuildAsync(parameterDictionary, "플레이어 빌드", _cancellationToken);
    }

    /// <summary>
    /// 실행 중이거나 대기 중인 Jenkins 빌드를 중지한다.
    /// </summary>
    public async Task<JenkinsCancelResult> CancelBuildAsync(
        int _buildNumber,
        CancellationToken _cancellationToken)
    {
        if (_buildNumber <= 0)
        {
            return new JenkinsCancelResult(false, "유효한 Jenkins 빌드 번호가 필요합니다.");
        }

        try
        {
            await AddCrumbAsync(_cancellationToken);
            string encodedJobName = Uri.EscapeDataString(options.JenkinsJobName);
            using HttpRequestMessage request = CreateRequest(HttpMethod.Post, $"job/{encodedJobName}/{_buildNumber}/stop");
            using HttpResponseMessage response = await httpClient.SendAsync(request, _cancellationToken);

            if (response.IsSuccessStatusCode == false)
            {
                return new JenkinsCancelResult(false, $"Jenkins 빌드 중지 실패: HTTP {(int)response.StatusCode}");
            }

            return new JenkinsCancelResult(true, $"Jenkins 빌드 #{_buildNumber} 중지 요청을 전달했습니다.");
        }
        catch (Exception exception)
        {
            return new JenkinsCancelResult(false, $"Jenkins 빌드 중지 실패: {exception.Message}");
        }
    }

    /// <summary>
    /// Jenkins 작업의 대기열과 최근 빌드 상태를 조회한다.
    /// </summary>
    public async Task<JenkinsBuildStatus> GetBuildStatusAsync(CancellationToken _cancellationToken)
    {
        try
        {
            string encodedJobName = Uri.EscapeDataString(options.JenkinsJobName);
            const string treeQuery = "inQueue,queueItem[url,why],lastBuild[number,url,building,result,displayName,timestamp,duration,estimatedDuration,actions[parameters[name,value]]],builds[number,url,building,result,timestamp,duration,estimatedDuration,actions[parameters[name,value]]]{0,6}";
            string encodedTreeQuery = Uri.EscapeDataString(treeQuery);
            using HttpRequestMessage request = CreateRequest(HttpMethod.Get, $"job/{encodedJobName}/api/json?tree={encodedTreeQuery}");
            using HttpResponseMessage response = await httpClient.SendAsync(request, _cancellationToken);

            if (response.IsSuccessStatusCode == false)
            {
                return new JenkinsBuildStatus(false, false, false, null, "OFFLINE", $"HTTP {(int)response.StatusCode}", null, 0, 0L, 0L, null, Array.Empty<JenkinsBuildHistoryItem>());
            }

            await using Stream responseStream = await response.Content.ReadAsStreamAsync(_cancellationToken);
            JenkinsJobState? jobState = await JsonSerializer.DeserializeAsync<JenkinsJobState>(responseStream, cancellationToken: _cancellationToken);

            if (jobState == null)
            {
                return new JenkinsBuildStatus(false, false, false, null, "UNKNOWN", "빌드 상태를 읽을 수 없습니다.", null, 0, 0L, 0L, null, Array.Empty<JenkinsBuildHistoryItem>());
            }

            IReadOnlyList<JenkinsBuildHistoryItem> recentBuildList = BuildRecentBuildHistory(jobState.Builds);
            IReadOnlyList<JenkinsBuildActivityItem> runningBuildList = BuildRunningBuildActivityList(jobState.Builds);
            IReadOnlyList<JenkinsBuildActivityItem> queuedBuildList = await GetQueuedBuildActivityListAsync(_cancellationToken);
            List<JenkinsBuildActivityItem> activeBuildList = new(runningBuildList.Count + queuedBuildList.Count);
            activeBuildList.AddRange(runningBuildList);
            activeBuildList.AddRange(queuedBuildList);

            if (runningBuildList.Count > 0)
            {
                JenkinsBuildActivityItem primaryBuild = runningBuildList[0];
                return new JenkinsBuildStatus(true, queuedBuildList.Count > 0, true, primaryBuild.BuildNumber, "BUILDING", primaryBuild.Detail, primaryBuild.BuildUrl, primaryBuild.ProgressPercent, primaryBuild.ElapsedMilliseconds, primaryBuild.EstimatedDurationMilliseconds, primaryBuild.StartedAtUtc, recentBuildList, primaryBuild.BuildPlatform, activeBuildList);
            }

            if (queuedBuildList.Count > 0)
            {
                JenkinsBuildActivityItem primaryBuild = queuedBuildList[0];
                return new JenkinsBuildStatus(true, true, false, primaryBuild.BuildNumber, "QUEUED", primaryBuild.Detail, primaryBuild.BuildUrl, 0, 0L, 0L, null, recentBuildList, primaryBuild.BuildPlatform, activeBuildList);
            }

            JenkinsBuildState? lastBuild = jobState.LastBuild;

            if (lastBuild == null)
            {
                return new JenkinsBuildStatus(true, false, false, null, "IDLE", "아직 실행된 빌드가 없습니다.", null, 0, 0L, 0L, null, recentBuildList, "UNKNOWN", activeBuildList);
            }

            DateTimeOffset? startedAtUtc = lastBuild.Timestamp > 0L
                ? DateTimeOffset.FromUnixTimeMilliseconds(lastBuild.Timestamp)
                : null;

            string result = string.IsNullOrWhiteSpace(lastBuild.Result) ? "UNKNOWN" : lastBuild.Result;
            long completedDuration = Math.Max(0L, lastBuild.Duration);
            int completedProgressPercent = string.Equals(result, "UNKNOWN", StringComparison.OrdinalIgnoreCase) ? 0 : 100;
            string completedBuildPlatform = ResolveBuildPlatform( lastBuild.Actions );
            return new JenkinsBuildStatus(true, false, false, lastBuild.Number, result, $"{lastBuild.DisplayName} · {result}", lastBuild.Url, completedProgressPercent, completedDuration, lastBuild.EstimatedDuration, startedAtUtc, recentBuildList, completedBuildPlatform, activeBuildList);
        }
        catch (Exception exception)
        {
            return new JenkinsBuildStatus(false, false, false, null, "OFFLINE", exception.Message, null, 0, 0L, 0L, null, Array.Empty<JenkinsBuildHistoryItem>());
        }
    }

    private static IReadOnlyList<JenkinsBuildHistoryItem> BuildRecentBuildHistory(IReadOnlyList<JenkinsBuildState>? _buildList)
    {
        if (_buildList == null || _buildList.Count == 0)
        {
            return Array.Empty<JenkinsBuildHistoryItem>();
        }

        List<JenkinsBuildHistoryItem> resultList = new(_buildList.Count);

        for (int buildIndex = 0; buildIndex < _buildList.Count; buildIndex++)
        {
            JenkinsBuildState buildState = _buildList[buildIndex];
            string gameVersion = ResolveBuildParameter(buildState.Actions, GameVersionParameterName);
            string buildMode = ResolveBuildParameter(buildState.Actions, BuildModeParameterName);
            string buildPlatform = ResolveBuildPlatform( buildState.Actions );
            string state = buildState.Building
                ? "BUILDING"
                : string.IsNullOrWhiteSpace(buildState.Result) ? "UNKNOWN" : buildState.Result;
            DateTimeOffset? startedAtUtc = buildState.Timestamp > 0L
                ? DateTimeOffset.FromUnixTimeMilliseconds(buildState.Timestamp)
                : null;
            JenkinsBuildHistoryItem historyItem = new(
                buildState.Number,
                string.IsNullOrWhiteSpace(gameVersion) ? "—" : gameVersion,
                string.IsNullOrWhiteSpace(buildMode) ? "UNKNOWN" : buildMode,
                buildPlatform,
                state,
                startedAtUtc,
                Math.Max(0L, buildState.Duration),
                buildState.Url);
            resultList.Add(historyItem);
        }

        return resultList;
    }

    private static IReadOnlyList<JenkinsBuildActivityItem> BuildRunningBuildActivityList(IReadOnlyList<JenkinsBuildState>? _buildList)
    {
        if (_buildList == null || _buildList.Count == 0)
        {
            return Array.Empty<JenkinsBuildActivityItem>();
        }

        List<JenkinsBuildActivityItem> resultList = new();

        for (int buildIndex = 0; buildIndex < _buildList.Count; buildIndex++)
        {
            JenkinsBuildState buildState = _buildList[buildIndex];

            if (buildState.Building == false)
            {
                continue;
            }

            DateTimeOffset? startedAtUtc = buildState.Timestamp > 0L
                ? DateTimeOffset.FromUnixTimeMilliseconds(buildState.Timestamp)
                : null;
            long elapsedMilliseconds = startedAtUtc.HasValue
                ? Math.Max(0L, (long)(DateTimeOffset.UtcNow - startedAtUtc.Value).TotalMilliseconds)
                : 0L;
            int progressPercent = CalculateBuildProgressPercent(elapsedMilliseconds, buildState.EstimatedDuration);
            string buildMode = ResolveBuildParameter(buildState.Actions, BuildModeParameterName);
            string buildPlatform = ResolveBuildPlatform(buildState.Actions);
            JenkinsBuildActivityItem activityItem = new(
                buildState.Number,
                string.IsNullOrWhiteSpace(buildMode) ? "UNKNOWN" : buildMode,
                buildPlatform,
                "BUILDING",
                $"빌드 #{buildState.Number} 실행 중",
                buildState.Url,
                progressPercent,
                elapsedMilliseconds,
                buildState.EstimatedDuration,
                startedAtUtc);
            resultList.Add(activityItem);
        }

        IReadOnlyList<JenkinsBuildActivityItem> result = resultList
            .OrderByDescending(_item => _item.StartedAtUtc)
            .ToArray();
        return result;
    }

    private async Task<IReadOnlyList<JenkinsBuildActivityItem>> GetQueuedBuildActivityListAsync(CancellationToken _cancellationToken)
    {
        try
        {
            const string treeQuery = "items[id,url,why,cancelled,task[name],actions[parameters[name,value]]]";
            string encodedTreeQuery = Uri.EscapeDataString(treeQuery);
            using HttpRequestMessage request = CreateRequest(HttpMethod.Get, $"queue/api/json?tree={encodedTreeQuery}");
            using HttpResponseMessage response = await httpClient.SendAsync(request, _cancellationToken);

            if (response.IsSuccessStatusCode == false)
            {
                return Array.Empty<JenkinsBuildActivityItem>();
            }

            await using Stream responseStream = await response.Content.ReadAsStreamAsync(_cancellationToken);
            JenkinsQueueCollectionState? queueState = await JsonSerializer.DeserializeAsync<JenkinsQueueCollectionState>(responseStream, cancellationToken: _cancellationToken);

            if (queueState == null || queueState.Items.Count == 0)
            {
                return Array.Empty<JenkinsBuildActivityItem>();
            }

            List<JenkinsBuildActivityItem> resultList = new();

            for (int queueIndex = 0; queueIndex < queueState.Items.Count; queueIndex++)
            {
                JenkinsQueueState queueItem = queueState.Items[queueIndex];

                if (queueItem.Cancelled || string.Equals(queueItem.Task?.Name, options.JenkinsJobName, StringComparison.OrdinalIgnoreCase) == false)
                {
                    continue;
                }

                string buildMode = ResolveBuildParameter(queueItem.Actions, BuildModeParameterName);
                string buildPlatform = ResolveBuildPlatform(queueItem.Actions);
                string detail = string.IsNullOrWhiteSpace(queueItem.Why)
                    ? "Jenkins 대기열에서 실행을 기다리는 중입니다."
                    : queueItem.Why;
                JenkinsBuildActivityItem activityItem = new(
                    null,
                    string.IsNullOrWhiteSpace(buildMode) ? "UNKNOWN" : buildMode,
                    buildPlatform,
                    "QUEUED",
                    detail,
                    queueItem.Url,
                    0,
                    0L,
                    0L,
                    null);
                resultList.Add(activityItem);
            }

            IReadOnlyList<JenkinsBuildActivityItem> result = resultList.ToArray();
            return result;
        }
        catch
        {
            return Array.Empty<JenkinsBuildActivityItem>();
        }
    }

    /// <summary>
    /// Jenkins 빌드 파라미터에서 대상 플랫폼을 읽는다.
    /// </summary>
    private static string ResolveBuildPlatform( IReadOnlyList<JenkinsBuildAction>? _actionList )
    {
        string buildPlatform = ResolveBuildParameter( _actionList, BuildPlatformParameterName );
        string result = string.IsNullOrWhiteSpace( buildPlatform ) ? "UNKNOWN" : buildPlatform;
        return result;
    }

    private static string ResolveBuildParameter(IReadOnlyList<JenkinsBuildAction>? _actionList, string _parameterName)
    {
        if (_actionList == null || string.IsNullOrWhiteSpace(_parameterName))
        {
            return string.Empty;
        }

        for (int actionIndex = 0; actionIndex < _actionList.Count; actionIndex++)
        {
            JenkinsBuildAction action = _actionList[actionIndex];

            for (int parameterIndex = 0; parameterIndex < action.Parameters.Count; parameterIndex++)
            {
                JenkinsBuildParameter parameter = action.Parameters[parameterIndex];

                if (string.Equals(parameter.Name, _parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    string result = parameter.Value.ValueKind == JsonValueKind.String
                        ? parameter.Value.GetString() ?? string.Empty
                        : parameter.Value.ToString();
                    return result;
                }
            }
        }

        return string.Empty;
    }

    private static int CalculateBuildProgressPercent(long _elapsedMilliseconds, long _estimatedDurationMilliseconds)
    {
        if (_estimatedDurationMilliseconds <= 0L)
        {
            return 0;
        }

        double progressRatio = (double)_elapsedMilliseconds / _estimatedDurationMilliseconds;
        int progressPercent = (int)Math.Round(progressRatio * 100.0, MidpointRounding.AwayFromZero);
        int result = Math.Clamp(progressPercent, 1, 95);
        return result;
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
    /// 요청된 플랫폼에 맞는 Addressables Content State 기본 경로를 결정한다.
    /// </summary>
    private string ResolveContentStatePath( string? _requestedPath, eBuildPlatform _platform )
    {
        if ( string.IsNullOrWhiteSpace( _requestedPath ) == false )
        {
            string requestedPath = _requestedPath.Trim();
            return requestedPath;
        }

        if ( _platform == eBuildPlatform.ANDROID )
        {
            return AndroidContentStatePath;
        }

        if ( _platform == eBuildPlatform.IOS )
        {
            return IosContentStatePath;
        }

        return options.DefaultContentStatePath;
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

        [JsonPropertyName("builds")]
        public List<JenkinsBuildState> Builds { get; set; } = new();
    }

    private sealed class JenkinsQueueState
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("why")]
        public string? Why { get; set; }

        [JsonPropertyName("cancelled")]
        public bool Cancelled { get; set; }

        [JsonPropertyName("task")]
        public JenkinsQueueTaskState? Task { get; set; }

        [JsonPropertyName("actions")]
        public List<JenkinsBuildAction> Actions { get; set; } = new();
    }

    private sealed class JenkinsQueueCollectionState
    {
        [JsonPropertyName("items")]
        public List<JenkinsQueueState> Items { get; set; } = new();
    }

    private sealed class JenkinsQueueTaskState
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
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

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("duration")]
        public long Duration { get; set; }

        [JsonPropertyName("estimatedDuration")]
        public long EstimatedDuration { get; set; }

        [JsonPropertyName("actions")]
        public List<JenkinsBuildAction> Actions { get; set; } = new();
    }

    private sealed class JenkinsBuildAction
    {
        [JsonPropertyName("parameters")]
        public List<JenkinsBuildParameter> Parameters { get; set; } = new();
    }

    private sealed class JenkinsBuildParameter
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public JsonElement Value { get; set; }
    }
}
