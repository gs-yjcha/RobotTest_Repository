# RobotTest 기획안 — Tablet 작업 지시 시스템

- 버전: v2 · 2026-11-15 (v1 → `이전버전/`)
- 상태: **검토 초안** (구성 방향은 사용자가 정했고, 세부 구조는 AI가 제안했다)
- 범위: 범용 모바일 로봇 작업 지시 시험. 특정 고객 프로젝트 사양을 담지 않는다.

### v2 변경점

- 태블릿을 **Windows 태블릿**으로 확정했다.
- gateway를 **Robot Agent**(로봇 1대당 1개, 차상 PC에서 실행)로 재정의했다.
- 목적지(POI) 등록과 로컬 설정 기능을 추가했다.
- **I/O 제어**를 1단계 범위에 포함했다.
- 2단계 RCS는 **기존 외부 개발 RCS를 연동**한다. 자체 개발하지 않는다.

## 1. 목표

- **최종:** Tablet ↔ RCS ↔ MobED x N. RCS는 기존 외부 개발품을 쓴다.
- **1단계 (2027-03 전시 시연 목표):** Tablet ↔ Robot Agent ↔ MobED x 1. 작업 지시, 목적지 등록, 로컬 설정, I/O 제어, 상태 모니터링을 한다.

## 2. Gateway 결정 — Robot Agent 방식

### 개념

gateway는 "태블릿의 말(작업)을 로봇의 말(API·I/O)로 바꾸는 번역기"다. 이 번역기를 **로봇 1대당 1개씩, 로봇 위 차상 PC에서** 실행한다. 이 프로그램을 **Robot Agent**라 부른다.

### 위치 후보 비교

| 후보 | 장점 | 단점 |
|---|---|---|
| A. 차상 PC (추천) | I/O 직결, MobED 유선 연결, 2단계 그대로 사용 | 차상 PC 필요 |
| B. Windows 태블릿 내부 | 장비 추가 없음, 초기 시험 빠름 | Wi-Fi 끊김 = 제어 끊김, I/O 원격 연결, 2단계에서 옮겨야 함 |
| C. 별도 서버 PC | 중앙 관리 쉬움 | 2단계 RCS 자리와 겹치고, 로봇 수만큼 원격 I/O 필요 |

### A를 추천하는 이유

1. **I/O가 로봇 위에 있다.** 도어, 실린더, 센서 I/O 모듈은 차상에 결선된다. Agent가 옆에 있어야 배선과 응답이 단순하다.
2. **MobED 제어선이 유선이 된다.** Agent ↔ MobED는 로봇 내부 이더넷으로 연결된다. Wi-Fi가 흔들려도 주행 제어와 안전 감시는 로봇 안에서 유지된다.
3. **2단계 구조와 같다.** 기존 RCS는 로봇마다 하나씩 있는 Agent에 접속하는 구조다. Agent에 RCS 연결 모듈만 추가하면 되고, 다시 만들 필요가 없다.
4. **안전 감시가 로컬에서 돈다.** 태블릿 하트비트가 끊기면 Agent가 직접 정지를 요청한다.

### 개발 중 운용

Agent는 같은 프로그램을 **실행 위치만 바꿔서** 쓴다. 차상 PC가 준비되기 전에는 Windows 태블릿이나 노트북에서 Agent + Mock Robot으로 개발한다(B 방식). 실기 시험(T7)부터 차상 PC로 옮긴다.

## 3. 시스템 구성

### 1단계 — 물리 배치

```
┌─ Windows 태블릿 ─────────────┐
│  Tablet App (웹 UI, 전체화면) │
└──────────────┬───────────────┘
               │ Wi-Fi · HTTP + WebSocket
┌─ MobED 차상 ─┴──────────────────────────────────┐
│  차상 PC: Robot Agent                            │
│   ├─ Job Manager (작업 큐·상태 머신)             │
│   ├─ Setup Service (목적지·맵·로봇 설정)          │
│   ├─ Robot Adapter ──유선 이더넷·gRPC──▶ MobED   │
│   ├─ IO Adapter ─────유선──────────▶ I/O 모듈    │
│   │                                  (도어·실린더·센서) │
│   └─ Safety Watchdog (하트비트·정지)              │
└──────────────────────────────────────────────────┘
```

### 2단계 — RCS 연동

```
[Windows 태블릿: 작업 지시 UI] ──▶ [RCS (기존 외부 개발품, 서버 PC)]
                                         │ RCS ↔ Agent 프로토콜 (TCP)
                         ┌───────────────┼───────────────┐
                    [Robot Agent #1] [Robot Agent #2] … [Robot Agent #N]
                         │                │                │
                      MobED #1         MobED #2         MobED #N

[Windows 태블릿: 설정·점검 UI] ─ ─ ─▶ 각 Robot Agent (Setup·IO 직접 접속, 유지보수용)
```

- **작업 지시 경로**는 RCS로 옮긴다. 작업 배정, 다중 로봇 관리, 교통 관리는 RCS가 맡는다.
- **설정·점검 경로**(목적지 등록, I/O 시험)는 2단계에서도 태블릿 ↔ Agent 직접 접속을 유지한다.
- RCS ↔ Agent 프로토콜 상세는 외부 개발사 자료다. **이 저장소에 싣지 않고** `local/`의 비공개 문서로 관리한다.

## 4. Agent 기능 (1단계)

| 영역 | 기능 |
|---|---|
| 작업 | 목적지 이동 작업 생성·취소·일시정지·재개, 도착 후 완료 승인 |
| 목적지 | 현재 위치를 목적지로 등록, 이름 변경, 삭제, 목록 조회 (MobED POI로 저장) |
| 로컬 설정 | 로봇 접속 정보, 사용 맵 선택, 초기 위치 지정, 최대 속도 단계, 도착 허용오차 |
| I/O | 출력 On/Off(도어 개폐·실린더), 입력 상태 표시(도어·센서), 작업 단계별 I/O 동작 |
| 상태 | 연결, 주행 모드, 배터리, 위치, 비상정지·오류 |
| 안전 | 하트비트 누락 시 정지 요청, 도어 열림 중 출발 차단(인터록 조건은 설정) |

로봇 접속 정보(주소·인증서 경로·API Key)는 Agent의 로컬 설정 파일에 두고 git에서 제외한다. 화면에는 API Key 앞 4자리만 표시한다.

## 5. 패키지 구성 (예정)

| 경로 | 역할 |
|---|---|
| `packages/protocol` | Job·Station·RobotStatus·IoPoint 타입과 메시지 스키마 |
| `packages/robot-adapter` | `RobotAdapter` 인터페이스 + Mock Robot |
| `packages/io-adapter` | `IoAdapter` 인터페이스 + Mock I/O |
| `apps/agent` | Robot Agent (REST·WebSocket, Job Manager, Setup, Watchdog) |
| `apps/tablet-web` | Windows 태블릿 UI (터치, 가로 화면) |
| `local/` | MobED Adapter, 실제 I/O 드라이버 설정, RCS 연동 모듈 (git 제외) |

- 기술 후보: TypeScript, Node.js 22+, Fastify, React(Vite), zod, Vitest.
- **태블릿 실행 방식:** 1차는 Edge 전체화면(키오스크)으로 연다. 필요하면 WebView2 래퍼로 바탕화면 앱처럼 포장한다.
- **Agent 실행 방식:** 차상 PC 부팅 시 자동 실행(Windows 서비스 또는 시작 프로그램). 차상 PC OS는 확인 필요.

## 6. 프로토콜 초안 (태블릿 ↔ Agent)

### 데이터

- **Station:** `stationId`, `name`, `poiRef`. 좌표는 MobED POI에 두고 Agent는 참조만 한다.
- **Job:** `jobId`, `type`(`MOVE`), `targetStationId`, `state`, `ioPlan?`, `createdAt`, `updatedAt`, `error?`
- **RobotStatus:** `robotId`, `connection`, `mode`, `battery`, `pose`, `currentJobId?`, `error?`
- **IoPoint:** `ioId`, `name`, `dir`(`IN`/`OUT`), `value`

### Job 상태 머신

```
QUEUED → ASSIGNED → MOVING → ARRIVED → WAIT_CONFIRM → DONE
             │          │
             └──────────┴──▶ PAUSED ─▶ MOVING (재개)
                          └──▶ FAILED / CANCELED
```

### API

| 구분 | 방향 | 이름 |
|---|---|---|
| 작업 | T → A | `POST /jobs`, `POST /jobs/:id/cancel·pause·resume·confirm` |
| 목적지 | T → A | `GET /stations`, `POST /stations/from-current-pose`, `PATCH·DELETE /stations/:id` |
| 설정 | T → A | `GET·PUT /settings`, `POST /setup/active-map`, `POST /setup/initial-pose` |
| I/O | T → A | `GET /io`, `POST /io/:id` (출력, 설정 화면 전용) |
| 정지 | T → A | `POST /robot/stop` (전체 정지) |
| 알림 | A → T | `ws: job.updated`, `ws: robot.status`, `ws: io.changed` |
| 하트비트 | 양방향 | `ws: ping/pong` |

## 7. 작업 단위

| T | 내용 | 완료 기준 |
|---|---|---|
| T0 | 저장소 골격·규칙·기획안 | 골격 커밋 완료, 기획안 승인 대기 |
| T1 | `protocol` 타입·스키마, Job 상태 머신 | 상태 전이 단위 테스트 통과 |
| T2 | Mock Robot, Mock I/O | 이동 시간 후 도착 이벤트, I/O 값 변경 이벤트 |
| T3 | Agent: Job·Setup·I/O API + Watchdog | Mock 통합 테스트: 등록 → 작업 → 도착 → I/O → 승인 → DONE |
| T4 | 태블릿 UI: 작업 화면 | Windows 태블릿 Edge에서 Mock 시나리오 확인 |
| T5 | 태블릿 UI: 설정·목적지·I/O 화면 | Mock에서 목적지 등록·I/O 수동 조작 확인 |
| T6 | MobED Adapter, 실제 I/O 드라이버 (local) | 실기: 상태 수신, POI 조회·등록, 정지 확인 (사용자 입회) |
| T7 | 차상 PC 설치·실기 시나리오 | 스테이션 2곳 왕복 + 도어 I/O, 통신 끊김 시 정지 |
| T8 | 2단계: RCS 연동 모듈 (local) | RCS에서 작업 배정 → Agent 실행 확인 |

## 8. 확인 필요

- 차상 PC 유무와 사양·OS, MobED와의 유선 연결 방식
- I/O 모듈 종류와 통신 방식 (예: Ethernet Remote I/O, USB DIO), 점수, 결선 대상
- 현장 Wi-Fi 구성 (로봇 탑재 AP / 현장 AP), 태블릿 기종·해상도
- 목적지 등록 시 맵 제작은 기존 도구로 하고 Agent는 POI만 다루는지
- RCS ↔ Agent 프로토콜 문서 사용 범위와 RCS 쪽 MobED 지원 여부 (외부 개발사 확인)
