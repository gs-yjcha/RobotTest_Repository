# RobotTest_Repository

모바일 로봇 작업 지시 시스템을 단계적으로 시험하는 저장소다.
특정 고객 프로젝트와 무관한 **범용 구조·코드·테스트 절차**만 둔다.

## 목표

| Phase | 구성 | 내용 |
|---|---|---|
| 1 | Tablet <-> Gateway <-> MobED x 1 | 태블릿에서 작업 지시, 로봇 상태 실시간 확인 |
| 2 | Tablet <-> RCS <-> MobED x N | 작업 큐·로봇 배정·다중 로봇 상태 관리 |

```
Phase 1
[Tablet Browser] --HTTP/WebSocket--> [Gateway (Node.js)] --Robot Adapter--> [MobED]
                                                          └─ (개발 시) Mock Robot

Phase 2
[Tablet Browser] --HTTP/WebSocket--> [RCS] --Agent 프로토콜--> [Robot Agent] x N --> [MobED] x N
```

핵심 원칙: **태블릿은 "작업(Job)" 단위로만 말한다.** 로봇 고유 명령은 Adapter 안에만 둔다.
그래야 Phase 2에서 Gateway 자리에 RCS를 넣어도 태블릿 화면과 프로토콜을 바꾸지 않는다.

## 폴더

| 경로 | 내용 |
|---|---|
| `docs/기획안/` | 기획안 (`_YYMMDD_vN`) |
| `docs/설계결정/` | 설계 결정 기록 |
| `docs/테스트/` | 시험 절차·결과 |
| `apps/`, `packages/` | 코드 (T1부터 생성) |
| `local/` | 로컬 전용 (MobED 연동 설정·자료 참조). **git 제외** |

상세 계획: [`docs/기획안/RobotTest_기획안_261115_v1.md`](docs/기획안/RobotTest_기획안_261115_v1.md)

## 공개 저장소 주의

이 저장소는 Public이다. 다음은 **커밋하지 않는다.**

- 로봇 제조사 API 문서·`.proto`·생성 코드·예제 소스 (비밀유지 대상)
- 인증서(`*.crt`, `*.key`, `*.p12`), API Key, Access Token, `.env`
- 고객사명·견적·현장 정보 등 특정 프로젝트 자료
- 실제 로봇 IP·맵·스테이션 좌표
