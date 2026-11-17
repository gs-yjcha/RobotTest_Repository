# RobotTest 기획안 — Tablet 작업 지시 시스템

- 버전: v3 · 2026-11-17 (v1·v2 → `이전버전/`)
- 상태: **승인** (2026-11-17 사용자 승인. 구성 방향은 사용자가 정했고, 세부 구조는 AI가 제안했다)
- 범위: 범용 모바일 로봇 작업 지시 시험. 특정 고객 프로젝트 사양을 담지 않는다.

### v3 변경점 (2026-11-17 사용자 결정)

- 1단계 연구는 **Robot Agent를 거치지 않고 Tablet이 MobED에 직접 gRPC 명령**을 보낸다.
- Tablet 앱을 **웹(React) → WPF(.NET) 데스크톱 앱**으로 바꾼다. Windows 태블릿 전용이므로 웹을 거칠 이유가 없다.
- 인증서는 **폴더를 지정하고 그 안의 파일을 골라** 쓴다. 접속 정보는 태블릿 로컬에 저장한다.
- Robot Agent·차상 PC 구성은 **이후 단계로 미룬다**. MobED 통신 코드를 분리해 두어 나중에 Agent로 옮길 수 있게 한다.
- 터치 길게 누르기(우클릭 변환)는 WPF 설정으로 끈다. 사용자 확인: 방법이 있으면 문제없음.

## 1. 목표

- **최종:** Tablet ↔ RCS ↔ MobED x N. RCS는 기존 외부 개발품을 쓴다.
- **1단계 (연구):** Tablet(WPF) → MobED x 1 직접 연결. 연결·인증, 상태 모니터링, 목적지 이동, 정지, 위치 보정, 로컬 설정을 시험한다.
- **이후:** 차상 PC Robot Agent 도입(I/O·안전 감시 로컬화) → RCS 연동.

## 2. 연결 방식 결정 — Tablet 직접 연결

| 후보 | 장점 | 단점 |
|---|---|---|
| A. Tablet → MobED 직접 (1단계 채택) | 장비 추가 없음, 구조 단순, 연구 시작 빠름 | Wi-Fi 끊김 = 제어 끊김, I/O 원격 연결 필요, 다중 로봇 확장 시 구조 변경 |
| B. Tablet → 차상 PC Agent → MobED (v2안) | I/O 직결, 유선 제어, 로컬 안전 감시, RCS 구조와 같음 | 차상 PC·Agent 개발 필요 |

A를 1단계로 고른 이유: 지금 목표는 **MobED API 명령 송수신 검증**이다. Agent 없이 태블릿에서 바로 명령이 오가는지 먼저 확인한다. 통신 코드는 `Mobed.Client`로 분리해 B로 넘어갈 때 Agent 안으로 옮긴다.

### 직접 연결에서 빠지는 것과 대응

| Agent가 맡던 기능 | 1단계 대응 |
|---|---|
| 하트비트 감시 (Wi-Fi 끊김 시 정지 요청) | 앱 종료·비활성화·연결 끊김 감지 시 정지 요청. **연결이 끊겼을 때 로봇이 스스로 멈추는지 확인 필요.** 본체 비상정지 버튼 접근성 확보 |
| Remote I/O·인터록 | **1차 범위에서 제외** (2026-11-17 승인). 필요하면 태블릿이 I/O 모듈에 직접 연결 |
| 작업 상태 머신 (Job Manager) | 태블릿 `Tablet.Core`로 옮긴다 |

## 3. 시스템 구성

### 1단계 — 물리 배치

```
┌─ Windows 태블릿 ───────────────────────────────┐
│  Tablet App (WPF, 전체화면)                     │
│   ├─ Views · ViewModels (MVVM)                  │
│   ├─ Tablet.Core: Job 상태 머신 · IRobotClient  │
│   ├─ Mobed.Client: gRPC · TLS · 인증 (local/)   │
│   └─ Tablet.Mock: Mock Robot (실기 없이 시험)   │
└──────────────────────┬─────────────────────────┘
                       │ Wi-Fi · gRPC (TLS, 토큰 인증)
                    [MobED]
```

### 이후 — Agent·RCS 도입

```
[Tablet App] ──▶ [Robot Agent (차상 PC)] ──유선 gRPC──▶ MobED
                        └─ Mobed.Client 를 그대로 옮김, I/O·Watchdog 추가
[Tablet App] ──▶ [RCS] ──▶ [Robot Agent] x N ──▶ MobED x N
```

태블릿은 `IRobotClient` 인터페이스로만 로봇과 말한다. 구현을 `MobedDirectClient` → `AgentClient`로 바꾸면 화면 코드는 그대로 쓴다.

## 4. 1단계 기능

| 영역 | 기능 | 비고 |
|---|---|---|
| 연결 | 접속 주소, 인증서 폴더 지정 → 파일 선택, API Key, 단계별 연결 시험 | UI 시안 v9 설정 화면 |
| 상태 | 연결, 주행 가능 여부(불가 사유), 배터리, 위치·일치율, 비상정지 | 상태 스트림 구독 |
| 작업 | 목적지(POI) 이동, 일시정지·재개·취소, 도착 후 완료 승인 | 주행 명령 API |
| 주행 설정 | 회피/정차 모드, 속도 단계 | 모드별 API 대응 확인 필요 |
| 위치 보정 | 지도에서 지정, 등록 목적지에서 지정 | |
| 수동 주행 | 누르는 동안 이동, 수동 주행·지그재그·프리스타일 | 원격 수동 조종 API 제공 여부 **확인 필요** — 확인 후 범위 결정 |
| 목적지 | 현재 위치를 목적지로 등록, 이름 변경, 삭제 | MobED POI |
| 맵 | 맵 표시, 영역(Zone) 편집 | 1차는 표시 우선 |
| 정지 | 전체 정지 (상단 고정) | |
| I/O | — | 1차 제외 |

## 5. 저장소 구성 (예정)

| 경로 | 역할 | git |
|---|---|---|
| `apps/tablet-wpf/RobotTest.Tablet.sln` | 솔루션 | 포함 |
| `apps/tablet-wpf/Tablet.App` | WPF 화면 (Views·ViewModels·리소스) | 포함 |
| `apps/tablet-wpf/Tablet.Core` | 모델, Job 상태 머신, `IRobotClient` | 포함 |
| `apps/tablet-wpf/Tablet.Mock` | Mock Robot | 포함 |
| `apps/tablet-wpf/Tablet.Tests` | 단위 시험 (상태 머신·Mock 시나리오) | 포함 |
| `local/Mobed.Client` | MobED gRPC 구현 (proto·생성 코드 포함) | **제외** |
| `apps/tablet-web` | 기존 React 시안 코드. 보류, 참고용 (2026-11-17 승인) | 미추적 유지 |

- MobED proto·생성 코드는 비밀유지 자료라 공개 저장소에 넣지 않는다(`.gitignore`의 `*.proto`, `**/generated/`, `local/`). `Tablet.App`은 `local/Mobed.Client`가 있으면 실기 모드, 없으면 Mock 모드로 빌드한다.
- `.gitignore`에 .NET 빌드 산출물(`bin/`, `obj/`, `*.user`)을 T1에서 추가한다.

### 기술 선택

- **.NET 10 (LTS) + WPF.** .NET 8 지원은 2026-11에 끝났으므로 새 프로젝트는 .NET 10으로 시작한다.
- MVVM: `CommunityToolkit.Mvvm`. gRPC: `Grpc.Net.Client`, `Google.Protobuf`, `Grpc.Tools`. 시험: xUnit.
- 폴더 선택: WPF `OpenFolderDialog`(전체 경로 획득).
- 설정 저장: `%LOCALAPPDATA%\RobotTest\settings.json`. API Key는 Windows DPAPI(`ProtectedData`)로 암호화해 저장하고, 화면에는 앞 4자리만 보인다.
- 실행: 전체화면(키오스크) 창. 필요하면 로그온 시 자동 실행.

### 터치 처리 (WPF)

- 길게 누르기 → 우클릭 변환 끔: `Stylus.IsPressAndHoldEnabled="False"` (수동 주행·지도 영역).
- 누르는 동안 이동 버튼: `TouchDown`에서 시작·터치 캡처, `TouchUp`·`LostTouchCapture`·창 `Deactivated`에서 **반드시 정지**.
- 지도 확대·이동: `ManipulationDelta`(두 손가락).
- 버튼 크기·화면 배치는 UI 시안(Surface Pro 11, 1440×960 기준)을 따른다.

## 6. 화면 구성

UI 시안 `UI 시안/태블릿 전체_UI시안_261117_v10.html`을 화면 설계 기준으로 쓴다(2026-11-18 사용자 승인).
v10 외형·화면 구성만 먼저 WPF에 반영한다. I/O는 1차 제외, 연결 표기는 MobED 직접 연결 기준이다. 실제 주행·작업 상태 머신·설정 영구 저장은 T2~T7의 기존 완료 기준으로 별도 검증한다.

| 시안 화면 | WPF View |
|---|---|
| 상단 상태바·전체 정지 | `ShellView` 상단 |
| 운행 (작업 지시·위치 보정·수동 주행 탭) | `OperateView` |
| 맵 편집 | `MapEditView` |
| 알람·이력 | `LogView` |
| 설정 (로봇 연결·연결 시험 포함) | `SettingsView` |
| Remote I/O | 1차 제외 — 메뉴에서 숨김 |

직접 연결에 맞춰 시안의 `Agent` 표기(상태바 칩, 설정 문구)는 구현 시 `MobED 연결` 기준으로 바꾼다.

## 7. Job 상태 머신 (태블릿 내부)

```
QUEUED → MOVING → ARRIVED → WAIT_CONFIRM → DONE
            │
            └──▶ PAUSED ─▶ MOVING (재개)
            └──▶ FAILED / CANCELED
```

로봇의 주행 상태(대기·이동·일시정지·성공·취소·실패)를 받아 위 상태로 옮긴다.

## 8. 작업 단위

| T | 내용 | 완료 기준 |
|---|---|---|
| T0 | 기획안 v3 | 사용자 승인 (2026-11-17 완료) |
| T1 | WPF 솔루션 골격, `.gitignore` 보강, Mock 모드 실행 | 빈 셸 화면이 태블릿 전체화면으로 뜸 |
| T2 | `Tablet.Core`: 모델·`IRobotClient`·Job 상태 머신, Mock Robot | 상태 전이 단위 시험 통과 |
| T3 | 설정 화면: 연결 정보·인증서 폴더·API Key 저장, 연결 시험(Mock) | 저장·재실행 후 값 유지, Key 암호화 확인 |
| T4 | `local/Mobed.Client`: 인증·서버 판본 조회·주행 가능 상태 스트림 | **실기: 연결 시험 6단계 통과** (사용자 입회) |
| T5 | 운행 화면: 상태 표시·목적지 이동·일시정지·재개·취소·정지 | 실기: 목적지 2곳 왕복, 정지 확인 |
| T6 | 위치 보정, 현재 위치 목적지 등록 | 실기: 보정 후 일치, POI 등록 확인 |
| T7 | 수동 주행·맵 편집 (API 확인 결과에 따라) | 범위 확정 후 정함 |

각 단계는 완료 기준 확인 후 커밋·push한다.

## 9. 확인 필요

- 태블릿 Wi-Fi로 MobED gRPC 서버에 직접 닿는지 (MobED AP / 현장 AP, 주소·포트)
- 연결이 끊겼을 때 로봇이 스스로 멈추는지, 멈추기까지 시간
- 원격 수동 조종 API 제공 여부와 판본, 조종기 지그재그·프리스타일 모드의 API 대응
- 회피/정차 주행 모드의 API 대응
- (결정됨) Remote I/O 1차 제외, `apps/tablet-web` 보류·참고용 유지 — 2026-11-17 승인
