# TinyHero Operations Portal

TinyHero 로컬 Addressables 콘텐츠 빌드와 배포를 관리하는 전용 운영 대시보드입니다.

## 실행

```powershell
./Tools/OperationsPortal/Start-TinyHeroOperationsPortal-Desktop.ps1
```

브라우저에서 `http://127.0.0.1:8090`으로 접속합니다. 포털은 로컬 전용으로 `127.0.0.1`에만 바인딩됩니다.

바탕화면의 `TinyHero-운영툴.ps1` 또는 위 스크립트를 실행하면 Jenkins, 운영툴, 게임용 콘텐츠 서버가 함께 실행되고 브라우저가 자동으로 열립니다. Jenkins가 이미 실행 중이면 기존 프로세스를 재사용합니다.

세 서비스를 함께 종료하려면 바탕화면의 `TinyHero-운영툴-종료.ps1`을 실행합니다. 종료 스크립트는 `8081`, `8082`, `8090` 포트에서 TinyHero 관련 프로세스만 확인해 종료합니다.

## 주요 기능

- Jenkins 멀티플랫폼 파이프라인의 `PLAYER_BUILD`, `CONTENT_UPDATE` 빌드 실행
- 게임 버전, 플레이어 출력 경로, 원격 콘텐츠 필수 정책 입력
- Jenkins 대기열, 진행 중 빌드, 최근 빌드 결과 자동 갱신
- Jenkins 및 로컬 콘텐츠 배포 상태 확인
- 빌드된 Addressables Windows/Android/iOS ZIP 검증 및 플랫폼별 즉시 배포
- 기존 콘텐츠 자동 백업과 실패 시 원상 복구
- ZIP 경로 탈출 방지, 카탈로그·해시·번들 구성 검증
- 최근 직접 배포 이력과 SHA-256 기록

## 설정

`appsettings.json`의 `OperationsPortal` 섹션에서 Jenkins 주소, 콘텐츠 서버 물리 경로와 공개 URL을 설정합니다.

Jenkins 인증이 필요한 경우 환경 변수를 사용합니다.

```powershell
$env:TINYHERO_JENKINS_USER = "jenkins-user"
$env:TINYHERO_JENKINS_TOKEN = "api-token"
```

환경 변수를 사용하지 않는 경우 운영 페이지의 `Jenkins 인증` 버튼에서 사용자 이름과 API 토큰 또는 비밀번호를 최초 한 번 입력합니다. 인증 정보는 `App_Data` 아래에 ASP.NET Core Data Protection으로 암호화되어 저장되며 저장소에는 포함되지 않습니다.

기본 로컬 콘텐츠 경로는 `C:/TinyHeroLocalServer/TinyHeroContent`, 게임 콘텐츠 URL은 `http://127.0.0.1:8082/TinyHeroContent`입니다.

## ZIP 구조

다음 중 하나의 구조를 지원합니다.

```text
ServerData/
  StandaloneWindows64/ # Android 또는 iOS도 가능
    catalog_*.json
    catalog_*.hash
    *.bundle
```

```text
StandaloneWindows64/ # Android 또는 iOS도 가능
  catalog_*.json
  catalog_*.hash
  *.bundle
```

원본 Unity 에셋은 저장소에 반영한 뒤 Jenkins 업데이트 빌드를 사용합니다. 즉시 업로드는 Unity에서 이미 빌드된 Addressables 서버 패키지를 대상으로 합니다.
