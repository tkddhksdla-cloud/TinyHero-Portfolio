# TinyHero 운영 센터

`Release-WindowLayout/TinyHero.OperationsLauncher.exe`를 실행하면 Jenkins(8081), 콘텐츠 서버(8082), 운영 포털(8090)의 ON/OFF 상태를 확인하고 전체 또는 개별로 시작·종료할 수 있습니다.

실행기는 `Tools/OperationsPortal/Manage-TinyHeroOperationsService.ps1`을 호출합니다. 콘텐츠 서버와 운영 포털은 별도 프로세스로 실행되며, Jenkins 종료 전에는 진행 중 빌드 중단 경고를 표시합니다.

## 개발 빌드

```powershell
dotnet build Tools/OperationsLauncher/TinyHero.OperationsLauncher.csproj --configuration Release
```
