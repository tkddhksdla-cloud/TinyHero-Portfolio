using System.IO.Compression;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using TinyHero.OperationsPortal.Configuration;
using TinyHero.OperationsPortal.Models;

namespace TinyHero.OperationsPortal.Services;

public sealed class ContentPackageService
{
    private const int MaximumArchiveEntryCount = 20000;
    private readonly OperationsPortalOptions options;
    private readonly DeploymentHistoryService deploymentHistoryService;
    private readonly string workingDirectoryPath;
    private readonly SemaphoreSlim publishLock = new(1, 1);

    public ContentPackageService(
        IOptions<OperationsPortalOptions> _options,
        DeploymentHistoryService _deploymentHistoryService,
        IWebHostEnvironment _environment)
    {
        options = _options.Value;
        deploymentHistoryService = _deploymentHistoryService;
        workingDirectoryPath = Path.Combine(_environment.ContentRootPath, "App_Data", "packages");
        Directory.CreateDirectory(workingDirectoryPath);
    }

    public async Task<ContentPackageUploadResult> PublishAsync(
        IFormFile _packageFile,
        string? _releaseNote,
        CancellationToken _cancellationToken)
    {
        if (_packageFile.Length <= 0L)
        {
            return new ContentPackageUploadResult(false, "업로드된 파일이 비어 있습니다.", null);
        }

        if (_packageFile.Length > options.MaximumUploadBytes)
        {
            return new ContentPackageUploadResult(false, "업로드 허용 용량을 초과했습니다.", null);
        }

        if (string.Equals(Path.GetExtension(_packageFile.FileName), ".zip", StringComparison.OrdinalIgnoreCase) == false)
        {
            return new ContentPackageUploadResult(false, "Addressables 콘텐츠 ZIP 파일만 업로드할 수 있습니다.", null);
        }

        await publishLock.WaitAsync(_cancellationToken);
        string operationId = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        string operationDirectoryPath = Path.Combine(workingDirectoryPath, operationId);
        string archivePath = Path.Combine(operationDirectoryPath, "content.zip");
        string extractDirectoryPath = Path.Combine(operationDirectoryPath, "extract");

        try
        {
            Directory.CreateDirectory(extractDirectoryPath);
            await using (FileStream archiveStream = File.Create(archivePath))
            {
                await _packageFile.CopyToAsync(archiveStream, _cancellationToken);
            }

            string sha256 = await CalculateSha256Async(archivePath, _cancellationToken);
            ExtractArchiveSafely(archivePath, extractDirectoryPath);
            string contentDirectoryPath = FindWindowsContentDirectory(extractDirectoryPath);
            ValidateContentDirectory(contentDirectoryPath);
            DeploymentRecord deployment = await DeployAsync(
                operationId,
                _packageFile.FileName,
                _releaseNote,
                sha256,
                contentDirectoryPath,
                _cancellationToken);
            await deploymentHistoryService.AddAsync(deployment, _cancellationToken);
            return new ContentPackageUploadResult(true, "콘텐츠 패키지가 로컬 서버에 배포되었습니다.", deployment);
        }
        catch (Exception exception)
        {
            return new ContentPackageUploadResult(false, exception.Message, null);
        }
        finally
        {
            publishLock.Release();
            TryDeleteDirectory(operationDirectoryPath);
        }
    }

    private async Task<DeploymentRecord> DeployAsync(
        string _operationId,
        string _packageName,
        string? _releaseNote,
        string _sha256,
        string _contentDirectoryPath,
        CancellationToken _cancellationToken)
    {
        string localContentRoot = Path.GetFullPath(options.LocalContentRoot);
        string operationsRootPath = Path.Combine(localContentRoot, ".operations");
        string stagingRootPath = Path.Combine(operationsRootPath, "staging", _operationId);
        string stagingWindowsPath = Path.Combine(stagingRootPath, "Windows");
        string targetWindowsPath = Path.Combine(localContentRoot, "Windows");
        string backupWindowsPath = Path.Combine(operationsRootPath, "backups", _operationId, "Windows");
        Directory.CreateDirectory(stagingWindowsPath);

        if (Directory.Exists(targetWindowsPath))
        {
            CopyDirectory(targetWindowsPath, stagingWindowsPath);
        }

        CopyDirectory(_contentDirectoryPath, stagingWindowsPath);
        Directory.CreateDirectory(Path.GetDirectoryName(backupWindowsPath)!);

        bool wasTargetMoved = false;

        try
        {
            if (Directory.Exists(targetWindowsPath))
            {
                Directory.Move(targetWindowsPath, backupWindowsPath);
                wasTargetMoved = true;
            }

            Directory.Move(stagingWindowsPath, targetWindowsPath);
        }
        catch
        {
            if (Directory.Exists(targetWindowsPath))
            {
                Directory.Delete(targetWindowsPath, true);
            }

            if (wasTargetMoved && Directory.Exists(backupWindowsPath))
            {
                Directory.Move(backupWindowsPath, targetWindowsPath);
            }

            throw;
        }
        finally
        {
            TryDeleteDirectory(stagingRootPath);
        }

        FileInfo[] publishedFileArray = new DirectoryInfo(targetWindowsPath).GetFiles("*", SearchOption.AllDirectories);
        long totalBytes = publishedFileArray.Sum(_file => _file.Length);
        DateTimeOffset publishedAtUtc = DateTimeOffset.UtcNow;
        DeploymentRecord deployment = new(
            _operationId,
            publishedAtUtc,
            Path.GetFileName(_packageName),
            string.IsNullOrWhiteSpace(_releaseNote) ? "설명 없음" : _releaseNote.Trim(),
            _sha256,
            publishedFileArray.Length,
            totalBytes,
            targetWindowsPath);
        string manifestPath = Path.Combine(localContentRoot, "TinyHeroContentManifest.json");
        string manifestJson = System.Text.Json.JsonSerializer.Serialize(deployment, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(manifestPath, manifestJson, _cancellationToken);
        return deployment;
    }

    private static void ExtractArchiveSafely(string _archivePath, string _extractDirectoryPath)
    {
        string normalizedRootPath = Path.GetFullPath(_extractDirectoryPath) + Path.DirectorySeparatorChar;
        long extractedBytes = 0L;

        using ZipArchive archive = ZipFile.OpenRead(_archivePath);

        if (archive.Entries.Count > MaximumArchiveEntryCount)
        {
            throw new InvalidDataException("ZIP 항목 수가 허용 범위를 초과했습니다.");
        }

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string destinationPath = Path.GetFullPath(Path.Combine(_extractDirectoryPath, entry.FullName));

            if (destinationPath.StartsWith(normalizedRootPath, StringComparison.OrdinalIgnoreCase) == false)
            {
                throw new InvalidDataException("ZIP 내부에 허용되지 않은 경로가 포함되어 있습니다.");
            }

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(destinationPath);
                continue;
            }

            extractedBytes += entry.Length;

            if (extractedBytes > 20L * 1024L * 1024L * 1024L)
            {
                throw new InvalidDataException("압축 해제 용량이 허용 범위를 초과했습니다.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            entry.ExtractToFile(destinationPath, true);
        }
    }

    private static string FindWindowsContentDirectory(string _extractDirectoryPath)
    {
        List<string> candidateDirectoryList = Directory
            .EnumerateDirectories(_extractDirectoryPath, "Windows", SearchOption.AllDirectories)
            .Prepend(_extractDirectoryPath)
            .Where(_directory => Directory.EnumerateFiles(_directory, "catalog*.json", SearchOption.TopDirectoryOnly).Any())
            .OrderBy(_directory => _directory.Length)
            .ToList();

        if (candidateDirectoryList.Count == 0)
        {
            throw new InvalidDataException("Windows Addressables 카탈로그를 찾을 수 없습니다.");
        }

        return candidateDirectoryList[0];
    }

    private static void ValidateContentDirectory(string _contentDirectoryPath)
    {
        bool hasCatalog = Directory.EnumerateFiles(_contentDirectoryPath, "catalog*.json", SearchOption.TopDirectoryOnly).Any();
        bool hasCatalogHash = Directory.EnumerateFiles(_contentDirectoryPath, "catalog*.hash", SearchOption.TopDirectoryOnly).Any();
        bool hasBundle = Directory.EnumerateFiles(_contentDirectoryPath, "*.bundle", SearchOption.AllDirectories).Any();

        if (hasCatalog == false || hasCatalogHash == false || hasBundle == false)
        {
            throw new InvalidDataException("카탈로그, 해시, 번들이 모두 포함된 Addressables 패키지가 필요합니다.");
        }
    }

    private static void CopyDirectory(string _sourceDirectoryPath, string _targetDirectoryPath)
    {
        foreach (string directoryPath in Directory.EnumerateDirectories(_sourceDirectoryPath, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(_sourceDirectoryPath, directoryPath);
            Directory.CreateDirectory(Path.Combine(_targetDirectoryPath, relativePath));
        }

        foreach (string filePath in Directory.EnumerateFiles(_sourceDirectoryPath, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(_sourceDirectoryPath, filePath);
            string targetFilePath = Path.Combine(_targetDirectoryPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFilePath)!);
            File.Copy(filePath, targetFilePath, true);
        }
    }

    private static async Task<string> CalculateSha256Async(string _filePath, CancellationToken _cancellationToken)
    {
        await using FileStream fileStream = File.OpenRead(_filePath);
        byte[] hashBytes = await SHA256.HashDataAsync(fileStream, _cancellationToken);
        string result = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return result;
    }

    private static void TryDeleteDirectory(string _directoryPath)
    {
        try
        {
            if (Directory.Exists(_directoryPath))
            {
                Directory.Delete(_directoryPath, true);
            }
        }
        catch
        {
        }
    }
}
