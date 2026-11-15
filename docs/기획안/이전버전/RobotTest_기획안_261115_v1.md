# RobotTest 기획안 — Tablet 작업 지시 시스템

- 버전: v1 · 2026-11-15
- 상태: **검토 초안** (AI 제안 구성. 사용자 승인 전에는 확정 사항이 아니다)
- 범위: 범용 모바일 로봇 작업 지시 시험. 특정 고객 프로젝트 사양을 담지 않는다.

## 1. 목표

- **최종:** Tablet <-> RCS <-> MobED x N. 여러 대 로봇에 작업을 나눠 주고 상태를 한 화면에서 본다.
- **1단계 (2027-03 전시 시연 목표):** Tablet <-> MobED x 1. 태블릿에서 작업을 지시하고 로봇 상태·작업 진행을 실시간으로 본다.

## 2. 설계 원칙

1. **태블릿은 Job 단위로만 말한다.** "A 스테이션으로 이동", "완료 승인" 같은 작업 언어를 쓰고 로봇 고유 API를 모른다.
2. **로봇 고유 부분은 Robot Adapter에 가둔다.** Adapter 인터페이스를 하나 정하고 Mock·MobED 구현을 바꿔 끼운다.
3. **1단계 Gateway = 2단계 RCS의 축소판.** Job 큐·상태 머신을 Gateway 안에 두되, 로봇 1대만 배정한다. 2단계에서 RCS와 Robot Agent로 나눈다.
4. **안전 기본값.** 통신 끊김·하트비트 누락 시 현재 작업을 일시정지하고 로봇 정지를 요청한다.

## 3. 시스템 구성

### 1단계

```
[Tablet Browser: apps/tablet-web]
      │ HTTP (REST) + WebSocket (상태 push)
      ▼
[apps/gateway (Node.js)]
  ├─ Job Manager (큐·상태 머신)
  ├─ Robot Registry (로봇 1대)
  └─ Robot Adapter ──▶ [Mock Robot]  (개발·시연 리허설)
                   └─▶ [MobED Adapter] ─gRPC─▶ [MobED]  (local/ 설정, 저장소 외부 SDK 참조)
```

- Gateway는 로봇 옆 노트북 또는 차상 PC에서 실행한다 (실행 위치는 확인 필요).
- 태블릿과 Gateway는 같은 Wi-Fi에 있다고 가정한다.

### 2단계 (참고)

```
[Tablet] ─▶ [RCS: Job 큐 · 로봇 선택 · 교통 관리] ─▶ [Robot Agent] x N ─▶ [MobED] x N
```

- Tablet ↔ RCS 프로토콜은 1단계 Tablet ↔ Gateway 프로토콜을 그대로 쓴다.
- RCS ↔ Agent 프로토콜은 2단계 착수 시 결정한다.

## 4. 패키지 구성 (예정)

| 경로 | 역할 |
|---|---|
| `packages/protocol` | Job·RobotStatus 타입, 메시지 스키마 (브라우저·서버 공용) |
| `packages/robot-adapter` | `RobotAdapter` 인터페이스 + Mock Robot |
| `apps/gateway` | REST·WebSocket 서버, Job Manager |
| `apps/tablet-web` | 태블릿 웹 UI (가로 화면, 큰 버튼) |
| `local/` | MobED Adapter 연결 설정 (git 제외) |

기술 후보: TypeScript, Node.js 22+, Fastify, React(Vite), zod, Vitest. 사내 MobED SDK 실습과 같은 스택이라 재사용이 쉽다.

## 5. 프로토콜 초안

### 데이터

- **Station:** `stationId`, `name` — 목적지. 실제 좌표·맵 정보는 로봇 쪽 설정에 둔다.
- **Job:** `jobId`, `type`(`MOVE`), `targetStationId`, `state`, `robotId`, `createdAt`, `updatedAt`, `error?`
- **RobotStatus:** `robotId`, `connection`(`ONLINE`/`OFFLINE`), `mode`, `battery`, `currentJobId?`, `error?`

### Job 상태 머신

```
QUEUED → ASSIGNED → MOVING → ARRIVED → WAIT_CONFIRM → DONE
             │          │
             └──────────┴──▶ PAUSED ─▶ MOVING (재개)
                          └──▶ FAILED / CANCELED
```

### 메시지

| 방향 | 이름 | 내용 |
|---|---|---|
| Tablet → GW | `POST /jobs` | 작업 생성 (목적지) |
| Tablet → GW | `POST /jobs/:id/cancel` | 작업 취소 |
| Tablet → GW | `POST /jobs/:id/confirm` | 도착 후 완료 승인 |
| Tablet → GW | `POST /robots/:id/stop` | 전체 정지 |
| GW → Tablet | `ws: job.updated` | Job 상태 변경 |
| GW → Tablet | `ws: robot.status` | 로봇 상태 주기 전송 |
| 양방향 | `ws: ping/pong` | 하트비트 |

### RobotAdapter 인터페이스

```ts
interface RobotAdapter {
  connect(): Promise<void>;
  moveTo(stationId: string): Promise<void>;
  pause(): Promise<void>;
  resume(): Promise<void>;
  stop(): Promise<void>;
  onStatus(cb: (s: RobotStatus) => void): Unsubscribe;
  onArrived(cb: (stationId: string) => void): Unsubscribe;
}
```

MobED 구현은 사내 MobED SDK를 로컬 경로로 참조한다. 저장소에는 인터페이스와 Mock만 둔다.

## 6. 작업 단위

| T | 내용 | 완료 기준 |
|---|---|---|
| T0 | 저장소 골격·규칙·기획안 | 이 문서 승인, 첫 커밋 |
| T1 | `protocol` 타입·스키마, Job 상태 머신 | 상태 전이 단위 테스트 통과 |
| T2 | `robot-adapter` 인터페이스 + Mock Robot | Mock이 이동 시간 후 도착 이벤트 발생 |
| T3 | `gateway` REST·WebSocket + Job Manager | Mock 대상 통합 테스트: 생성→도착→승인→DONE |
| T4 | `tablet-web` 화면 (목적지 선택·진행·승인·정지) | 태블릿 브라우저에서 Mock 시나리오 수동 확인 |
| T5 | MobED Adapter (local) 연결 | 실기: 상태 수신, 정지 명령 확인 (사용자 입회) |
| T6 | 실기 시나리오 시험 | 스테이션 2곳 왕복, 통신 끊김 시 정지 확인 |
| T7 | 2단계 설계: RCS·Agent 분리, 다중 로봇 Mock | 별도 기획안 |

## 7. 확인 필요

- 태블릿 기종·해상도, 브라우저 (Android Chrome 가정)
- Gateway 실행 위치 (노트북 / 차상 PC)와 네트워크 구성
- MobED 쪽 목적지(Goal) 등록 방식과 시연용 스테이션 수
- 1단계 작업 종류가 "이동 + 완료 승인"으로 충분한지 (도어·I/O 제어 포함 여부)
- 2단계 RCS를 자체 개발할지, 외부 RCS에 붙일지
