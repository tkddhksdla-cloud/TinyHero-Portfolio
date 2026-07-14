using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using TinyHero.OperationsPortal.Models;

namespace TinyHero.OperationsPortal.Services;

public sealed class JenkinsCredentialService
{
    private readonly IDataProtector dataProtector;
    private readonly string credentialFilePath;
    private readonly object syncRoot = new();

    public JenkinsCredentialService(IDataProtectionProvider _dataProtectionProvider, IWebHostEnvironment _environment)
    {
        dataProtector = _dataProtectionProvider.CreateProtector("TinyHero.OperationsPortal.JenkinsCredentials.v1");
        string dataDirectoryPath = Path.Combine(_environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectoryPath);
        credentialFilePath = Path.Combine(dataDirectoryPath, "jenkins-credentials.dat");
    }

    /// <summary>
    /// 현재 Jenkins 인증 정보의 설정 상태를 반환한다.
    /// </summary>
    public JenkinsCredentialStatus GetStatus()
    {
        JenkinsCredential? credential = GetCredential();
        JenkinsCredentialStatus result = new(credential != null, credential?.UserName);
        return result;
    }

    /// <summary>
    /// Jenkins 인증 정보를 로컬 암호화 파일에 저장한다.
    /// </summary>
    public JenkinsCredentialStatus Save(JenkinsCredentialRequest _request)
    {
        string userName = _request.UserName?.Trim() ?? string.Empty;
        string apiToken = _request.ApiToken?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(apiToken))
        {
            throw new ArgumentException("Jenkins 사용자 이름과 API 토큰 또는 비밀번호가 필요합니다.");
        }

        JenkinsCredential credential = new(userName, apiToken);
        string json = JsonSerializer.Serialize(credential);
        string protectedText = dataProtector.Protect(json);

        lock (syncRoot)
        {
            File.WriteAllText(credentialFilePath, protectedText);
        }

        JenkinsCredentialStatus result = new(true, userName);
        return result;
    }

    /// <summary>
    /// 저장된 Jenkins 인증 정보를 제거한다.
    /// </summary>
    public void Clear()
    {
        lock (syncRoot)
        {
            if (File.Exists(credentialFilePath))
            {
                File.Delete(credentialFilePath);
            }
        }
    }

    /// <summary>
    /// 환경 변수 또는 로컬 암호화 파일에서 Jenkins 인증 정보를 반환한다.
    /// </summary>
    public JenkinsCredential? GetCredential()
    {
        string? environmentUserName = Environment.GetEnvironmentVariable("TINYHERO_JENKINS_USER");
        string? environmentApiToken = Environment.GetEnvironmentVariable("TINYHERO_JENKINS_TOKEN");

        if (string.IsNullOrWhiteSpace(environmentUserName) == false && string.IsNullOrWhiteSpace(environmentApiToken) == false)
        {
            return new JenkinsCredential(environmentUserName.Trim(), environmentApiToken.Trim());
        }

        lock (syncRoot)
        {
            if (File.Exists(credentialFilePath) == false)
            {
                return null;
            }

            try
            {
                string protectedText = File.ReadAllText(credentialFilePath);
                string json = dataProtector.Unprotect(protectedText);
                JenkinsCredential? result = JsonSerializer.Deserialize<JenkinsCredential>(json);
                return result;
            }
            catch
            {
                return null;
            }
        }
    }
}
