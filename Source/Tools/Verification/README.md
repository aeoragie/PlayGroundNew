# 검증 스크립트 (Verification)

각 단계에서 실제로 돌려 본 화면·API 검증 스크립트다. **앱 빌드와 무관**하고 솔루션에도 포함되지 않는다 —
회귀 테스트 스위트가 아니라 **"그때 무엇을 어떻게 확인했는지"의 기록**이자 다음 검증의 출발점이다.

CLAUDE.md의 각 Phase 절이 여기 파일들을 이름으로 참조한다.

## 준비

```powershell
cd Source/Tools/Verification
npm install                 # puppeteer-core만 받는다
dotnet run --project ../../PlayGround/PlayGround.Server --urls http://localhost:5000
```

스크립트는 `http://localhost:5000`을 가정한다. 스크린샷은 실행한 디렉터리에 저장된다.

## 파일

| 파일 | 확인하는 것 |
|---|---|
| `shot-connect.js` | **CDP 연결 참고 구현** — 나머지 스크립트가 전부 이 패턴을 쓴다 |
| `shot-states.js` | A3 스켈레톤 타이밍(200ms/3초)·빈 상태·로드 후 점프 0 |
| `shot-b1.js` | B1 경기 결과 입력 — 인라인 오류·캘린더·시간 리스트 |
| `shot-b2.js` | B2 엠블럼 교체→공개홈 반영 · 12MB 실패 · 세로 사진 EXIF |
| `shot-b3.js` | B3 커리어·포트폴리오 — 추가→수정→삭제(모달)→실행취소 |
| `api-b3.js` | B3 API 왕복 — 기간 역전 거부·유튜브 링크 정규화·대표 승계 |
| `shot-b4.js` | B4 선수 사진 — 권한별 카메라 뱃지 노출 |
| `perm-b4.js` | B4 권한 4종(보호자/팀 관리자/제3자 2종) + 외부 URL 거부 |
| `shot-b5.js` | B5 친선 행 마킹 · 세그먼트 URL 동기화 · 요약 공식만 |
| `sql-b5.sql` | B5 집계 경계 — 친선을 리그 스코프에 넣어도 순위표 무변화(양방향) |

`sql-b5.sql`만 sqlcmd로 돌린다:

```powershell
sqlcmd -S .\SQLEXPRESS -d PlayGround_Soccer -E -b -f 65001 -i sql-b5.sql -W
```

## 검증 계정 (로컬 전용)

| 계정 | 용도 |
|---|---|
| `verify-teamadmin-0713@test.local` | 검증fc 팀 관리자 (팀 정보·경기·로스터) |
| `verify-empty-0714@test.local` | EmptyFC — 빈 상태 확인용 |
| `verify-player-u12@test.local` | 신준우 (커리어 1건·영상 0건 — 빈 상태부터 시작) |
| `verify-player-u15@test.local` | 김정현 (커리어 2건·영상 3건, 광주광주FCU15 소속) |
| `verify-u15-1@test.local` | 광주광주FCU15 관리자 (선수 사진 권한 검증용) |

비밀번호는 전부 `password123!`. 자세한 시드·재구축 절차는 `Docs/Development/LocalVerification.md`.

## 헤드리스 함정 (여러 번 걸린 것들)

- **Edge 150부터 `puppeteer.launch()`가 실패한다.** `msedge.exe --headless=new --remote-debugging-port=PORT`로
  직접 띄우고 `puppeteer.connect()`로 붙는다 — `shot-connect.js` 참고. `--user-data-dir`은 실행마다 고유하게.
- **A1 폼 필드는 레이블과 입력이 형제가 아니다.** `label[for]` ↔ `document.getElementById()`로 찾을 것.
- **PC·모바일 트리가 둘 다 렌더된다**(한쪽은 CSS로 숨김). `querySelector`는 안 보이는 쪽을 집는다 —
  `getBoundingClientRect().width > 0`으로 보이는 요소만 골라야 한다.
- **스크린샷 직후 `waitForFunction`의 기본 rAF 폴링이 멈춘다** → `{ polling: 300 }` 필수.
- API 지연을 만들 때 전체 스로틀은 WASM 부팅까지 느려져 화면이 안 뜬다 → CDP `Fetch.requestPaused`로
  특정 경로만 붙잡되, **통과용 기본 핸들러를 반드시 함께 등록**할 것(없으면 모든 요청이 영구 정지).

## 원칙

검증에 쓴 데이터는 **끝나고 시드 상태로 되돌린다.** 스크립트가 스스로 정리하거나(‑b3·b5),
정리 SQL을 따로 돌린다. 검증 흔적이 다음 검증의 전제를 흔들면 안 된다.
