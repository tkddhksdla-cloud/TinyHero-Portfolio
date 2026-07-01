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
- Data Import / Editor Tools

## Technical Highlights

- Singleton Manager Bootstrap
- Data-driven Gameplay
- Addressables-first Resource Loading
- Resources Fallback Compatibility
- Snapshot-based Save / Load
- Secure Runtime Numeric Values
- AES Save Encryption + HMAC Integrity Check
- EditorWindow-based Data Tooling

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
    Player/               # 플레이어 관련 시스템
    Object/               # 몬스터, NPC, 포탈, 월드 아이템
    UI/                   # 도메인별 UI
    Editor/               # 데이터 import 및 생성 툴
    Tools/Editor/         # 제작 편의 EditorWindow
Docs/
  TechnicalRoadmap.md     # 기술 도입 로드맵
```

## Technical Roadmap

- 완료: Addressables 기반 UI/맵 리소스 로딩
- 완료: Secure Value 기반 런타임 수치 보호
- 완료: AES/HMAC 기반 세이브 파일 보호
- 예정: 데이터 검증 EditorWindow
- 예정: EditMode Test
- 예정: HybridCLR 기반 제한적 코드 핫픽스

자세한 진행 내역은 [TechnicalRoadmap.md](Docs/TechnicalRoadmap.md)를 참고합니다.

## Run

1. Unity `6000.3.15f1` 이상 버전으로 프로젝트를 엽니다.
2. `Assets/Scenes/SceneTitle.unity` 또는 `Assets/Scenes/SceneMap.unity`를 실행합니다.
