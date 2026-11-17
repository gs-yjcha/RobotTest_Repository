# transport — 물품 이송 플랫폼

MobED 기반 물품 이송 플랫폼. 모바일 로봇 작업 지시 시스템을 단계적으로 시험한다.
저장소 공통 규칙은 [루트 README](../../README.md)를 따른다.

## 목표

| Phase | 구성 | 내용 |
|---|---|---|
| 1 (연구) | Windows Tablet (WPF) -> MobED x 1 직접 연결 | 연결·인증, 상태 확인, 목적지 이동, 정지, 위치 보정, 로컬 설정 |
| 이후 | Windows Tablet <-> Robot Agent (차상 PC) <-> MobED | I/O 제어, 로컬 안전 감시 |
| 2 | Windows Tablet <-> RCS <-> Robot Agent x N <-> MobED x N | 기존 외부 RCS로 작업 배정·다중 로봇 관리 |

```
Phase 1
[Windows Tablet: WPF App] --Wi-Fi gRPC (TLS)--> [MobED]
          (개발 시) Mock Robot

이후
[Tablet] --> [차상 PC: Robot Agent] --유선 gRPC--> [MobED]
                                    └--유선--> [I/O 모듈]
Phase 2
[Tablet] --> [RCS] --TCP--> [Robot Agent] x N --> [MobED] x N
```

핵심 원칙

- **화면은 `IRobotClient`로만 로봇과 말한다.** 1단계는 MobED 직접 구현, 이후 Agent 구현으로 바꿔 끼운다.
- **MobED 고유 코드(gRPC·proto)는 `local/Mobed.Client`에만 둔다.** 공개 저장소에 넣지 않는다.

## 폴더

| 경로 | 내용 |
|---|---|
| `docs/기획안/` | 기획안 (`_YYMMDD_vN`, 이전 판은 `이전버전/`) |
| `docs/설계결정/` | 설계 결정 기록 |
| `docs/테스트/` | 시험 절차·결과 |
| `apps/tablet-wpf/` | WPF 태블릿 앱 (T1부터 생성) |
| `apps/tablet-web/` | 기존 React 시안 코드 (보류·참고용) |
| `UI 시안/` | 태블릿 UI 시안 HTML (화면 설계 기준) |
| `local/` | 로컬 전용 (`Mobed.Client`, 실제 연결 설정). **git 제외** (`.gitignore`의 `local/`) |

상세 계획: [`docs/기획안/RobotTest_기획안_261117_v3.md`](docs/기획안/RobotTest_기획안_261117_v3.md)
