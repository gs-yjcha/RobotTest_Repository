# RobotTest_Repository

모바일 로봇 작업 지시 시스템을 단계적으로 시험하는 저장소다.
특정 고객 프로젝트와 무관한 **범용 구조·코드·테스트 절차**만 둔다.

## 목표

| Phase | 구성 | 내용 |
|---|---|---|
| 1 | Windows Tablet <-> Robot Agent <-> MobED x 1 | 작업 지시, 목적지 등록, 로컬 설정, I/O 제어, 상태 확인 |
| 2 | Windows Tablet <-> RCS <-> Robot Agent x N <-> MobED x N | 기존 외부 RCS로 작업 배정·다중 로봇 관리 |

```
Phase 1
[Windows Tablet: 웹 UI] --Wi-Fi HTTP/WebSocket--> [차상 PC: Robot Agent] --유선 gRPC--> [MobED]
                                                                     └--유선--> [I/O 모듈]
                                                   (개발 시) Mock Robot / Mock I/O

Phase 2
[Tablet] --> [RCS] --TCP--> [Robot Agent] x N --> [MobED] x N
[Tablet] - - 설정·점검 - - > [Robot Agent] (직접 접속 유지)
```

핵심 원칙

- **Robot Agent = 로봇 1대당 1개, 로봇 위 차상 PC에서 실행.** 1단계 gateway가 2단계에서도 그대로 Agent가 된다.
- **태블릿은 작업(Job)·목적지·설정 단위로만 말한다.** 로봇 고유 명령은 Robot Adapter, I/O 장치 명령은 IO Adapter 안에만 둔다.

## 폴더

| 경로 | 내용 |
|---|---|
| `docs/기획안/` | 기획안 (`_YYMMDD_vN`) |
| `docs/설계결정/` | 설계 결정 기록 |
| `docs/테스트/` | 시험 절차·결과 |
| `apps/`, `packages/` | 코드 (T1부터 생성) |
| `local/` | 로컬 전용 (MobED Adapter, 실제 I/O 드라이버, RCS 연동 모듈). **git 제외** |

상세 계획: [`docs/기획안/RobotTest_기획안_261115_v2.md`](docs/기획안/RobotTest_기획안_261115_v2.md)

## 공개 저장소 주의

이 저장소는 Public이다. 다음은 **커밋하지 않는다.**

- 로봇 제조사 API 문서·`.proto`·생성 코드·예제 소스 (비밀유지 대상)
- 인증서(`*.crt`, `*.key`, `*.p12`), API Key, Access Token, `.env`
- 고객사명·견적·현장 정보 등 특정 프로젝트 자료
- 외부 개발사 RCS의 소스·통신 프로토콜 문서
- 실제 로봇 IP·맵·스테이션 좌표
