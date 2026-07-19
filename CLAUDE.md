# PlayGround (리뉴얼 — 신규 구축)

유소년 축구(U12~U18) **매칭 플랫폼**. 3대 축(팀 · 선수 · 에이전트) + 공개 경기기록(Records).
개발 순서: **랜딩(Phase 0) → 인증·온보딩 → Team → Player → Records 보강**.

> 기존 `C:\Workspace\PlayGround`의 **기술스택·아키텍처·컨벤션을 계승**하되,
> 코드는 가져오지 않고 **처음부터 새로 구현**하는 리뉴얼 프로젝트다.
> 기존 코드가 필요하면 참고(읽기)만 하고, 복사해 오지 않는다.

## 진행 상황 · 재개 가이드 (2026-07-15 갱신)

### 완료

- **랜딩(Phase 0)** / **인증·온보딩** — 소셜(Google·Kakao, LINE은 채널 미발급) + 이메일
  로그인(find-or-create), 역할 선택 → 팀·선수 온보딩 → 완료. JWT + `/api/auth/me`.
- **온보딩 역할 승격 시 JWT 재발급** — `UspUpdateUserRole`이 갱신된 사용자 행 반환 →
  온보딩 커맨드가 새 토큰 발급(응답 DTO `AccessToken`) → 클라이언트가
  `MarkUserAuthenticatedAsync`로 교체. 재로그인 없이 `/dashboard` 분기 정상.
- **팀 대시보드 P0** (`/dashboard/team/{section}`) — PC 6개 섹션 + 모바일(하단 탭 5개,
  경기=결과/영상 서브탭) 전부 구현.
- **팀 정보 섹션 백엔드 연동 (조회)** — SoccerTeams 확장(IsVerified·FoundedYear·MonthlyFee·
  IsMonthlyFeePublic·TrainingDays) + SoccerTeamValues/Coaches/Channels 신설(공개 홈페이지와
  데이터 공유 구조). `UspGetSoccerTeamInfoByManager` 4결과셋 1왕복(MultiQueryReader) →
  `GET api/soccer/team/me/info` → PC/모바일 섹션·사이드바·모바일 상단바 DTO 바인딩.
  빈 데이터는 뱃지·칸·카드 단위 숨김. **나머지 4개 섹션(일정·경기 결과·경기영상·선수 모집)은 아직 목데이터.**
  직접 URL 진입 페이지는 인증 상태 선확정 필수(TeamDashboardPage 참조 — 안 하면 헤더 레이스 401).
- **선수단 섹션 백엔드 연동** — `UspGetSoccerTeamRosterByManager`(단일 결과셋, 다중 `@join`:
  SoccerTeamPlayers + SoccerPlayers, 등번호 숫자순) → `GET api/soccer/team/me/roster` →
  PC·모바일 섹션 `Players` 파라미터 바인딩. 섹션 진입 시 지연 로드(OnParametersSetAsync, 1회).
  Claim은 C# 파생(UserId 연결 = Claimed, Pending은 Claim 플로우 때). 연령 탭은 전원 AgeGroup
  보유 시만 노출(전부/일부 null이면 탭 숨기고 전체 표시), Unclaimed 안내 박스는 존재 시만.
  카드 뷰는 `SoccerPlayers.PhotoUrl` 렌더(없으면 플레이스홀더 — 사진 컬럼은 94d1e08에서 추가됨).
- **공개 팀 홈페이지 1차+선수단 탭** (`/team/{slug}`, `/team/{slug}/{tab}`) — SoccerTeams에
  CoverImageUrl 신설, `UspGetSoccerTeamHomeBySlug`(5결과셋: 팀·가치·코치·채널·로스터, 비공개 제외)
  → `GET api/soccer/team/{slug}/home`(AllowAnonymous, 회비 비공개·UserId 등 관리 정보 미노출)
  → GNB(팀명 히어로 통과 시 fade-in, JS interop `team-public-home.js`)·히어로·6탭 골격·소개 탭·
  선수단 탭(공개 규칙: Claim 뱃지 비노출, Claimed만 공개 프로필 링크). **모집·진학진로·리뷰 탭은
  미구현** — 각각 스키마 필요.
- **공개 팀 홈 시즌성적 탭** (`/team/{slug}/record`) — `UspGetSoccerTeamSeasonRecordBySlug`
  (4결과셋: TeamId·최근 종료 경기 TOP 8·리그 순위·영상) → `GET api/soccer/team/{slug}/season-record?season=`
  (AllowAnonymous) → PublicRecordSection/MobilePublicRecordSection. 탭 진입 시 지연 로드.
  팀 대시보드 경기 섹션과 같은 데이터(팀 관점 변환은 Persistence, 승무패·요약은 클라)지만 **공개
  뷰라 이벤트 칩·필터 없음** — 요약 4카드(순위 없으면 숨김)+최근 경기+영상, "경기기록에서 전체 보기
  →" `/records` 링크. TeamMatchDto/TeamVideoDto·MatchResultsSection/VideosSection 헬퍼 재사용.
- **선수 대시보드 P0** (`/dashboard/player/{section}`) — PC(GNB·사이드바)+모바일(하단 탭 4),
  4섹션(프로필·커리어·시즌 통계·포트폴리오). **프로필은 백엔드 연동 완료**: SoccerPlayers 확장
  (HeightCm·WeightKg·PreferredFoot·SchoolName·GuardianPhone)과 SoccerPlayerFieldVisibilities
  (행 없으면 기본값: 키·몸무게·주발 공개 — Domain `SoccerPlayerProfileField.DefaultIsPublic`),
  SoccerPlayerFamilyLinks 신설 → `GET api/soccer/player/me/info`(연락처 서버 마스킹) 및
  `PUT .../visibility`(소유 선수만). **커리어·시즌 통계·포트폴리오는 목데이터**(PC 컴포넌트
  internal static을 모바일이 공유). SPEC의 "공개 프로필" 네이비 카드는 공개 선수 프로필
  구현 전까지 미노출 처리.
- **선수 대시보드 커리어·포트폴리오 백엔드 연동 (조회)** — SoccerPlayerCareers(TeamName 자유
  입력 + TeamId 연결, IsVerified 팀 확인 플래그)·SoccerPlayerPortfolioVideos(IsPrimary 대표,
  Tags JSON, DurationSeconds) 신설 → `GET api/soccer/player/me/career`·`me/portfolio`
  (기존 선수 액터에 핸들러 추가) → PC·모바일 4개 섹션 DTO 바인딩 + 페이지 섹션 진입 시
  지연 로드. 기간("2024.3 ~ 현재")·길이("1:42") 포맷은 클라이언트. 검증 시드
  `VerificationPlayerCareers.Seed.sql`(김정현 커리어2+영상3, 신준우 커리어1+영상0).
  **화면 검증 완료** (07-15: CSS 재빌드 반영 확인). 커리어·포트폴리오에 **빈 상태 안내** 추가
  (점선 카드 + 현재 상태 + 등록 가이드 — 주 액션은 헤더 오렌지 버튼 하나 유지).
- **초대코드 Claim 플로우** — `UspClaimSoccerPlayerInvite`(Pending·미만료 검증, 같은 계정의
  온보딩 프로필은 COALESCE 병합 후 소프트 삭제) → `POST api/soccer/player/me/claim`(실패 사유
  통합 NotFound — 코드 추측 대비, 값은 로그 미기록. **승격은 JWT 역할 General일 때만** —
  UspUpdateUserRole은 무조건 덮어쓰므로 주의). 관리자 로스터 API에 Pending 코드 포함(Unclaimed만)
  → PC 로스터 "초대코드 보내기" 클릭 시 코드 표시+클립보드 복사. 선수 대시보드는 무소속일 때
  "초대코드로 팀 연결" 카드(성공 시 승격 토큰 교체+재로드).
- **브랜드 워드마크 전환** — BrandLogo = `PlayGround`(네이비)+종목명(연회색, `Sport` 파라미터,
  이모지 박스 제거). 공개 팀 홈 GNB는 좌측 브랜드 고정 + 팀명 fade-in 구조 (핸드오프 SPEC의
  "우측 회색 링크"와 다른 확정 결정 — 획득 플라이휠 노출 목적).
- **`/dashboard` 진입 라우팅** — JWT 클레임 역할 기반 분기 (TeamAdmin→팀, Player→선수).
- **규칙 확정·전면 반영** — enum 문자열 저장, DB UTF-8 강제(NVARCHAR 금지), Client enum은
  `Models/` 분리 + `Soccer` 프리픽스, **페이지 라우트 문자열은 `Client/Routes.cs` 단일 관리**
  (@page 대신 `@attribute [Route(Routes.X)]`, 링크·NavigateTo는 상수/헬퍼만).

### 다음 작업 (우선순위)

> **순서 판단의 단일 기준: `Handoff/PLAN.DEVELOPMENTORDER.md`** (핸드오프 30종 기준 Phase A~D).
> **모든 UI 작업 전 `Handoff/Design.PatternsIndex/README.md` 필독** — 공용 패턴 15종 목차·결정표.
> 새 요소가 필요하면 새로 만들지 말고 결정표에서 기존 15종 조합을 먼저 찾는다.

1. ~~**Phase A — 횡단 기반**~~ 완료 (A1 폼 · A2 Toast·ConfirmModal · A3 스켈레톤·빈 상태 ·
   A4 내비게이션·에러 페이지 — 각 절 아래 참조).
2. **Phase B — 입력 UI** (A1·A2 위에): ~~B1 경기 결과 입력(+DatePicker)~~ 완료(아래, **친선·연습경기
   전용으로 용도 축소** — 설계 결정 7) → **B2 팀 정보 수정(+ImageUploader)** → B3 커리어·포트폴리오
   입력 → B4 선수 사진 업로드.
   - B1의 "스테이지·조 선택" 잔여 항목은 **폐기** — 대회 경기를 팀이 입력하지 않으므로 필요 없어졌다.
   - 신규: **주최측 경기 기록 입력 + 팀의 수정 신청 플로우** 설계 (대회 운영 서비스 착수와 함께,
     설계 결정 6·7 연계). 순위표 재계산(D5)의 호출 주체도 여기로 옮긴다.
3. **Phase C — 신규 화면**: 허브 → 팀 탐색 → 설정 → Claim 4스텝·알림 센터 → 공개 팀 홈 잔여 탭
   (모집·진학진로·리뷰, 탭당 스키마 신설) → 에이전트 열람 승인(최후순위).
4. **Phase D — 잔여 패턴**: 별도 단계 없이 화면 작업에 얹는다 (AvatarBadge만 Phase C 후 일괄 교체 1회).
   그 외 잔여: 온보딩 중복 방지.

### Phase A1 — 폼 공용 컴포넌트 완료 (2026-07-19, Design.FormPatterns)

- `Components/Shared/Forms/` 5종: **TextField**(단일·여러 줄·자동 포맷·글자수) · **SelectField**
  (모바일 바텀시트, 네이티브 셀렉트 금지·옵션 부가정보) · **RadioCards**(4개 이하) ·
  **CheckboxField**(법적 동의, 기본 체크 금지) · **SubmitButton**(스피너 허용 유일처).
- **검증 타이밍 3단계**는 `FormFieldContext`가 담당 — 필드가 자기를 등록(등록 순서=화면 순서),
  제출 시 전체 검증 후 **첫 오류 필드로 스크롤·포커스**(`wwwroot/js/forms.js`, 모바일은 고정 바
  오프셋 96px). 필드는 blur에 첫 검증 → 오류 중에는 입력 즉시 재검증.
- **사전 비활성 금지** — 미입력이어도 제출 버튼을 누를 수 있고, 눌러야 인라인 오류가 보인다.
  비활성은 제출 진행 중(이중 제출 잠금)에만. **폼 오류는 인라인만**(토스트 금지).
- 스타일은 `Styles/Css.Form.cs`에 모음. **주의: Tailwind `content` 글롭이 `Styles/**/*.cs`만
  스캔** — 클래스 문자열을 담은 .cs는 반드시 `Styles/`에 둘 것(Components/에 두면 클래스가
  생성되지 않아 색이 죽는다. 실제로 겪음). 토큰 신설: `danger`(#c0392b)·`navy-muted`(#c6cede).
- 데모 `/dev/form-patterns`(개발 전용)로 검증 완료: 입력 중 무지적 → blur 검증 → 수정 즉시 해제
  → 빈 제출 시 첫 오류 포커스 → 로딩("저장 중…") → 완료. 자동 포맷(2011. 03. 14 / 010-1234-5678),
  inputmode(numeric·tel), 모바일 46px·16px, 바텀시트 부가정보 전부 확인.
- **기존 화면은 아직 교체 전** — 온보딩·설정 등 실폼 적용은 A2(Feedback) 이후 함께.

### Phase A2 — 전역 피드백 완료 (2026-07-19, Design.FeedbackPatterns)

- **DI 싱글톤(Scoped) + App.razor 호스트** 방식 — `ToastService`/`ConfirmService`를 주입받아
  `Toasts.ShowSuccess(...)` / `await Confirm.ShowAsync(...)`로 호출. CascadingValue를 쓰지 않은
  이유: 호출부가 깔끔하고, 페이지가 3개 레이아웃(Main·Auth·Landing)으로 나뉘어 MainLayout 슬롯이
  전부를 못 덮는다. 호스트(`ToastHost`·`ConfirmDialogHost`)는 App.razor에 각 1개.
- **토스트** — 동시 1개 교체식(새 토스트가 이전 타이머 취소). 3.5초 / 액션 있으면 5초 /
  **오류는 자동 소멸 안 함**(수동 닫기 X 버튼 제공). PC 좌하단 24px, 모바일 하단 중앙 탭바 위.
- **확인 모달** — `ConfirmKind` 3종: Normal(네이비·동사형 레이블, 기본 포커스=주 버튼) /
  Destructive(레드·대상 이름 명시, **기본 포커스=취소**) / HighRisk(`RequiredPhrase` 일치 전까지
  주 버튼 잠금 `danger-muted`, 기본 포커스=입력칸). Enter=주 버튼, Esc·오버레이 클릭=취소.
  모바일은 전부 바텀시트(그랩바 36×4, `flex-col-reverse`로 **파괴 버튼이 위**).
- **애드혹 피드백 교체**: 초대코드 Claim = 코드 오류 → 인라인(`PlayerClaimResult.IsNetworkError`로
  구분 신설), 요청 실패 → 오류 토스트+재시도, 성공 → "{팀명}에 연결됐어요" 토스트.
  공개 범위 토글 = 결과가 화면에 즉시 보이므로 **성공 토스트 생략**, 실패만 토스트+재시도(PC·모바일).
  로스터 "복사됨" 라벨 스왑은 시각 변화가 이미 있어 그대로 둠(결정표: 중복 발송 금지).
- 스타일 `Styles/Css.Feedback.cs`, 토큰 신설 `danger-muted`(#e8b0a8) · 그림자 `toast`/`modal` ·
  애니메이션 `toast-in`. 데모 `/dev/feedback-patterns`로 **삭제 → 파괴 모달 → 실행취소 왕복** 검증
  완료(PC·모바일, 파괴 모달 기본 포커스=취소 확인).

### Phase A3 — 스켈레톤·빈 상태 완료 (2026-07-19, Design.LoadingStates·EmptyStates)

- **스켈레톤** `Components/Shared/Loading/`: `Skeleton`(프리미티브 4종 Bar·Circle·Thumb·Chip —
  **폭·높이는 인라인 style**. Tailwind JIT가 임의값을 스캔 못 하고, 폭은 고정 배열이라 랜덤 금지) +
  조합 인스턴스 `SkeletonRows`(아이콘+2줄+칩) · `SkeletonPlayerCards` · `SkeletonStatCards` ·
  `SkeletonSection`(헤더 제목+주 버튼 — **실제 섹션이 헤더를 직접 그리므로 스켈레톤에도 넣어야 점프 0**).
- **`LoadingGate`가 타이밍 3종을 전담** — 200ms 전에는 아무것도 안 그림(플리커 방지) / 3초 초과 시
  "불러오는 중이에요…" / 실패 시 그 자리를 에러 안내+"다시 시도"로 교체. `IsLoading`·`HasFailed`·
  `OnRetry`·`Skeleton`·`ChildContent` 파라미터.
- **실패 플래그를 각 페이지에 신설** — 기존 클라이언트는 오류를 삼키고 null을 반환해서 **실패와
  로딩이 구분되지 않았다**(스켈레톤 무한 회전). `mXxxFailed = 결과 is null`로 판정하고 재시도
  콜백을 연결(팀 로스터·경기·영상, 선수 커리어·통계·포트폴리오, 공개홈 시즌성적).
  시즌 통계는 pill 전환 실패 시 **기존 값을 유지**하고 첫 조회 실패만 에러로 바꾼다.
- **빈 상태** `Components/Shared/Empty/`: `EmptyState`(Tier A — 일러스트 88px/모바일 72px,
  `Art` 프리셋 Players·Video·Record·Trophy를 컴포넌트가 직접 렌더) · `EmptySlot`(Tier B — 아이콘
  40px + 한 줄, 페이지 내 한 블록만 빌 때). 스타일은 `Styles/Css.State.cs`.
- **CTA 결정(PLAN 예외 해석 적용)**: 헤더 오렌지 버튼이 이미 있는 섹션에는 **카드 안 CTA를 넣지
  않는다**(중복). 카드 안 CTA가 필요한 곳만 네이비 — 현재는 경기기록 "지난 시즌 아카이브"(아웃라인)
  하나. 방문자 뷰(공개홈·시즌통계)는 CTA 없이 안내 문장으로 종료.
- 교체: 애드혹 점선 카드 11곳(커리어·포트폴리오·경기결과·영상·시즌통계·공개홈 시즌성적, PC+모바일)
  → 공용 컴포넌트. 신규 5곳: 선수단 0명(PC·모바일 — **기존엔 빈 상태가 아예 없었다**), 경기기록
  진행중 대회 0건, 대회 상세 뉴스(Tier B, "없습니다"→"~해요"체 통일).
- 토큰 신설: `skeleton`/`skeleton-lit`/`skeleton-deep`/`skeleton-deep-lit`·`illustration`(#c5cfe4),
  `bg-shimmer`/`bg-shimmer-deep` + `animate-shimmer`(1.6s linear).
- 검증(스크래치패드 `shot-states.js`): **120ms 시점 스켈레톤 없음** → 표시 → 3초 문구 →
  섹션 top 90px가 스켈레톤·실물 동일(**점프 0**) → EmptyFC 빈 상태 3종 + 모바일.
  **헤드리스 팁**: 스크린샷 직후 `waitForFunction`의 기본 rAF 폴링이 멈춘다 — `{ polling: 300 }` 필수.
  API 지연은 스로틀 대신 CDP `Fetch.requestPaused`로 특정 경로만 붙잡는 게 결정적(스로틀은 WASM
  부팅까지 느려져 화면이 안 뜬다). **패턴 등록 시 통과용 기본 핸들러를 반드시 함께 달 것**(없으면
  모든 /api/ 요청이 영구 정지).

### Phase A4 — 내비게이션·에러 페이지 완료 (2026-07-19, Design.Navigation)

- **에러 페이지 3종** — `ErrorPageBody`(공용 본문: 일러스트 120px/모바일 96px + 코드 + 제목 + 설명 +
  버튼 + 각주)에 `Art` 프리셋 Pitch·Shield·Scoreboard. 페이지는 `NotFound`(`/not-found`) ·
  `Forbidden`(`/forbidden`) · `ServerError`(`/error`). 스타일 `Styles/Css.Error.cs`.
  **주 버튼은 네이비**(오렌지는 랜딩 CTA 전용), 기존 404의 이모지·오렌지 버튼은 제거했다.
  - **403은 로그인 상태 전용** — 게스트가 열면 `/login?returnUrl=`로 되돌린다(권한이 아니라 로그인 문제).
  - **500은 `ERR-{yyyyMMddHHmm}-{6자리}` 오류 코드**를 만들어 화면에 보여주고 로그에 예외 전체를 남긴다.
    "다시 시도"는 `?from=` 경로로 **forceLoad 새로고침**(깨진 컴포넌트 상태를 들고 가지 않는다).
- **전역 예외 경계** `GlobalErrorBoundary`(App.razor에서 Router를 감쌈) — 미처리 예외를 500으로 보낸다.
  **함정: `OnErrorAsync` 안에서 `Recover()`를 부르면 재렌더가 안 걸려 이동은 되지만 화면이 빈다.**
  `LocationChanged`에서 복구해야 정상 (실제로 겪음). 검증용 `/dev/throw`(개발 전용).
- **공용 GNB `Components/Shared/PublicGnb.razor`** — 게스트: 워드마크+경기기록+[로그인](returnUrl 보존),
  모바일 게스트는 메뉴 숨기고 [로그인]만. 로그인: 그 자리에 **벨 + 아바타 드롭다운**(내 대시보드·로그아웃).
  기존 `RecordsGnb`는 삭제하고 경기기록 3페이지가 이걸 쓴다. 랜딩 CTA는 로그인 시 "내 대시보드"로 교체.
  - **공개 팀 홈은 예외 유지** — `PublicTeamGnb`의 좌측 브랜드+팀명 fade-in 구조는 그대로 두고,
    우측 버튼만 로그인↔내 대시보드로 바꿨다(PlayGround 최소 노출 원칙, 벨·아바타 없음).
- **returnUrl 왕복** — `Routes.LoginWithReturn()` + `IsSafeReturnUrl()`(내부 상대 경로만 — `//evil.com`
  차단). `ReturnUrlStore`가 **sessionStorage**에 보관하는 이유: 소셜 로그인은 외부 도메인을 거쳐
  전체 리로드로 돌아오므로 쿼리·메모리가 살아남지 못한다. 소비 지점은 `/dashboard` **한 곳**
  (이메일·소셜 두 경로가 모두 여기로 모인다).
- **접근 가드** — 대시보드 게스트 진입 → `LoginWithReturn(현재경로)` / **역할 불일치 → `/dashboard`**
  (403이 아니라 허브 — 계정은 맞고 자리만 틀린 상황. README 인증 플로우 3).
- **라우트 이름 차이**: README는 `/auth`지만 구현은 `/login`(기존 라우트 유지). `/dashboard` 분기는
  역할 enum이 단일값이라 **0개→역할 선택 / 1개→직행** 2분기까지만 — "2개+ → 허브"는 다역할 모델이
  생길 때 추가한다.
- 검증: 404 진입 · 403 게스트 리다이렉트 · 500 코드 생성 · **강제 throw → 500 → 다시 시도 복귀** ·
  딥링크 `/dashboard/team/roster` → 로그인 → **원래 자리 복귀** · 로그인 GNB(벨+아바타, 로그인 버튼 없음) ·
  역할 불일치 리다이렉트 · 모바일 전부 확인.

### Phase B1 — 경기 결과 입력 + 날짜/시간 선택 완료 (2026-07-19, Design.DatePicker)

> **용도 축소됨 (설계 결정 7)**: 공식 대회·리그 기록은 주최측이 입력하므로, 이 폼은
> **연습경기·친선경기 전용**으로 남긴다. 아래 구현은 그대로 살아 있고 급히 걷어내지 않는다.
> **정리 필요(주최측 입력 경로 설계 시 함께)**: ① 폼의 "대회 · 리그" SelectField 제거 →
> 팀 입력은 항상 친선(TournamentId NULL) ② `UspGetSoccerTournamentOptionsByManager`·
> `GET me/tournament-options`·`GetTournamentOptionsAsync` 미사용화 ③ `UspCreateSoccerTeamMatchResult`의
> 순위표 재계산 블록은 주최측 입력 프로시저로 이관(팀 경로에선 도달 불가) ④ 토스트 문구 단일화.

- **`UspCreateSoccerTeamMatchResult`** — 경기 1행 + 우리 팀 득점 이벤트 N행(OPENJSON)을 한 트랜잭션으로
  저장하고 **이어서 `UspRecalculateSoccerTournamentStandings`를 호출한다**(D5 — 수동 재계산 경로 없음).
  득점자는 우리 팀 것만 받는다(상대 선수 명단을 알 수 없음). `UspGetSoccerTournamentOptionsByManager`는
  결과 입력 폼의 대회 선택지(우리 팀 참가 대회 우선 정렬).
- **재계산은 League일 때만 실행한다** — 순위표 스코프는 (대회+스테이지+조)인데 Cup·Split은 조를
  입력받기 전이라 특정할 수 없다. **없는 스코프를 만들어 엉뚱한 순위표를 찍느니 경기만 저장한다.**
  토스트 문구도 이에 맞춰 갈린다(리그: "순위표도 갱신됐어요" / 그 외: "저장했어요"). 조 입력은 후속.
- **Calendar** `Components/Shared/Pickers/` — PC 팝오버 300px / 모바일 바텀시트(셀 44px, 확정 버튼에
  선택 날짜 표시 = 지연 적용). 일요일 시작·요일 색(일 `weekend-sun`/토 `weekend-sat`)·오늘 teal 링·
  선택 네이비 원·경기 있는 날 teal 도트·퀵버튼(오늘·이번 주말). **네이티브 `input[type=date]` 0건.**
  - **`Range` 파라미터로 선택 범위를 나눈다**: 일정 추가=`FutureOnly`(과거 비활성, 핸드오프 원문) /
    **결과 입력=`PastOnly`(미래 비활성)** — 이미 치른 경기의 결과라 방향이 반대다.
  - 팝오버가 모달 본문(overflow-y-auto) 안에서 잘려 퀵버튼이 가려졌다 → `forms.js`의 `revealPopover`로
    열릴 때 `scrollIntoView({block:'nearest'})`.
- **TimeList** — 15분 단위 96개 + 직접 입력(유효한 형태일 때만 반영). 날짜와 **별도 필드**.
  기본값 = 그 팀의 **최근 경기와 같은 시각**(페이지가 `RecentMatchTime`으로 주입, 없으면 14:00).
- **`MatchResultFormDialog`** — PC 중앙 모달 / 모바일 전체 시트. A1 폼(TextField·SelectField·RadioCards·
  SubmitButton) + Calendar/TimeList. 득점자는 로스터 칩(같은 선수 2번 = 2골). 저장 성공 = A2 토스트,
  **검증 오류는 인라인만**(토스트 금지). 저장 후 페이지가 경기 목록을 재조회한다.
- 제너레이터 두 가지 함정: **파라미터 줄 꼬리 주석**(기본값 없는 경우)이면 그 파라미터가 누락된다 →
  주석은 프로시저 헤더로. **BIT 기본값 `= 0/1`이 `bool x = 1`로 생성돼 컴파일 에러** → 제너레이터
  `ProcedureGenerator.GetParameterDefaultValue`에서 Boolean만 true/false로 매핑하도록 수정(기존 생성물 변화 0).
- `SubmitButton`에 `Class` 파라미터 추가 — 없는 파라미터를 넘기면 렌더 예외 → A4 예외 경계가 500으로
  보내 화면이 그냥 사라진다(디버깅에 시간 걸림).
- 검증(스크래치패드 `shot-b1.js`): 빈 제출 → 인라인 3종·토스트 0 / 캘린더 31칸 중 미래 12칸 비활성·
  오늘 링·경기일 도트 / 시간 96개 / 리그 선택 저장 → 토스트 "순위표도 갱신됐어요" →
  **리그 순위 API가 null → 2로 변경**(DB 순위표에도 검증fc 2위 3점 4-1 반영) / 모바일 시트 셀 44px.
  검증 후 테스트 데이터·순위표는 원상 복구.

### 선수 시즌 통계 연동 — 완료 (2026-07-16, 선수 대시보드 4섹션 전부 실데이터)

- `UspGetSoccerPlayerSeasonStatsByUser`(4결과셋: ⓪PlayerId → ①시즌 출전 경기(Appearances+
  종료 경기+대회 형식) → ②선수 득점·도움 이벤트 → ③출전 연도 목록) →
  `GET api/soccer/player/me/season-stats?season=` → PC·모바일 섹션 바인딩.
- 팀 관점 변환·경기별 골/도움 집계(PlayerId/AssistPlayerId 매칭, 자책 제외)는 Persistence,
  요약(경기·분·득점·도움·경기당 평균 — 분 기록 경기 기준)·경기명("vs 강동 SC (3:1 승)")은
  클라이언트. 시즌 pill = 출전 연도, 전환 시 재조회(페이지 콜백). 출전 없으면 안내 카드
  (자동 집계 섹션이라 등록 버튼 없음). 화면 검증 완료(김정현 4경기 265' 골2 도움1·모바일·신준우).

### 팀 대시보드 경기 결과·영상 연동 — 완료 (2026-07-16)

- `UspGetSoccerTeamMatchesByManager`(4결과셋: ⓪TeamId → ①종료 경기+대회명·형식 → ②우리 팀
  이벤트 → ③리그 순위) → `GET api/soccer/team/me/matches?season=` / `UspGetSoccerTeamVideosByManager`
  (팀 소유+팀 경기 연결 영상) → `me/videos`. 기존 팀 읽기 액터에 핸들러 추가.
- 팀 관점 변환(IsHome·상대·아군 스코어)은 Persistence, 승무패·시즌 요약(승무패·득실)·이벤트 칩
  ("득점 김민준 ×2" — 이름 있는 이벤트 그룹핑, 자책 제외)은 클라이언트. 대회 구분 서버 파생
  (친선=대회 NULL, League, 그 외 Cup). 리그 순위 카드는 League 순위표에 팀 행 있을 때만.
- PC 경기 결과·경기영상 + 모바일 경기(결과/영상 서브탭) 바인딩, 섹션 진입 시 지연 로드
  (모바일 서브탭 공용이라 결과·영상 함께 로드). 빈 상태 점선 카드. 시드에 검증fc 친선 이벤트 추가.
  화면 검증 완료(검증fc 친선·신답 리그 순위·모바일). **새 Tailwind 클래스 추가 시 css:build 필수.**

### Records 상세 — 완료 (2026-07-16)

- `/records/{id}` — `UspGetSoccerTournamentDetail`(8결과셋: 대회·순위표·경기·수상·역대 우승
  (SeriesSlug)·영상·뉴스·**등장 팀 공개 슬러그**) → `GET api/soccer/records/tournaments/{id}`
  (AllowAnonymous) → RecordsDetailPage + RecordsStandingsTable/RecordsMatchRowCard/TeamNameLink
  /RecordsFormatting(포맷 헬퍼).
- **Format별 가변 탭**(설계 결정 5): Cup=대회 정보/예선/토너먼트/미디어, Split=…/1차 풀리그/2차
  스플릿리그/…, League=리그 경기/미디어. 진입 기본 탭 = 리그→리그 경기, 대회→예선/1차(레퍼런스 동작).
- 통계 바(총 경기·득점·경기당)는 경기 결과셋에서 클라 계산. 순위표 PC 8열/모바일 5열(승-무-패 통합),
  진출권 teal + 범례. PK 괄호 표기("2 (4)"/"(3) 2") + PK 승자 강조. 조·라운드·월 필터는 데이터 기반
  (있는 값만 칩 노출). 팀명은 공개 슬러그 있으면 팀 홈 링크. zone 문구(본선 진출 등)는 레퍼런스
  고정 카피 — 대회별 커스텀은 후속. 화면 검증 완료(cup 4탭·league·모바일).

### Records 목록+아카이브 — 완료 (2026-07-15)

- `/records`(목록)·`/records/archive`(아카이브) — `UspGetSoccerTournamentsBySeason`(3결과셋:
  대회+Champion 수상+연도 목록) → `GET api/soccer/records/tournaments?season=`(AllowAnonymous,
  Soccer_Records 읽기 액터) → RecordsPage/RecordsArchivePage + RecordsGnb/RecordsTournamentRow.
- 설계 결정 반영: 올해만+아카이브 분리 / 상태 컬러 바+진행중→예정→종료 자동 정렬 / 내 연령
  우선 배치(로그인 Player 계정의 AgeGroup — 최상단·자동 펼침·teal 테두리·"내 연령(U15)만 보기"
  opt-in 토글, 비로그인은 개인화 요소 전체 숨김) / [대회|리그] 세그먼트(대회=Cup+Split,
  리그=League + 지역 그룹 헤더). 세그먼트 전환 시 첫 연령 자동 펼침. 검색은 대회명 필터만(팀·선수는 후속).
- 공개 팀 홈 GNB "경기기록" 링크 → `/records` 실링크 연결. 화면 검증 완료(PC·모바일·개인화).

### 경기(Match) 스키마 — 완료 (2026-07-15, 설계 문서 `Docs/Architecture/MatchSchemaDesign.md`)

- 테이블 8개: SoccerTournaments(Format: Cup/Split/League, SeriesSlug=역대 우승 연결)·
  SoccerMatches(친선=TournamentId NULL, PK 스코어 컬럼)·SoccerMatchEvents(골 1행+Assist 컬럼)·
  SoccerMatchAppearances·SoccerTournamentStandings(저장식)·SoccerMatchVideos·News·Awards.
  외부 팀/선수는 Id NULL + Name 병행. 결정 #4·#5 컬럼 선반영.
- **순위표는 D5 확정안**: 경기 결과 저장 시 `UspRecalculateSoccerTournamentStandings`
  (스코프 단위, 승점→득실차→다득점→팀명) 자동 호출 — 경기 저장 유즈케이스 구현 때 잊지 말 것.
  IsQualified·0전 행은 보존(수동 보정 영역). 추후 Agent 대시보드 버튼 트리거 예정.
- 시드 `VerificationMatches.Seed.sql`(재계산 경로 포함 검증 완료 — 승점 동률 득실차 타이브레이크 확인).
- **인덱스에 필터드(WHERE) 금지** — 있으면 해당 테이블 DML에 QUOTED_IDENTIFIER ON이 강제되어
  sqlcmd(기본 OFF) 시드·운영 스크립트가 깨진다 (`Indexes/SoccerMatchDomain.Indexes.sql` 참조).

### 검증 팁 (2026-07-14 확립)

- **상세는 `Docs/Development/LocalVerification.md`** — 검증 계정·재구축 절차·화면 확인
  체크리스트·헤드리스 자동화 팁을 정리해 둠 (PC 이동 시 이 문서대로 재구축).
- 로컬 검증 계정: `verify-teamadmin-0713@test.local` / `password123!` (검증fc 팀, 팀 정보
  시드 주입됨), `verify-empty-0714@test.local` (EmptyFC — 빈 상태 확인용). 로컬 DB 전용.
  시드: `Source/Database/Soccer/Seeds/VerificationTeamInfo.Seed.sql`.
- 화면 검증: 헤드리스 Edge + playwright-core/puppeteer-core(스크래치패드에 설치), localStorage
  `pg.accessToken`에 토큰 주입 후 진입. `python`은 스토어 스텁 — 스크립트는 PowerShell
  (한글 포함 .ps1은 UTF-8 BOM 필수).
- **Edge 150부터 `puppeteer.launch()`가 "Failed to launch... Code: 0"으로 실패**(헤드리스
  시그널링 변경). 우회: `msedge.exe --headless=new --remote-debugging-port=PORT --user-data-dir=고유`로
  직접 띄우고 `puppeteer.connect({browserWSEndpoint})`로 붙는다(`/json/version`의 webSocketDebuggerUrl).
  참고 스크립트 `scratchpad/shot-connect.js`. userDataDir는 실행마다 고유(Date.now())로 — 락 충돌 방지.

### 새 PC 환경 재구축 체크리스트 (gitignore 항목 — 클론만으로 안 되는 것)

1. **로컬 DB**: SQL Server 2019+ (UTF-8 콜레이션 필요, 개발은 SQLEXPRESS 기준) 설치 후
   `Source/Database/README.md`의 셋업 명령 실행 (UTF-8 `COLLATE` 포함 생성 → Tables →
   Procedures → Seeds).
2. **시크릿**: `Source/PlayGround/PlayGround.Server/appsettings.Local.json` 을
   `appsettings.Local.json.example` 복사로 생성 후 Jwt:Key·OAuth(Google/Kakao) 입력.
   값은 팀 공유 저장소 또는 이전 프로젝트(`D:\Study\Workspace\PlayGround`)의 appsettings 참조.
3. **Tailwind**: `cd Source/PlayGround/PlayGround.Client && npm install && npm run css:build`.
4. **실행 확인**: `dotnet run --project Source/PlayGround/PlayGround.Server` →
   `https://localhost:50451` (랜딩) / `/dashboard/team` (팀 대시보드).
   SQL 프로젝트(.sqlproj)는 dotnet CLI로 빌드되지 않음 — VS로 열거나 앱 프로젝트만 빌드.

## 핵심 설계 결정 (2026-07-11 확정)

1. **축구 전용** — UI·스키마 모두 축구만. 멀티스포츠 추상화(SportId/SportConfig 등)를
   만들지 않는다. 확장은 실제 필요가 생겼을 때 진행.
2. **클린 아키텍처 유지** — Core(재사용 범용)와 PlayGround(프로젝트 전용)를 분리하고,
   아래 "프로젝트별 역할과 규칙"의 참조 방향을 절대 위반하지 않는다.
3. **최소 골격에서 출발 (YAGNI)** — 기능은 화면/유즈케이스 단위로 설계 확정 후 추가한다.
   미리 만들어 두는 코드 금지. **단, 4·5번은 "나중에 추가하는 비용이 훨씬 비싼" 확정 항목이라 예외.**
4. **에이전트 축 선반영** — 에이전트는 반드시 도입 예정. 스키마(AgentProfile, PlayerAgentLink,
   TeamRecommendation, Tournament.OrganizerId/Type, Commission, AgentReview, CompetitionStaff)는
   선반영하되, API는 `[Authorize(Roles="Agent,AgencyAdmin")]` 가드, UI는 feature flag로 숨긴다.
5. **KFA 데이터는 어댑터 경유 자체 DB화** — 직접 연동이 아니라 읽어와서 우리 DB에 적재한다.
   API를 쓰게 되더라도 `IExternalMatchProvider` 어댑터가 내부 구조에 맞춰 리턴.
   Match/Tournament에 `DataSource(Manual/AgentHosted/KfaApi)` · `ExternalRef`(멱등키) · `SyncStatus` 선반영.
6. **대회 운영은 별도 웹 서비스로 분리** — **Server는 공유, Client 프로젝트만 분리**
   (착수 시 `PlayGround.Competition.Client` 신규 추가 — 지금 만들지 않는다).
   대회 서비스는 Tournament/Match/CompetitionStaff에만 쓰기, Team/Player는 읽기 전용. 인증은 SSO 공유.
7. **공식 경기 기록의 주체는 주최측** (2026-07-19 확정) — 대회·리그 경기 결과는 주최측이 입력하고,
   **팀·선수에게는 읽기 전용**이다. 팀이 기록 오류를 발견하면 직접 고치는 게 아니라 **수정 신청**을
   올리고 주최측이 반영한다(수정 신청 플로우는 후속 설계).
   - **팀이 직접 입력하는 것은 연습경기·친선경기뿐이다.** B1에서 만든 결과 입력 폼은 이 용도로 남긴다.
   - 따라서 **팀 대시보드 입력 경로에는 대회·리그 선택이 없어야 하고**, 순위표 재계산(D5)도
     팀 입력 경로에서는 발생하지 않는다 — 재계산은 주최측 입력 경로의 책임으로 옮겨간다.
   - 현재 코드는 아직 대회 선택이 남아 있다(B1 절 "정리 필요" 참조). 기능은 살아 있으니 급히 걷어내지
     않고, 주최측 입력 경로를 설계할 때 함께 정리한다.

## 기술 스택

- **.NET 10.0** / C#
- **Blazor WebAssembly** (SPA 프론트엔드, Server가 호스팅)
- **ASP.NET Core Web API** (REST API 서버)
- **Entity Framework Core 10.x** (CRUD, 마이그레이션) + **Dapper** (SP 호출, 고성능 조회)
- **SQL Server** (주 저장소) + **Redis** (캐시)
- **ASP.NET Core Identity + JWT** (인증/인가)
- **Tailwind CSS** (유틸리티 기반 스타일링)
- **NLog** (로깅)
- **xUnit(v3), Moq, FluentAssertions** (테스트)

## 프로젝트 구조

```
PlayGroundNew/
├── PlayGround.slnx                    솔루션 (신형 XML 포맷 — .sln 아님에 주의)
├── Directory.Build.props              빌드 출력 경로 중앙 관리 (Binary/, Intermediate/)
├── Directory.Packages.props           NuGet 패키지 버전 중앙 관리 (CPM)
├── .editorconfig                      코딩 스타일 규칙
│
├── Source/
│   ├── Core/                          (재사용 가능한 범용 레이어 — PlayGround 비종속)
│   │   ├── Shared/                    → Core.Shared.csproj (네임스페이스 PlayGround.Shared)
│   │   └── Infrastructure/            → Core.Infrastructure.csproj (네임스페이스 PlayGround.Infrastructure)
│   │
│   ├── PlayGround/                    (PlayGround 프로젝트 전용 레이어)
│   │   ├── PlayGround.Contracts/      Client/Server 공유 DTO
│   │   ├── PlayGround.Domain/         엔티티, 도메인 Enum, 비즈니스 규칙
│   │   ├── PlayGround.Application/    유즈케이스 (Command), 인프라 포트
│   │   ├── PlayGround.Persistence/    DB 접근 구현 (EF Core, Dapper, Repository)
│   │   ├── PlayGround.Server/         ASP.NET Core API + Blazor 호스팅
│   │   ├── PlayGround.Client/         Blazor WebAssembly 프론트엔드
│   │   └── (예정) PlayGround.Competition.Client — 대회 운영 전용 Client (착수 시 추가, Server 공유)
│   │
│   └── Database/                      SQL 원본 (버전 관리의 단일 진실 소스)
│       ├── Account/ Database.Account.sqlproj  인증·신원 DB (SSO 공유), SDK 스타일 SQL 프로젝트(dacpac)
│       └── Soccer/  Database.Soccer.sqlproj   도메인 DB (Team, Player, Match, Agent, Content)
│           └── (각) Schema/ Tables/ Procedures/ Queries/ Indexes/ Seeds/
│
└── Tests/
    ├── Tests.Unit/                    단위 테스트 (Domain, Application, Core.Shared)
    ├── Tests.Integration/             통합 테스트 (API 엔드포인트)
    └── Tests.Infrastructure/          인프라 테스트 (DB, Redis 등 외부 의존)
```

## 프로젝트별 역할과 규칙 (반드시 준수)

### Core.Shared — 범용 유틸리티

- **역할**: 어떤 프로젝트에서도 재사용 가능한 순수 .NET 코드.
  `Result<T>` 모나드, `Envelope<T>`/`PagedData<T>` 래퍼, 확장 메서드, 범용 검증.
- **참조**: 없음 (NuGet 포함 외부 의존 최소화).
- **금지**: 도메인(축구/선수/팀) 특화 코드, 외부 라이브러리 의존.

### Core.Infrastructure — 외부 라이브러리 래핑

- **역할**: 외부 기술을 프레임워크에 맞게 래핑. DB 기반 클래스(RepositoryBase,
  CommandBase, QueryBase, ProcedureBase 등), Redis 래핑(RedisService/RedisSession),
  NLog 설정(LoggingExtensions), 텔레메트리/복원력 확장(ServiceDefaultsExtensions).
- **참조**: Core.Shared만.
- **금지**: PlayGround.* 참조 (PlayGround 비종속이어야 다른 프로젝트에서 재사용 가능).
- **Akka Actor 래핑(`Actor/`)**: Controller → Database 전달 과정의 비동기 처리에 사용.
  `AkkaService`(IHostedService)가 ActorSystem 생명주기를 관리하고, 액터 생성은 DI 리졸버
  경유(`CreateActor`/`CreateRouter`/`CreateHashRouter`) — 액터 생성자에 서비스 주입 가능.
  요청-응답은 `ActorRef.SendAsync(message, timeout)` 사용 (타임아웃 시 `ActorResultCode.Timeout`).
- **다중 결과셋 SP**: `ProcedureMultipleAsync`는 `MultiQueryReader`를 반환 —
  반드시 `using`으로 dispose (커넥션 소유권 포함).

### PlayGround.Contracts — 공유 DTO

- **역할**: Client와 Server가 함께 쓰는 요청/응답 DTO. 도메인별 폴더에
  `{Domain}Contracts.cs` 하나로 통합 (예: `Team/TeamContracts.cs`).
- **참조**: 없음.
- **금지**: 로직(메서드), 엔티티, 외부 의존. 순수 데이터 클래스만.

### PlayGround.Domain — 도메인 모델

- **역할**: 엔티티, 값 객체, 도메인 Enum(포지션·경기상태 등), 도메인 특화
  ResultCode, 순수 비즈니스 규칙.
- **참조**: Core.Shared만.
- **금지**: 외부 라이브러리(EF Core 포함) 의존, DB/HTTP 등 인프라 관심사.

### PlayGround.Application — 유즈케이스

- **역할**: API 하나 = 유즈케이스 하나. `{기능}/Commands/`, 인프라 포트 인터페이스(`Interfaces/`),
  Entity↔DTO 매핑(`Mappers/`), 입력 검증(`Validators/`).
- **참조**: Domain, Contracts, Core.Shared.
- **금지**: Persistence/Server 참조, DB 직접 접근 (반드시 포트 인터페이스 경유).
- **네이밍 (필수)**:
  - **유즈케이스는 읽기/쓰기 무관 `{기능}Command`** — 액션 동사(Get/Create) 붙이지 않는다.
    `Command`는 CQRS의 '쓰기 전용'이 아니라 **GoF Command = 실행 가능한 비즈니스 동작**의 의미.
    (예: 조회도 `SoccerLandingContentsCommand`, 생성도 `SoccerPlayerProfileCommand`.) 폴더는 `{기능}/Commands/`.
  - **기술 역량·외부 어댑터는 `{역량}Service`** (인증·JWT·해시·외부 API 등). 유즈케이스가 아니라 유즈케이스가 *의존하는* 수단.
    (예: `OAuthService`, `JwtTokenService`, `PasswordHasherService`.) 판별: "비즈니스 동작이면 Command, 갖다 쓰는 기술 수단이면 Service."
  - **축구 전용 유즈케이스는 `Soccer` 프리픽스** (종목 접두 규칙). 상세·근거는 `Docs/Architecture/NamingConventions.md`.

### PlayGround.Persistence — DB 구현

- **역할**: Application이 정의한 포트의 구현체. EF Core DbContext·마이그레이션,
  Dapper SP 호출, Repository 구현.
- **참조**: Application, Domain, Contracts, Core.Shared, Core.Infrastructure.
- **금지**: 비즈니스 규칙 (규칙은 Domain/Application에, 여기는 저장·조회만).

### PlayGround.Server — API 서버

- **역할**: ASP.NET Core 컨트롤러, 인증/인가(JWT), DI 구성, Blazor Client 호스팅.
  컨트롤러는 얇게 — 유즈케이스 호출 + `Envelope<T>` 응답 변환만.
- **참조**: 모든 레이어.
- **규칙**: URL은 `api/{role}/me/{resource}`(본인 데이터), `api/{role}/{resource}`(검색).
  응답은 항상 `Envelope<T>`.
- **종목별 분리**: Server는 여러 스포츠 종목을 함께 호스팅한다. 컨트롤러는 종목별로 분리 —
  폴더 `Controllers/{Sport}/`, 네임스페이스 `...Controllers.{Sport}`, 클래스 `{Sport}XxxController`
  (예: `Controllers/Soccer/SoccerLandingController`), 라우트 `api/{sport}/...`.
  (SportId/SportConfig 같은 추상화는 만들지 않는다 — 단순 명명·폴더 분리만.)

### PlayGround.Client — Blazor WASM 프론트엔드

- **역할**: SPA UI. Layout, Pages, 재사용 컴포넌트, API 통신 서비스, 인증 상태 관리.
- **참조**: Contracts, Domain, Core.Shared. (서버 레이어 참조 불가 — HTTP로만 통신)
- **규칙**: **하나의 시각 패턴은 한 곳에만.**
  우선순위 = 컴포넌트(.razor) > 시맨틱 클래스/상수(`Styles/Css.*.cs`) > 페이지에 raw 유틸 직접(금지).
  같은 마크업이 2번째 등장하면 즉시 컴포넌트로 추출. 새 화면 = "공용 컴포넌트에서 먼저 찾고 없으면 만든다".
- **공용 컴포넌트 (`Components/Shared/`)** — 새 화면에서 우선 재사용:
  - `PillButton` (Variant: Orange/Ghost/White/Navy × Size: Small/Medium/Large/ExtraLarge, `Class`로 배치 지정)
  - `BrandLogo` (`Href` null이면 정적, `Compact` 크기, `InheritColor` 색상 상속)
  - `CardTitle`/`CardText` (`SizeClass`로 뷰포트별 크기, `Inverted`로 어두운 배경 대응)
  - `SectionHeader` (오버라인 + H2, `BottomMarginClass`)
  - 도메인 카드는 `Components/Landing/` 등 기능 폴더에 (예: `RoleCard`).

### Source/Database — SQL 원본 (Account / Soccer 2-DB)

- **역할**: 테이블 DDL, 저장 프로시저, 인덱스의 단일 진실 소스. DB 배포는 이 파일 기준.
- **분리**: `Account`(인증·신원, SSO 공유 대비) / `Soccer`(도메인). 논리 DB는
  `DatabaseTypes` enum(Account/Soccer)과 매핑, 커넥션은 `DatabaseConfiguration` 섹션.
- **DB 간 FK·트랜잭션 불가** — `SoccerPlayers.UserId → Account.Users.Id`는 앱 계층 정합성.
  두 DB 걸치는 작업(온보딩)은 Account 먼저 → 성공 시 Soccer 순서 (분산 트랜잭션 회피).
- **종목 프리픽스** — 타 스포츠 도입 대비, **Soccer 도메인 테이블은 `Soccer` 프리픽스**
  (`SoccerPlayers`, `SoccerTeams`, `SoccerLandingContents`). 생성물도 자동으로 `Soccer{테이블}Entity`,
  프로시저 결과 Record도 `Soccer~Record`. **프로시저는 `Usp*` 유지**(네임스페이스로 종목 구분).
  **Account(공용 신원)는 프리픽스 없음**(`Users`, `SocialAccounts`).
- **규칙**: 테이블명 PascalCase 복수형, 컬럼명 PascalCase(`PlayerId`),
  프로시저 `Usp` 접두사. 스키마 변경은 반드시 SQL 파일 먼저 수정. 상세는 `Source/Database/README.md`.
- **코드 생성**: `Source/Tools/Generator.Database`가 SQL 파일을 읽어 Entity/Procedure/Query C#를
  `PlayGround.Persistence/Database/Generated/{Account,Soccer}.{Entities,Procedures,Queries}`에 생성.
  실행: `cd Source/Tools/Generator.Database && dotnet run` (경로 상대라 이 폴더에서 실행).
  생성물은 `// <auto-generated />` — 수동 편집 금지, SQL 수정 후 재생성.

### Tests.* — 테스트

- **Tests.Unit**: 외부 의존 없는 순수 단위 테스트 (Domain, Application, Core.Shared).
- **Tests.Integration**: API 엔드포인트 통합 테스트 (Server 참조).
- **Tests.Infrastructure**: 실제 DB/Redis가 필요한 테스트.

### 의존성 그래프

```
Core.Shared (의존성 없음)          PlayGround.Contracts (의존성 없음)
  ↑                                  ↑
Core.Infrastructure                PlayGround.Domain (Core.Shared 참조)
  ↑                                  ↑
  │                                PlayGround.Application (Domain, Contracts, Core.Shared)
  │                                  ↑
  └────────────── PlayGround.Persistence (Application, Domain, Contracts, Core.Shared, Core.Infrastructure)
                                     ↑
                  PlayGround.Server (모든 레이어)

PlayGround.Client (Contracts, Domain, Core.Shared) — Server와는 HTTP만
```

**새 코드를 어디에 둘지 판단 기준**: "이 코드가 PlayGround가 아닌 다른 프로젝트에서도
쓸 수 있는가?" → Yes면 Core, No면 PlayGround. "DB/외부 기술을 아는가?" →
Yes면 Infrastructure/Persistence, No면 Shared/Domain/Application.

## 데이터 흐름 패턴

- **내부 로직**: `Result<T>` 모나드로 함수형 에러 처리 (예외는 예외 상황에만)
- **API 응답**: `Envelope<T>` + 페이징은 `PagedData<T>`

## 로깅 규칙 (필수)

**로직을 작성할 때는 반드시 로그를 함께 작성한다.**

### 계층별 책임

| 계층 | 로깅 책임 |
|---|---|
| Core.Shared | 로그 없음 — Result가 곧 반환값 |
| Core.Infrastructure | **Trace/Debug 진단만** (SQL 실행시간, 재시도 등) + 생명주기 Info. 오류는 Result/Exception으로 반환하고 **Error 로그 금지** (중복 방지) |
| Application (유즈케이스) | **비즈니스 로그의 주 책임 계층** — 맥락(누가·무엇을)을 아는 곳에서 로깅 |
| Server (Controller) | 최소화 — 컨트롤러는 얇게 |

### 레벨 기준

| 레벨 | 기준 | 예 |
|---|---|---|
| **Info** | **비즈니스 이벤트 — 데이터 요청/수신/상태변경은 반드시 남긴다** | 프로필 조회 요청, 팀 생성 완료 |
| Debug | 개발 진단 | SQL 실행시간, 캐시 히트 |
| Trace | 상세 덤프 (평소 꺼둠) | 파라미터 전체 |
| Warn | 자동 복구된 이상 (NotFound 같은 정상적 빈 결과는 Warn 아님) | 재시도 후 성공, 폴백 사용 |
| Error | 요청 실패 | 유즈케이스 실패, 예외 → Result 변환 지점 |
| Fatal | 프로세스 지속 불가 | 기동 실패, 설정 누락 |

### 포맷·헬퍼 (Core.Infrastructure/Logging)

- **메시지 포맷: `문장. { Key:Value, Key:Value }`** — 헬퍼가 자동 생성 + 구조화 속성 동시 기록.
- `Logger.InfoWith("Player profile requested", ("PlayerId", id))` — Trace/Debug/Info/Warn/Error/Fatal 각 `~With` 제공.
- **실패 Result를 받은 로직은 반드시 `result.LogWith(Logger, "작업명")` 호출** — DetailCode가 레벨을
  자동 결정 (시스템 오류→Error/Fatal, 비즈니스→Warn, 입력 오류·성공→Info). 라이브러리가 Error를
  남기지 않으므로 이걸 빼먹으면 오류가 로그에 남지 않는다.
- 민감정보(패스워드·토큰·API 키) 로깅 금지. 메시지는 영어.

## UI 구현 규칙 (SPEC 기반 — 필수)

1. **UI 작성 전 해당 화면의 SPEC 문서 필독** (`Handoff/*/SPEC*.md`).
   섹션 순서·카피·컴포넌트 구성을 임의로 변경/추가/삭제하지 않는다. 카피는 한 글자도 바꾸지 않는다.
2. **디자인 토큰(tailwind.config)만 사용, 색상 하드코딩 금지.**
   토큰 정의: `PlayGround.Client/tailwind.config.js` + `Styles/app.tailwind.css`(CSS 변수).
   오렌지(`#FF6B35`)는 **CTA 전용, 전체의 5~10%만**.
3. **한글 버튼/pill은 `white-space:nowrap`**, 한글 문단은 `word-break:keep-all`(모바일 필수).
4. **빈 데이터 노출 금지** — 통계·리뷰 등 데이터가 없는 시기엔 해당 섹션 자체를 넣지 않는다.
5. 디자인 레퍼런스 HTML(`Handoff/*/*.html`)은 브라우저로 열어 시각 비교.
6. **섹션/화면 단위로 작게 구현하고 사람이 검수 후 다음 단계 진행.**
7. Tailwind 빌드: `cd Source/PlayGround/PlayGround.Client && npm run css:build` (watch는 `css:watch`).

## 빌드 & 테스트

- **빌드**: `dotnet build PlayGround.slnx`
- **테스트**: `dotnet test PlayGround.slnx`
- **실행**: `dotnet run --project Source/PlayGround/PlayGround.Server` (Client 포함 호스팅)
- 빌드 출력: `Binary/`, 중간 산출물: `Intermediate/` (git 제외)
- 패키지 추가 시: `Directory.Packages.props`에 버전 등록 → csproj에는 버전 없이 `<PackageReference Include="..." />`

---

# 코딩 컨벤션

## C# 네이밍

- **클래스, 메서드, 속성, 상수(const), static 필드**: PascalCase (`private static readonly Logger`도 포함)
- **지역 변수, 매개변수**: camelCase
- **private 인스턴스 필드**: `m` 접두사 + PascalCase — **readonly여도 m 접두사** (예: `mConnectionString`, `mHttp`, `mRepository`).
  (static/const만 위의 PascalCase. `private readonly`가 static/const 규칙과 겹칠 때는 이 규칙이 우선.)
- **인터페이스**: `I` 접두사 (예: `IPlayerRepository`)
- **비동기 메서드**: `Async` 접미사 필수 (예: `GetPlayerByIdAsync`)

## C# 포매팅

- **들여쓰기**: 공백 4칸. **중괄호**: Allman 스타일 (여는 중괄호 새 줄).
- **모든 제어문에 중괄호 필수** — `if (x) return;` 한 줄 작성 금지.
- **var**: 타입이 명확할 때(`new`, 캐스트, 리터럴)만. 기본 타입은 명시적 선언.
- **네임스페이스**: block scoped (`namespace Foo { }`).
- **LINQ 체이닝**: 메서드마다 새 줄, 첫 메서드와 동일 들여쓰기 레벨(계단식 금지).
- **패턴 매칭 선호**: `as`+null 체크 대신 패턴 매칭, `switch` 문 대신 `switch` 식.
- **using 선언문 선호**: `using var x = ...;`
- **값 정렬 금지**: `=` 열 맞춤하지 않음.
- **블록 섹션 주석**: `// ────` 장식 금지. `//.// 섹션명` 형식 (앞뒤 빈 줄).

## using 지시문 순서

그룹 순서: **System → Microsoft → 3rd Party → Core → PlayGround** (그룹 간 빈 줄 없음,
그룹 내 알파벳순, Core/PlayGround는 의존성 낮은 순).

## 방어적 코딩

- **모든 public 메서드**: 매개변수 유효성 검증 + `Debug.Assert`
- **예상 못한 상황**: `Debug.Assert(false, "설명")` 후 안전한 반환
- **null 체크 필수**: `ArgumentNullException` + `Debug.Assert` 조합

## 주석 & 로그

- **주석**: 한글, 간결하게. 이름으로 알 수 있으면 주석 생략.
- **로그/예외 메시지**: 영어 (`Logger.LogError(ex, "Failed to retrieve player")`).
- **민감정보 로깅 금지**: 패스워드, 토큰, API 키.

## Blazor 컴포넌트

- **파일명**: PascalCase (예: `KpiCard.razor`). 마크업 → `@code` 블록 순서.
- **매개변수**: `[Parameter]` public 속성, 이벤트는 `EventCallback<T>`.
- **스타일**: Tailwind 유틸리티 클래스. 다크 모드(`dark:`)와 반응형을 컴포넌트 단위로 내재화.
- **Razor 주석**: `@* 섹션 이름 *@` (장식 문자 금지).
- **함정**: `@{ var x = ...; }` 인라인 패턴은 컴파일 에러 → `@code` 블록 사용.
  한글 접미사는 `(@mData.Year)년`처럼 괄호 필수.

## 데이터베이스

- **쿼리 호출은 프로시저가 기본.** 로직에서 DB 조회는 저장 프로시저를 통해 한다.
  raw 쿼리 구문은 **테스트/일회성 확인 수준만** 허용 (`Queries/` + QueryBase).
- **enum은 정수(0,1)가 아니라 문자열로 저장한다** (2026-07-13 확정 규칙).
  컬럼은 `VARCHAR(20)` + enum 멤버 이름 그대로(`'General'`, `'Pending'`), 주석에 허용 값 명시.
  생성 엔티티는 string 프로퍼티, 읽는 쪽(Application/Client)에서 `Enum.TryParse`로 컨버팅하고
  쓸 때는 `ToString()`. **enum 멤버 이름 = DB 저장 문자열**이므로 개명은 데이터 마이그레이션과 함께.
- **DB 문자열 인코딩은 UTF-8로 강제, 다른 인코딩 금지** (2026-07-13 확정 규칙).
  DB 생성 시 `COLLATE Latin1_General_100_CI_AS_SC_UTF8` (글로벌 목표 — 한국은 로케일 중 하나).
  컬럼·파라미터는 `VARCHAR`만 사용 — `NVARCHAR`·`N''` 리터럴 금지. `VARCHAR` 크기는
  바이트라 한글 컬럼은 글자수×3 (예: `VARCHAR(300) -- UTF-8 (한글 100자)`).
  상세는 `Source/Database/README.md`.
- **엔티티·프로시저 호출 객체는 손으로 쓰지 않고 제너레이터로 생성한다.**
  1. SQL 작성: 테이블은 `Source/Database/{Account,Soccer}/Tables/`, 프로시저는 `.../Procedures/`.
     프로시저 결과 전용 슬림 엔티티는 마커로 지정 — `-- @entity: XxxRow` / `-- @source: join` /
     `-- @join: 테이블 AS 별칭 (컬럼들)`.
  2. 생성: `cd Source/Tools/Generator.Database && dotnet run`
     → `PlayGround.Persistence/Database/Generated/{DB}.{Entities,Procedures}`에 생성 (수동 편집 금지).
  3. 로직: Repository(`RepositoryBase`)에서 **생성된 프로시저 호출 객체 + 엔티티**를 사용.
     예) `var p = new UspGetLandingContents(this); var qr = await p.QueryAsync<LandingContentRecord>();`
  - **네이밍 규칙**: 테이블 전체 매핑 = `{테이블}Entity`(자동), 프로시저/쿼리 결과 투영 = `{이름}Record`
    (마커 `-- @entity:`에 `~Record`로 지정). 파일명만으로 "테이블 엔티티 vs 조회 결과"가 구분된다.
    (`~Result`는 `Result<T>` 모나드와 충돌하므로 쓰지 않는다.)
  - **생성 코드는 커밋한다** (수동 실행 단계라 clone 즉시 빌드되도록). `Generated/`는 ignore 안 함.
  4. DB 배포: 프로시저 SQL을 대상 DB에 적용 (LocalDB는 `Source/Database/README.md` 참조).
- EF Core = 마이그레이션/일부 CRUD, Dapper = 프로시저 호출(생성 객체 경유)·성능 중요 조회.

---

# Claude 작업 규칙

- **요청한 기능만 정확히 구현** — 예상 기능 선반영, 복잡한 추가 코드 생성 금지.
  추가 기능이 필요해 보이면 코드 작성 없이 제안만.
- **레이어 참조 방향 위반 금지** — 새 파일 생성 전 위 "프로젝트별 역할과 규칙"에서 위치 확인.
- **기존 코드 패턴 따르기** — 같은 일을 하는 코드가 이미 있으면 그 패턴 재사용.
- **기존 PlayGround 코드는 참고만** — 파일을 통째로 복사해 오지 않는다.
- **패키지 버전은 CPM으로만** — csproj에 Version 속성 직접 기입 금지.

## 디자인 핸드오프
- UI 작업 전 Handoff/Design.Landing.Phase0/의 README.md, SPEC.LANDING.md,
  CLAUDE.APPEND.md를 반드시 읽고 그 규칙(토큰·카피·섹션 순서 고정)을 따른다.
- 디자인 레퍼런스 HTML(*.dc.html)은 브라우저로 열어 시각 비교한다.