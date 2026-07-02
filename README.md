# TinyHero

Unity 6 기반 2D 플랫포머 RPG 프로젝트입니다.

## Overview

- 장르: 2D 플랫포머 RPG
- 핵심 루프: 이동, 전투, 퀘스트, 인벤토리, 장비, 스킬, NPC 상호작용, 맵 전환
- 타겟 플랫폼: PC Windows
- 개발 상태: 개인 개발 프로젝트

## Tech Stack

- Unity 6 `6000.3.15f1`
- C#
- Universal Render Pipeline 2D
- UGUI / TextMeshPro
- Cinemachine
- ScriptableObject
- Resources + Addressables Hybrid Loading
- HybridCLR Hotfix Assembly
- AES/HMAC Protected Save Data
- Unity EditorWindow Tooling

## Core Systems

- Player Movement / Combat
- Monster / NPC / Portal / World Drop
- Runtime Map Loading
- Quest System
- Inventory / Equipment / Shop
- Active / Passive Skill System
- Popup / View UI Navigation
- Save / Load Snapshot System
- Object Pooling
- Excel Data Import / Editor Tools

## Technical Highlights

- Singleton Manager Bootstrap
- Data-driven Gameplay
- Addressables-first Resource Loading
- Resources Fallback Compatibility
- Snapshot-based Save / Load
- Secure Runtime Numeric Values
- HybridCLR-based Hotfix Module
- AES Save Encryption + HMAC Integrity Check
- Excel-driven Data Pipeline
- EditorWindow-based Data Tooling

## Details

### Addressables-first Resource Loading

UI 팝업, 맵 데이터, 배경 스프라이트, 포탈, 몬스터, NPC 프리팹을 Addressables 우선 로딩 구조로 전환했습니다. 기존 `Resources` 경로는 fallback으로 유지해 로딩 실패나 미등록 에셋 상황에서도 기존 런타임 흐름이 깨지지 않도록 구성했습니다.

### Resource Loading Abstraction

`CResourceManager`와 `CUINavigationController`를 중심으로 리소스 로딩 진입점을 통합했습니다. UI 매니저가 Addressables API를 직접 호출하지 않고 공통 로더를 경유하도록 정리해, 로딩 방식 변경과 fallback 정책을 한 곳에서 관리할 수 있습니다.

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

### Editor Tooling

퀘스트, 상점, 아이템, NPC 상호작용, 몬스터 행동 패턴, 랜덤박스 보상 테이블 등 주요 콘텐츠 제작을 위한 EditorWindow 도구를 포함합니다. 검색, 생성, 복제, 삭제, 저장, 검증 흐름을 갖춘 제작 도구 중심으로 확장하는 방향을 유지합니다.

### Object Pooling

반복 생성되는 런타임 오브젝트와 UI 효과는 풀링 구조를 우선 적용합니다. 전투, 드랍, 데미지 폰트, 스킬 VFX처럼 빈번하게 생성되는 대상의 인스턴스 비용을 줄이고 맵 전환 시 정리 흐름을 관리합니다.

## Scenes

- `SceneTitle`: 타이틀 씬
- `SceneMap`: 메인 플레이 씬
- `SceneMapTool`: 맵 제작/검증 씬

## Project Structure

```text
Assets/
  AddressableAssetsData/  # Addressables 설정
  Resources/              # 런타임 로드 리소스
  Scenes/                 # 주요 씬
  Scripts/
    Core/                 # 공용 매니저, 저장, 맵, 스킬, UI 기반
    Hotfix/               # HybridCLR Hot Update Assembly
    HotfixContracts/      # Hotfix 요청/응답 계약
    Player/               # 플레이어 관련 시스템
    Object/               # 몬스터, NPC, 포탈, 월드 아이템
    UI/                   # 도메인별 UI
    Editor/               # 엑셀 데이터 import 및 생성 툴
    Tools/Editor/         # 제작 편의 EditorWindow
Docs/
  TechnicalRoadmap.md     # 기술 도입 로드맵
```
