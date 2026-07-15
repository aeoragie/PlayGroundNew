# 경기(Match) 스키마 설계안 — Records · 검수용

> 상태: **검수 대기** (2026-07-15 작성). 검수 확정 후 `Source/Database/Soccer/Tables/`에 DDL로 구현한다.
> 근거 문서: `Handoff/Design.Records/`(SPEC + README 설계 결정 5개), SPEC.TEAMDASHBOARD §3~5,
> SPEC.PLAYERDASHBOARD §3, CLAUDE.md 핵심 설계 결정 #4(에이전트 축)·#5(KFA 어댑터 적재).

## 1. 목적과 소비처

하나의 경기 데이터 구조를 4곳이 공유한다. 소비처별 요구 필드가 스키마의 근거다.

| 소비처 | 필요한 것 |
|---|---|
| `/records` 공개 화면 (신규) | 시즌·대회 목록(형식·범위·연령·지역 그룹·상태), 아카이브(연도·우승팀), 상세(개요·수상·역대 우승, 조별/토너먼트/스플릿/리그 순위표·경기, PK 표기, 미디어 영상·뉴스) |
| 팀 대시보드 경기 결과·영상 | 시즌 요약(8승 2무 1패·득실·리그 순위), 대회 필터(리그/컵/친선), 경기 카드(일시·구장·스코어·득점 이벤트 칩), 경기 영상 목록 |
| 선수 대시보드 시즌 통계 | 시즌 pill, 요약(출전 경기·분, 득점, 도움, 경기당 평균), 경기별 기록(날짜·경기·대회·득점·도움·출전분) — "팀 경기 결과에서 자동 집계" |
| 공개 팀 홈 시즌성적 탭 | 팀 기준 시즌 요약 + 경기 결과 (미구현 — 이 스키마 확정 후) |

## 2. 설계 원칙 (기존 규칙 승계)

- Soccer 프리픽스, GUID PK + `NEWID()`, `CreatedAt/UpdatedAt/DeletedAt`(소프트 삭제), enum은 VARCHAR 문자열(멤버 이름 그대로), UTF-8 VARCHAR(한글 글자수×3), FK 없이 앱 계층 참조.
- **결정 #5 선반영**: 외부 적재 대상 테이블(대회·경기·순위표)에 `DataSource VARCHAR(20)`('User','KfaApi','Seed') + `ExternalId VARCHAR(64)`(멱등키) + `SyncStatus VARCHAR(20) NULL`('Synced','Pending','Failed' — 외부 소스일 때만).
- **결정 #4 선반영**: 대회에 `OrganizerUserId`·`OrganizerType`('Platform','Agent','External') — API·UI는 숨김.
- **외부 팀·선수 허용**: KFA 적재·과거 기록은 플랫폼 미가입 팀/선수가 대부분 → 모든 참조는
  `~Id UNIQUEIDENTIFIER NULL` + `~Name VARCHAR` 병행 (커리어의 TeamName 자유 입력과 동일 패턴).
  Id가 있으면 팀 홈/선수 프로필 링크, 없으면 텍스트.

## 3. 테이블 설계 (7개)

### 3.1 SoccerTournaments — 대회/리그 (통합)

[대회|리그] 세그먼트는 별도 테이블이 아니라 `Format`으로 구분. 매년 새 행(SeasonYear별).

| 컬럼 | 타입 | 비고 |
|---|---|---|
| TournamentId | UNIQUEIDENTIFIER PK | |
| SeasonYear | INT NOT NULL | 시즌 = 연도. 별도 시즌 테이블 없음(§5-D1). 아카이브 연도 칩 = DISTINCT |
| Name | VARCHAR(300) NOT NULL | |
| SeriesSlug | VARCHAR(100) NULL | 같은 대회의 연도별 행을 묶는 키 — "역대 우승팀" 조회용 |
| Format | VARCHAR(20) NOT NULL | 'Cup'(조별+토너먼트) \| 'Split'(풀리그+스플릿) \| 'League' — 상세 탭 구성 결정 |
| Scope | VARCHAR(20) NOT NULL | 'National'(전국) \| 'Regional'(지방) — 목록 뱃지 |
| AgeGroup | VARCHAR(20) NOT NULL | 'U12','U15','U18' — 연령 아코디언 |
| RegionGroup | VARCHAR(60) NULL | 리그의 지역 그룹 헤더('서울','인천'…) — League만 사용 |
| Status | VARCHAR(20) NOT NULL | 'Scheduled'(예정) \| 'InProgress'(진행중) \| 'Completed'(종료) — 목록 컬러 바·정렬 |
| StartDate / EndDate | DATE NULL | 개요 "기간" |
| TeamCount | INT NULL | 참가팀 수 — 외부 적재 시 참가팀 전체가 없을 수 있어 저장(§5-D2) |
| HostName | VARCHAR(300) NULL | 주최 |
| MethodText / MatchTimeText / VenueText / TiebreakText | VARCHAR(600) NULL | 개요 카드 key-value (방식·경기시간·구장·순위 결정 — 자유 텍스트) |
| RegulationPdfUrl | VARCHAR(2048) NULL | 규정 PDF |
| SourceName / SourceUrl | VARCHAR(300)/VARCHAR(2048) NULL | 출처 표기 (완료 기준 체크리스트) |
| OrganizerUserId / OrganizerType | UNIQUEIDENTIFIER NULL / VARCHAR(20) NULL | 결정 #4 선반영 ('Platform','Agent','External') |
| DataSource / ExternalId / SyncStatus | | 결정 #5 |
| CreatedAt / UpdatedAt / DeletedAt | | |

### 3.2 SoccerMatches — 경기

친선 경기는 `TournamentId NULL` (팀 대시보드 리그/컵/친선 필터의 '친선').

| 컬럼 | 타입 | 비고 |
|---|---|---|
| MatchId | UNIQUEIDENTIFIER PK | |
| TournamentId | UNIQUEIDENTIFIER NULL | NULL = 친선(대회 무관 팀 자체 경기) |
| StageType | VARCHAR(20) NULL | 'Group'(예선 조별) \| 'Split1' \| 'Split2' \| 'Knockout' \| 'League' — 상세 탭 배치. 친선은 NULL |
| GroupName | VARCHAR(30) NULL | 조별 스테이지의 조 ('1조'…'14조'). 조 선택 칩·조 순위표 키 |
| RoundName | VARCHAR(30) NULL | 조별 'R1'~'R3'(라운드 필터), 토너먼트 'PO','R16','QF','SF','F'(라운드 칩) |
| HomeTeamId / HomeTeamName | UNIQUEIDENTIFIER NULL / VARCHAR(300) NOT NULL | Id 있으면 팀 홈 링크 |
| AwayTeamId / AwayTeamName | 〃 | |
| HomeScore / AwayScore | INT NULL | NULL = 미종료 |
| HomePkScore / AwayPkScore | INT NULL | 승부차기 — "1 (4)" 괄호 표기. 있을 때만 |
| Status | VARCHAR(20) NOT NULL | 'Scheduled' \| 'Completed' \| 'Canceled' (경기 단위 '진행중' 실시간 운영은 없음 — §5-D3) |
| MatchedAt | DATETIME2 NULL | 일시 (시간 미정 대비 NULL 허용) |
| VenueName | VARCHAR(300) NULL | 구장 |
| DataSource / ExternalId / SyncStatus | | 결정 #5 |
| CreatedAt / UpdatedAt / DeletedAt | | |

파생 규칙: 팀 대시보드 대회 필터 = TournamentId NULL → 친선, Format='League' → 리그, 그 외(Cup/Split) → 컵.
승/무/패는 스코어에서 파생(저장하지 않음).

### 3.3 SoccerMatchEvents — 득점 이벤트 (선수 통계 원천 ①)

**한 골 = 한 행**, 도움은 같은 행의 Assist 컬럼(§5-D4). 선수 득점 = PlayerId 카운트, 도움 = AssistPlayerId 카운트.

| 컬럼 | 타입 | 비고 |
|---|---|---|
| EventId | UNIQUEIDENTIFIER PK | |
| MatchId | UNIQUEIDENTIFIER NOT NULL | |
| TeamId / TeamName | UNIQUEIDENTIFIER NULL / VARCHAR(300) NOT NULL | 득점 팀 |
| EventType | VARCHAR(20) NOT NULL | 'Goal' \| 'OwnGoal' \| 'PenaltyGoal' (자책골은 상대 득점 처리·개인 득점 미집계) |
| PlayerId / PlayerName | UNIQUEIDENTIFIER NULL / VARCHAR(150) NULL | 득점자 (외부 선수는 이름만, 미상은 둘 다 NULL) |
| AssistPlayerId / AssistPlayerName | 〃 | 도움 (없으면 NULL) |
| MinuteOfPlay | INT NULL | 표시용 ('23'') |
| CreatedAt / UpdatedAt / DeletedAt | | |

### 3.4 SoccerMatchAppearances — 출전 기록 (선수 통계 원천 ②)

시즌 통계의 "12경기 824분 · 경기당 68분"의 원천. 경기 결과 입력 시 출전 선수·분을 함께 기록.

| 컬럼 | 타입 | 비고 |
|---|---|---|
| AppearanceId | UNIQUEIDENTIFIER PK | |
| MatchId / TeamId / PlayerId | UNIQUEIDENTIFIER NOT NULL | 플랫폼 선수만 (외부 선수 출전 기록은 수집하지 않음) |
| MinutesPlayed | INT NULL | NULL = 분 미상(경기 수만 집계) |
| IsStarter | BIT NOT NULL DEFAULT 0 | 선발 여부 (표시는 후속) |
| CreatedAt / UpdatedAt / DeletedAt | | |

### 3.5 SoccerTournamentStandings — 순위표 (저장 방식, §5-D5)

조 순위표·리그 순위표·스플릿 순위표 공용. 키 = (TournamentId, StageType, GroupName).

| 컬럼 | 타입 | 비고 |
|---|---|---|
| StandingId | UNIQUEIDENTIFIER PK | |
| TournamentId | UNIQUEIDENTIFIER NOT NULL | |
| StageType / GroupName | VARCHAR(20) NOT NULL / VARCHAR(30) NULL | 'Group'+'1조', 'League'+NULL, 'Split1'… |
| TeamId / TeamName | UNIQUEIDENTIFIER NULL / VARCHAR(300) NOT NULL | |
| TeamRank | INT NOT NULL | (`Rank`는 T-SQL 예약어 회피) |
| Played / Won / Drawn / Lost | INT NOT NULL DEFAULT 0 | |
| Points / GoalsFor / GoalsAgainst | INT NOT NULL DEFAULT 0 | 득실차는 파생 |
| IsQualified | BIT NOT NULL DEFAULT 0 | 진출권 teal 행 (조별 진출·리그 왕중왕전 공용) |
| DataSource / ExternalId / SyncStatus | | 결정 #5 — 경기 없이 순위표만 적재되는 경우 대비 |
| CreatedAt / UpdatedAt / DeletedAt | | |

### 3.6 SoccerMatchVideos — 경기 영상 (미디어 탭 + 팀 대시보드 경기영상 공용)

선수 포트폴리오 영상(`SoccerPlayerPortfolioVideos` — 선수 소유)과 별개 유지(§5-D6).

| 컬럼 | 타입 | 비고 |
|---|---|---|
| VideoId | UNIQUEIDENTIFIER PK | |
| TournamentId | UNIQUEIDENTIFIER NULL | Records 미디어 탭 기준 |
| MatchId | UNIQUEIDENTIFIER NULL | 경기 연결 시 VS 배너(홈/원정 팀명) 구성 |
| TeamId | UNIQUEIDENTIFIER NULL | 팀 대시보드 경기영상 섹션 기준 (팀 자체 훈련 영상 등 대회 무관 영상 허용) |
| Title | VARCHAR(300) NOT NULL | |
| VideoUrl / ThumbnailUrl | VARCHAR(2048) NOT NULL / NULL | |
| VideoType | VARCHAR(20) NOT NULL | 'Highlight' \| 'FullMatch' \| 'Training' (기존 Client enum과 일치) |
| DurationSeconds | INT NULL | 표시 "1:42"는 클라이언트 |
| RecordedOn | DATE NULL | |
| CreatedAt / UpdatedAt / DeletedAt | | |

### 3.7 SoccerTournamentNews / SoccerTournamentAwards — 뉴스·수상

- **SoccerTournamentNews**: NewsId PK, TournamentId, Title VARCHAR(300), Url VARCHAR(2048),
  PublisherName VARCHAR(150) NULL, PublishedOn DATE NULL, 타임스탬프. (미디어 탭 뉴스 서브탭)
- **SoccerTournamentAwards**: AwardId PK, TournamentId, AwardType VARCHAR(20)
  ('Champion','RunnerUp','FairPlay'), TeamId NULL / TeamName NOT NULL, DisplayOrder, 타임스탬프.
  역대 우승팀 = 같은 `SeriesSlug`의 과거 연도 Champion들. 아카이브 "우승팀" 컬럼도 여기서.

## 4. 집계 전략 — 실시간 집계 (A안 채택 제안)

집계 테이블 없이 프로시저에서 SUM/COUNT (§5-D7):

- **선수 시즌 통계**: `UspGetSoccerPlayerSeasonStats(@UserId, @SeasonYear)` — Appearances(경기·분)
  + Events(득점 = PlayerId, 도움 = AssistPlayerId) + 경기별 기록(경기당 1행 병합). 시즌 pill 목록은 출전 연도 DISTINCT.
- **팀 시즌 요약**: 팀의 Completed 경기에서 승/무/패·득실 SUM, 리그 순위는 Standings 조회.
- **대회 통계 바**: 총 경기·종료 COUNT, 득점 SUM·경기당 평균.

근거: 유소년 데이터 규모(팀당 시즌 수십 경기)에서 인덱스만으로 충분, 정합성 자동.
느려지면 그때 집계 테이블 추가 (YAGNI). 인덱스 선반영: Matches(TournamentId), Matches(HomeTeamId)/(AwayTeamId),
Events(MatchId), Events(PlayerId)/(AssistPlayerId), Appearances(PlayerId), Standings(TournamentId).

## 5. 결정 포인트 요약 (검수 대상)

| # | 결정 | 추천 | 대안 |
|---|---|---|---|
| D1 | 시즌 테이블 | **연도 컬럼(SeasonYear)만** — 시즌은 실체가 연도뿐 | SoccerSeasons 테이블 (기간·이름 필요해지면 추가) |
| D2 | 참가팀 목록 테이블 | **이번엔 없음** — TeamCount 저장 + 순위표·경기가 사실상 참가팀 | SoccerTournamentTeams (참가 신청 기능 때 추가) |
| D3 | 경기 상태 | **Scheduled/Completed/Canceled** — '진행중'은 대회 레벨만 | Match에 InProgress 추가 (실시간 중계 없으므로 불요) |
| D4 | 득점/도움 구조 | **골 1행 + Assist 컬럼** — 골·도움 1:1, 집계 단순 | 이벤트 행 분리(Goal/Assist 별도) — 정합성 관리 비용 ↑ |
| D5 | 순위표 | **저장** — 외부 적재(경기 없이 순위만)·순위 결정 규칙(승자승 등) 재현 불가 대응. 자체 입력 경기는 저장 시 Application이 재계산·갱신 | 경기에서 실시간 계산 (외부 적재·수동 보정 불가) |
| D6 | 영상 테이블 | **SoccerMatchVideos 신설** — 소유·수명주기가 선수 포트폴리오와 다름 | PortfolioVideos 확장 (혼합 시 공개 규칙 복잡) |
| D7 | 시즌 통계 집계 | **실시간 SUM** (프로시저) | 집계 테이블 + 트리거/배치 (규모상 불요) |
| D8 | 팀 '일정' 섹션의 훈련 등 비경기 일정 | **이번 스코프 제외** — 경기 일정은 Match(Scheduled)로 커버, 훈련·회비일정은 후속 SoccerTeamScheduleEvents | 지금 포함 (Records와 무관해 스코프 팽창) |

## 6. 구현 순서 제안 (검수 후)

1. **스키마 + 검증 시드** — 테이블 7개 DDL → 제너레이터 → 로컬 DB. 시드: cup/split/league 3형식 대회
   + 검증fc·리그 팀 경기/이벤트/출전/순위/미디어 (화면 검증용 현실 데이터).
2. **`/records` 목록 + 아카이브** (공개, AllowAnonymous) — 조회 프로시저·API·화면.
3. **`/records` 상세** — 형식별 가변 탭 (대회 정보 → 조별 → 토너먼트 → 리그 → 미디어).
4. **팀 대시보드 경기 결과·영상 연동** (목데이터 교체) + 공개 홈 시즌성적 탭.
5. **선수 시즌 통계 연동** — 자동 집계 프로시저. (선수 대시보드 완결)

각 단계는 화면 단위 검수 후 진행 (UI 규칙 6).
