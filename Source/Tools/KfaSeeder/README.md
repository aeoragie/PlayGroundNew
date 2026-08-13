# KfaSeeder — KFA 크롤링 데이터를 샘플 시드로

KFA 크롤러 산출물(JSON 5종)을 Records·팀·선수 화면용 샘플 시드 SQL로 변환한다.
생성물은 `Source/Database/Soccer/Seeds/Kfa/`에 떨어지며 **커밋하지 않는다**(gitignore).

## 실행

```powershell
cd Source/Tools/KfaSeeder
.\GenerateKfaSeed.ps1 -InputDir 'D:\Study\Workspace\PlayGroundOld2\Backup\Others\Crawler'

# 적용 (00→07 순서 — 00이 KfaApi 소스 데이터를 지우므로 재실행 안전)
foreach ($f in Get-ChildItem ..\..\Database\Soccer\Seeds\Kfa\0*.sql) {
    sqlcmd -S .\SQLEXPRESS -d PlayGround_Soccer -b -f 65001 -i $f.FullName
}

# 쇼케이스 로스터 사진을 개발 S3 버킷에 업로드 (aws configure 필요)
.\UploadPhotos.ps1
```

## 무엇이 만들어지나

| 파일 | 내용 |
|---|---|
| 00_CleanKfa | `DataSource='KfaApi'` 행 전량 삭제 (재실행 정리) |
| 01_Teams | KFA 팀 전체 (비공개 프로필 — 팀 탐색에 노출 안 됨) |
| 02_Players | 로스터 보유 팀의 선수 + 팀 소속 (등번호·포지션·키·몸무게·사진) |
| 03_Tournaments | 대회·리그 152개 (Format·지역·상태·기간) |
| 04_Matches | 경기 전량 (상세 보유 경기는 전후반·주심·감독·순번 포함) |
| 05_MatchDetails | 득점·카드 이벤트 + 홈/원정 라인업 (출전시간·주장) |
| 06_Standings | 리그·조별 순위표 (완료 경기에서 계산) |
| 07_PlaygroundFc | 검증fc → **플레이그라운드FC** 개명 + 쇼케이스 팀(울산HDFCU12) 데이터 연결 |

## 연결 규칙

- 모든 GUID는 md5(외부키)로 결정적 — 재생성해도 같은 값이라 파일 간 참조·재실행이 안전하다.
- 경기·이벤트·라인업의 선수 연결은 (KFA 팀, 등번호) 우선, (KFA 팀, 이름) 보조.
- 쇼케이스 팀은 행을 만들지 않고 자리 GUID만 쓴다. 07이 검증fc의 실제 TeamId로 치환하고
  이름은 생성 시점에 이미 플레이그라운드FC로 바뀌어 있다.
- 사진·엠블럼은 KFA 파일 서버(`files.joinkfa.com`) 외부 URL을 그대로 쓰고,
  쇼케이스 로스터 20명만 개발 S3 버킷(`/uploads/player-photo/kfa/…`)으로 올린다.

## 주의

- 로컬 개발 DB 전용이다. 운영에 넣지 않는다.
- 07은 검증fc가 이미 있어야 한다(LocalVerification의 팀 온보딩 선행).
- 시각은 KST 벽시계를 UTC(-9h)로 바꿔 저장한다. 상태 판정 기준일은 `-Today`로 고정 가능.
