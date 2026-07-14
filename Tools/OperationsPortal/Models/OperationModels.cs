namespace TinyHero.OperationsPortal.Models;

public sealed record PortalStatusResponse(
    ServiceStatus Jenkins,
    ServiceStatus ContentServer,
    ContentStatus Content,
    PortalDefaults Defaults);

public sealed record ServiceStatus(bool IsOnline, string Label, string Detail);

public sealed record ContentStatus(
    bool IsReady,
    string RootPath,
    int FileCount,
    long TotalBytes,
    DateTimeOffset? LastPublishedAtUtc,
    DeploymentRecord? LastDeployment);

public sealed record PortalDefaults(
    string JenkinsJobName,
    string ContentBaseUrl,
    string ContentStatePath,
    string PublishPath,
    string LocalContentRoot);

public sealed record JenkinsBuildRequest(
    string? ContentStatePath,
    bool RequireRemoteContent = false);

public sealed record JenkinsTriggerResult(bool IsTriggered, string Message, string? QueueUrl);

public sealed record DeploymentRecord(
    string Id,
    DateTimeOffset PublishedAtUtc,
    string PackageName,
    string ReleaseNote,
    string Sha256,
    int FileCount,
    long TotalBytes,
    string TargetPath);

public sealed record ContentPackageUploadResult(bool IsPublished, string Message, DeploymentRecord? Deployment);
