# TinyHero Operations Portal

TinyHero 로컬 Addressables 콘텐츠 빌드와 배포를 관리하는 전용 운영 대시보드입니다.

## 실행

```powershell
./Tools/OperationsPortal/Start-TinyHeroOperationsPortal.ps1
```

브라우저에서 `http://127.0.0.1:8090`으로 접속합니다. 포털은 로컬 전용으로 `127.0.0.1`에만 바인딩됩니다.

운영툴을 실행하면 게임용 콘텐츠 서버도 `http://127.0.0.1:8082/TinyHeroContent`에서 함께 실행되므로 별도 HTTP 서버가 필요하지 않습니다.

## 주요 기능

- Jenkins `TinyHero-Build-Windows`의 `CONTENT_UPDATE` 빌드 실행
- Jenkins 및 로컬 콘텐츠 배포 상태 확인
- 빌드된 Addressables Windows ZIP 검증 및 즉시 배포
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

기본 로컬 콘텐츠 경로는 `C:/TinyHeroLocalServer/TinyHeroContent`, 게임 콘텐츠 URL은 `http://127.0.0.1:8082/TinyHeroContent`입니다.

## ZIP 구조

다음 중 하나의 구조를 지원합니다.

```text
ServerData/
  Windows/
    catalog_*.json
    catalog_*.hash
    *.bundle
```

```text
Windows/
  catalog_*.json
  catalog_*.hash
  *.bundle
```

원본 Unity 에셋은 저장소에 반영한 뒤 Jenkins 업데이트 빌드를 사용합니다. 즉시 업로드는 Unity에서 이미 빌드된 Addressables 서버 패키지를 대상으로 합니다.
