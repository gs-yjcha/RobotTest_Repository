# tablet-wpf — Windows 태블릿 앱 (WPF)

기획안: [`docs/기획안/RobotTest_기획안_261117_v3.md`](../../docs/기획안/RobotTest_기획안_261117_v3.md) · 화면 설계: `UI 시안/` v10 (2026-11-18 승인)

| 프로젝트 | 역할 |
|---|---|
| `Tablet.App` | WPF 화면 (MVVM, CommunityToolkit.Mvvm) |
| `Tablet.Core` | 모델, `IRobotClient`, Job 상태 머신 |
| `Tablet.Mock` | Mock Robot — 실기 없이 화면 시험 |
| `Tablet.Tests` | xUnit 단위 시험 |
| `../../local/Mobed.Client` | MobED 직접 연결 구현. **git 제외** (있을 때만 빌드에 포함) |

## 빌드·실행

```
dotnet build RobotTest.Tablet.sln
dotnet test RobotTest.Tablet.sln
dotnet run --project Tablet.App -- --windowed   # 개발용 창 모드
dotnet run --project Tablet.App                 # 전체화면 (F11로 전환)
```

- 요구: .NET 10 SDK, Windows.
- `local/Mobed.Client/Mobed.Client.csproj`가 있으면 `MOBED_DIRECT`로 빌드되어 실기에 연결한다. 없으면 Mock으로 실행된다. 실기 빌드에서도 `--mock`을 주면 Mock으로 실행한다.
- `Mobed.Client`는 `Tablet.Core`를 참조하고 `Mobed.Client.MobedClientFactory.Create()`로 `IRobotClient`를 돌려준다.

## 터치

- 창 전체에 `Stylus.IsPressAndHoldEnabled="False"` — 길게 누르기가 우클릭으로 바뀌지 않게 한다.
- 누르는 동안 이동 버튼은 `TouchUp`·`LostTouchCapture`·창 `Deactivated`에서 반드시 정지한다 (T7).

## T2 Job 상태 시험

- `IRobotClient.Navigation`이 제공되면 `IRobotNavigation`으로 목적지 이동·일시정지·재개·취소를 요청하고, 단일 소비자가 Job ID별 주행 상태를 순서대로 받는다.
- `JobStateMachine`은 로봇 도착 뒤 작업자 확인까지 받아야 작업을 완료한다. 해당 Job의 실패·취소는 종료 상태이며, 다른 Job의 수신 상태는 무시한다.
- Mock은 시험에서 성공·실패를 직접 결정한다. 정지 요청에는 진행 중인 Mock 주행의 취소 상태를 발행한다.
- 상태 시험은 `dotnet test Tablet.Tests/Tablet.Tests.csproj`로 실행한다. 운행 화면에 주행 명령을 연결하는 작업은 T5다.

## T3 연결 설정

- 설정 화면에서 장비의 접속 주소와 포트를 입력한다. 주소는 `host:port` 또는 `https://host:port` 형식이다.
- 인증서 폴더를 선택하거나 경로를 입력하고, 폴더에서 찾은 `.crt`·`.pem`·`.cer` 중 사용할 파일을 고른다. 만료된 파일은 목록에 표시한다.
- `저장 후 연결 시험`은 연결 정보와 인증서 위치를 `%LOCALAPPDATA%\RobotTest\settings.json`에 저장한다. API Key는 Windows DPAPI(`CurrentUser`)로 암호화하고, 화면에서 일부만 표시한다. 키 입력란을 비워 둔 채 저장하면 등록된 키를 유지한다.
- Mock 시험은 주소 형식, 로컬 인증서 유효 기간, 키 등록 상태를 확인한 뒤 모의 연결 결과를 표시한다. 실패 단계에서 멈추고 뒤 단계는 건너뛴다. 실제 장비의 TLS·인증·상태 확인은 T4에서 수행한다.

## T4 MobED 직접 연결 시험

- 로컬 어댑터가 포함된 빌드에서 설정 화면의 `저장 후 연결 시험`을 눌러 접속한다. 인증서 및 접속 주소가 맞는지 확인하고, 실제 시험은 사용자가 장비를 확인하는 상태에서 진행한다.
- TCP 연결, TLS/HTTP·2, 인증, 서버 API 판본, 주행 모드 스트림, 활성 맵·위치 상태를 순서대로 확인한다. 주행 모드와 위치 상태는 시험 당시 응답을 표시한다. 연결 중 상태 스트림이 종료되면 상단 연결 상태가 오류로 바뀐다.
- 이 단계의 MobED 직접 연결은 상태 조회 기능을 제공한다. 전체 정지·운행 명령 연결은 T5에서 검증한다.
- `local/Mobed.Client`와 제조사 API 정의는 로컬 전용이다. 로컬 어댑터 없이 빌드하거나 `--mock` 인자를 주면 Mock으로 실행한다.

## v10 화면 구성

- 운행: 예시 지도, 목적지 선택, 주행 설정, 작업 지시·위치 보정·수동 주행 탭.
- 맵 편집: 목적지·영역·맵 이미지 탭. 알람·이력: 수신 대기 표시와 실행 중 앱 이벤트.
- 설정: MobED 직접 연결 정보·인증서 폴더/파일·API Key·연결 시험 6단계 배치.
- 목적지·주행 모드·속도 선택은 화면 상태만 바꾼다. 주행·보정·맵 저장은 미연결로 비활성화했다.
- 위치 일치율·배터리·알람은 미수신으로 표시한다. Mock 연결·정지 요청은 실제 로봇 상태 확인을 뜻하지 않는다.
- 검증 실행은 `dotnet run --project Tablet.App -- --windowed --mock`을 사용한다.
