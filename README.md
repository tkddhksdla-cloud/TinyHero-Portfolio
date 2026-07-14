# TinyHero

Unity 6 기반 2D 플랫포머 RPG 프로젝트입니다.

## Overview

- 장르: 2D 플랫포머 RPG
- 핵심 루프: 이동, 전투, 퀘스트, 인벤토리, 장비, 스킬, NPC 상호작용, 맵 전환
- 타겟 플랫폼: PC Windows
- 개발 상태: 개인 개발 프로젝트

## Tech Stack

- Engine: Unity 6 `6000.3.15f1`
- Language: C#
- Rendering: Universal Render Pipeline 2D
- Runtime Content Loading: Addressables
- Hot Update: HybridCLR
- CI/CD: Jenkins Pipeline, PowerShell

## Core Systems

- Player Movement / Combat
- Monster / NPC / Portal / World Drop
- Runtime Map Loading
- Quest System
- Inventory / Equipment / Shop
- Active / Passive Skill System
- Audio Management
- Popup / View UI Navigation
- Save / Load Snapshot System
- Object Pooling
- Excel Data Import / Editor Tools

## Technical Highlights

- Singleton Manager Bootstrap
- Data-driven Gameplay
- Addressables-first Resource Loading
- Resources Fallback Compatibility
- AudioMixer-based BGM / SFX Routing
- Snapshot-based Save / Load
- Secure Runtime Numeric Values
- HybridCLR-based Hotfix Module
- AES Save Encryption + HMAC Integrity Check
- Excel-driven Data Pipeline
- EditorWindow-based Data Tooling
- Jenkins-based Windows Custom Build
- Remote Addressables Content Update
- Local Content Operations Portal

## Details

### Addressables-first Resource Loading

UI 팝업, 맵 데이터, 배경 스프라이트, 포탈, 몬스터, NPC 프리팹을 Addressables 우선 로딩 구조로 전환했습니다. 기존 `Resources` 경로는 fallback으로 유지해 로딩 실패나 미등록 에셋 상황에서도 기존 런타임 흐름이 깨지지 않도록 구성했습니다.

### Resource Loading Abstraction

`CResourceManager`와 `CUINavigationController`를 중심으로 리소스 로딩 진입점을 통합했습니다. UI 매니저가 Addressables API를 직접 호출하지 않고 공통 로더를 경유하도록 정리해, 로딩 방식 변경과 fallback 정책을 한 곳에서 관리할 수 있습니다.

### Remote Addressables Content Delivery

런타임 콘텐츠는 빌드에 포함되는 `TinyHero_Local` 그룹과 배포 후 갱신 가능한 `TinyHero_Remote` 그룹으로 분리합니다. 프리팹은 로컬 그룹에 유지하고, 맵 데이터, 배경 이미지, Hotfix DLL, 오디오와 아이템·퀘스트·상점·플레이어·몬스터·텍스트 데이터는 원격 콘텐츠 업데이트 대상으로 관리합니다. 등록은 Unity 메뉴 `TinyHero/Addressables/Sync Runtime Resources`를 사용하며, Addressables key는 `Assets/Resources` 기준 상대 경로에서 확장자를 제거한 형식을 따릅니다.

타이틀 진입 시 원격 카탈로그와 필수 다운로드 크기를 확인합니다. 업데이트가 있으면 확인 팝업과 다운로드 진행 팝업을 표시하고, 다운로드 후 필수 데이터 검증까지 완료해야 게임을 시작할 수 있습니다. 일시적인 Addressables 로드 실패는 제한 횟수만큼 재시도하지만, 새 원격 카탈로그가 적용된 상태에서 필수 데이터 로드가 끝내 실패하면 이전 `Resources` 데이터로 조용히 fallback하지 않고 진입을 차단합니다.

최초 Player 빌드는 원격 카탈로그와 `addressables_content_state.bin`을 생성합니다. 이후 `CONTENT_UPDATE` 빌드는 해당 Player 릴리스의 content state를 기준으로 변경된 Addressables 콘텐츠만 다시 빌드하므로, 전체 실행 파일을 재생성하지 않고 콘텐츠를 배포할 수 있습니다. 상세 절차는 `Tools/Addressables/README.md`를 참고합니다.

### Local Content Operations Portal

`Tools/OperationsPortal`은 로컬 Addressables 콘텐츠 빌드와 배포 상태를 관리하는 ASP.NET Core 운영 대시보드입니다. `Tools/OperationsPortal/Start-TinyHeroOperationsPortal.ps1`을 실행하면 운영 페이지 `http://127.0.0.1:8090`과 게임용 콘텐츠 서버 `http://127.0.0.1:8082/TinyHeroContent`가 함께 실행됩니다.

운영툴에서는 Jenkins `TinyHero-Build-Windows`의 `CONTENT_UPDATE` 빌드를 요청하고, 빌드된 Windows Addressables ZIP을 검증한 뒤 로컬 콘텐츠 경로에 배포할 수 있습니다. 기존 콘텐츠 백업, 실패 시 복원, ZIP 경로 검증과 배포 이력을 포함하며 세부 사용법은 `Tools/OperationsPortal/README.md`를 참고합니다.

### Audio Management

`CAudioManager`는 전역 싱글톤 오디오 매니저로, `CCorePersistentManagerBootstrapper`가 `Assets/Resources/Prefabs/Core/CAudioManager.prefab`을 통해 게임 시작 전에 보장합니다. 매니저 오브젝트는 `DontDestroyOnLoad` 기반으로 유지되며, 씬마다 별도 오디오 매니저를 배치하지 않는 구조입니다.

BGM과 SFX는 `TinyHeroAudioMixer`의 `Master`, `BGM`, `SFX` 그룹으로 분리됩니다. 볼륨은 `MasterVolume`, `BGMVolume`, `SFXVolume` 노출 파라미터를 통해 제어하며, BGM은 2개의 AudioSource를 교차 사용해 페이드 인/아웃 전환을 처리합니다. 같은 BGM이 다시 요청되면 중복 재생하지 않습니다.

런타임 오디오 리소스는 `Assets/Resources/Audio/BGM` 또는 `Assets/Resources/Audio/SFX`에 넣고, 확장자를 제외한 파일 이름으로 호출합니다. 예를 들어 `Assets/Resources/Audio/SFX/SFX_CLICK_00.wav`는 `CAudioManager.Instance.PlaySfx( "SFX_CLICK_00" )`로 재생합니다. 자주 쓰는 SFX는 첫 재생 지연을 줄이기 위해 `PreloadSfx`로 미리 캐시할 수 있습니다.

맵 BGM은 맵툴의 맵 데이터에서 지정하는 흐름을 기본으로 하며, 타이틀 씬은 `SceneTitleBgmRoot`가 시작 BGM을 요청합니다. UI 클릭음은 `CButtonEx` 인스펙터의 SFX 이름을 사용하고 기본값은 `SFX_CLICK_00`입니다.

### HybridCLR Hotfix Module

`TinyHero.Hotfix` 어셈블리를 별도 Hot Update Assembly로 구성하고, `CHotfixRuntimeLoader`가 Addressables 우선 로딩과 `Resources` fallback을 통해 Hotfix DLL을 로드합니다. 메인 빌드의 `MonoBehaviour`와 매니저는 안정적인 Unity Adapter로 유지하고, 스킬 사용, 데미지 계산, 보상 계산, 조건 판정처럼 변경 가능성이 높은 로직은 `IHotfixModule` 기반 모듈로 우회할 수 있도록 설계합니다.

핫픽스 실행은 `CHotfixExecutionContext`와 `CHotfixExecutionResult`를 통해 요청/응답 계약을 유지합니다. Hotfix 모듈이 `SUCCESS`를 반환하면 기존 로직을 대체하고, `FALLBACK`을 반환하면 메인 빌드의 기본 로직이 그대로 실행됩니다. 현재 스킬 사용 흐름은 `CSkillManager`에서 Hotfix 모듈을 먼저 확인한 뒤, 처리 대상이 아니면 기존 Active Skill 로직으로 이어지는 구조입니다.

### Runtime Map Loading

`CMapManager`는 맵 데이터, 배경, 포탈, 몬스터, NPC 리소스를 맵 전환 흐름 안에서 비동기로 준비합니다. 맵툴에서 저장한 데이터 구조와 런타임 스폰 마커를 기준으로 동작하며, 씬 오브젝트 하드코딩 의존을 줄이는 방향으로 구성했습니다.

### Secure Runtime Values

`CSecureInt`, `CSecureLong`, `CSecureFloat` 구조체를 통해 메모리 상 핵심 수치를 암호화 값과 난수 키, 무결성 검증값으로 보관합니다. HP/MP, 레벨, 경험치, 스탯 포인트, 인벤토리 수량, 스킬 포인트, 최종 데미지처럼 변조 가치가 높은 값부터 제한적으로 적용했습니다.

### Protected Save Data

저장 시스템은 `CGameSaveData` 스냅샷을 기준으로 플레이어 스탯, 인벤토리, 장비, 퀘스트, 스킬 상태를 저장합니다. 저장 파일은 AES로 암호화하고 HMAC으로 무결성을 검증하며, 기존 평문 세이브 로드 호환성도 유지합니다.

### Excel-driven Data Pipeline

플레이어 스탯, 몬스터 스탯, 아이템, 퀘스트, 상점, 스킬, 보상 테이블 등 데이터 기반 콘텐츠를 엑셀 import 및 EditorWindow 툴을 통해 관리합니다. 반복적인 ScriptableObject 생성과 데이터 입력을 툴링으로 줄이고, 런타임 시스템은 정리된 정의 데이터를 참조하는 구조를 지향합니다.

런타임 UI와 콘텐츠 문구는 `Assets/RawData/Excel/Text/TextTableData.xlsx`에서 TextKey와 언어별 문자열을 관리하고, 파싱된 `Assets/Resources/Data/Text/TextTableData.asset`을 `CDataManager.GetText`로 조회합니다. 원격 텍스트 테이블이 갱신되면 다운로드 검증 단계에서 `CDataManager` 캐시를 재구성해 새 문구를 적용합니다.

### Editor Tooling

퀘스트, 상점, 아이템, NPC 상호작용, 몬스터 행동 패턴, 랜덤박스 보상 테이블 등 주요 콘텐츠 제작을 위한 EditorWindow 도구를 포함합니다. 검색, 생성, 복제, 삭제, 저장, 검증 흐름을 갖춘 제작 도구 중심으로 확장하는 방향을 유지합니다.

### Object Pooling

반복 생성되는 런타임 오브젝트와 UI 효과는 풀링 구조를 우선 적용합니다. 전투, 드랍, 데미지 폰트, 스킬 VFX처럼 빈번하게 생성되는 대상의 인스턴스 비용을 줄이고 맵 전환 시 정리 흐름을 관리합니다.

런타임 Hierarchy에서는 풀 컨테이너를 `Pool_Monster`, `Pool_FX`처럼 역할별 부모 아래에 구성해 반복 생성 오브젝트가 루트에 흩어지지 않도록 관리합니다.

### Jenkins-based Windows Custom Build

`Jenkinsfile`과 `Tools/CI/Invoke-TinyHeroCustomBuild.ps1`를 통해 PC Windows 빌드를 Unity batchmode에서 실행할 수 있도록 구성했습니다. 빌드 파이프라인은 HybridCLR 준비, Hotfix DLL 준비, Addressables 콘텐츠 빌드, Windows Player 빌드를 순차적으로 수행합니다.

빌드 산출물은 Jenkins build number 기준으로 분리된 `Builds/Windows/<BUILD_NUMBER>` 경로에 생성되며, `Logs/TinyHeroCustomBuild.log`를 통해 Unity 빌드 로그를 추적할 수 있습니다.

Jenkins Item `TinyHero-Build-Windows`는 `PLAYER_BUILD`와 `CONTENT_UPDATE` 모드를 제공합니다. 로컬 Jenkins는 Unity MCP의 `8080` 포트와 충돌하지 않도록 `http://localhost:8081`을 사용하며, 콘텐츠 서버 주소는 기본적으로 `http://127.0.0.1:8082/TinyHeroContent`를 사용합니다.

## Scenes

- `SceneTitle`: 타이틀 씬
- `SceneMap`: 메인 플레이 씬
- `SceneMapTool`: 맵 제작/검증 씬

## Project Structure

```text
Assets/
  AddressableAssetsData/  # Addressables 설정
  Resources/              # 런타임 로드 리소스
    Audio/                # BGM, SFX, AudioMixer
  Scenes/                 # 주요 씬
  Scripts/
    Core/                 # 공용 매니저, 저장, 맵, 스킬, UI 기반
      Audio/              # 전역 오디오 매니저
    Hotfix/               # HybridCLR Hot Update Assembly
    HotfixContracts/      # Hotfix 요청/응답 계약
    Player/               # 플레이어 관련 시스템
    Object/               # 몬스터, NPC, 포탈, 월드 아이템
    UI/                   # 도메인별 UI
    Editor/               # 엑셀 데이터 import 및 생성 툴
    Tools/Editor/         # 제작 편의 EditorWindow
Docs/
  TechnicalRoadmap.md     # 기술 도입 로드맵
Tools/
  Addressables/           # 원격 콘텐츠 빌드/배포 스크립트
  OperationsPortal/       # 로컬 콘텐츠 운영 대시보드
```
