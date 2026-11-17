# RobotTest_Repository

MobED 기반 플랫폼별 개발 저장소다. 플랫폼마다 `platforms/` 아래 폴더 하나를 쓰고, 공통 코드는 `common/`에 둔다.
특정 고객 프로젝트와 무관한 **범용 구조·코드·테스트 절차**만 둔다.

## 플랫폼

| 플랫폼 | 폴더 | 상태 |
|---|---|---|
| 물품 이송 | [`platforms/transport`](https://github.com/gs-yjcha/RobotTest_Repository/tree/main/platforms/transport) | 기획안 v3 (WPF 태블릿 → MobED 직접 연결), T1 진행 |
| 드론 스테이션 | [`platforms/drone-station`](https://github.com/gs-yjcha/RobotTest_Repository/tree/main/platforms/drone-station) | 골격만 |
| MOMA (MobED + 협동로봇) | [`platforms/moma`](https://github.com/gs-yjcha/RobotTest_Repository/tree/main/platforms/moma) | 골격만 |
| 감시정찰 | [`platforms/surveillance`](https://github.com/gs-yjcha/RobotTest_Repository/tree/main/platforms/surveillance) | 골격만 |
| 공통 | [`common`](https://github.com/gs-yjcha/RobotTest_Repository/tree/main/common) | 골격만 |

## 폴더 규칙

```
RobotTest_Repository/
├─ common/                 플랫폼 공통 코드·문서
├─ platforms/<플랫폼>/
│  ├─ README.md            목적·구성·진행 상태
│  ├─ docs/                기획안·설계 결정·테스트
│  ├─ src/ (또는 apps/, packages/)
│  └─ local/               로컬 전용, git 제외
└─ README.md               플랫폼 목록
```

- 폴더 이름은 영문 일반명(kebab-case)으로 짓는다. 고객사명·현장명을 넣지 않는다.
- 새 플랫폼은 `platforms/`에 폴더를 만들고 위 표에 한 줄을 추가한다.

## 공개 저장소 주의

이 저장소는 Public이다. 다음은 **커밋하지 않는다.**

- 로봇 제조사 API 문서·`.proto`·생성 코드·예제 소스 (비밀유지 대상)
- 인증서(`*.crt`, `*.key`, `*.p12`), API Key, Access Token, `.env`
- 고객사명·견적·현장 정보 등 특정 프로젝트 자료
- 외부 개발사 RCS의 소스·통신 프로토콜 문서
- 실제 로봇 IP·맵·스테이션 좌표
