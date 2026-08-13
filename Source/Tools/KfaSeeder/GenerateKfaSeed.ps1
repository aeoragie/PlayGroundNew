# KFA 크롤링 JSON을 샘플 시드 SQL로 변환한다.
#
#   .\GenerateKfaSeed.ps1 -InputDir <크롤링폴더> [-OutDir <출력폴더>] [-Today yyyy-MM-dd]
#
# 입력: Teams/Players/Matches/Match_Results/Match_Details_2026.json (KFA 크롤러 산출물)
# 출력: Source/Database/Soccer/Seeds/Kfa/*.sql (생성물이라 커밋하지 않는다. 재실행 안전)
#       + photo-manifest.json (쇼케이스 로스터 사진 S3 업로드 목록 — UploadPhotos.ps1이 소비)
#
# 설계 근거.
# - DataSource='KfaApi' + ExternalId(멱등키)는 스키마가 선반영한 자리다(설계 결정 5).
# - 경기·이벤트·라인업의 팀/선수 연결은 GUID를 md5(외부키)로 결정적으로 만들어 잇는다.
#   재생성해도 같은 GUID가 나오므로 파일 간 참조와 재실행이 안전하다.
# - 쇼케이스 팀(울산HDFCU12)은 행을 만들지 않고 자리 GUID만 쓴다. 07이 검증fc(플레이그라운드FC로
#   개명)의 실제 TeamId로 치환하고, 이름은 생성 시점에 이미 플레이그라운드FC로 바꿔 둔다.
# - 시각은 KST 벽시계를 UTC(-9h)로 바꿔 저장한다(시각 기준은 UTC 하나).

param(
    [Parameter(Mandatory = $true)][string]$InputDir,
    [string]$OutDir = (Join-Path $PSScriptRoot '..\..\Database\Soccer\Seeds\Kfa'),
    [string]$Today = '2026-08-12'
)

$ErrorActionPreference = 'Stop'
$ShowcaseKfaName = '울산HDFCU12'
$ShowcaseNewName = '플레이그라운드FC'
$KfaFiles = 'https://files.joinkfa.com'

#.// JSON 로드 — ConvertFrom-Json(5.1)은 대용량 제한이 있어 JavaScriptSerializer를 직접 쓴다

Add-Type -AssemblyName System.Web.Extensions
$serializer = New-Object System.Web.Script.Serialization.JavaScriptSerializer
$serializer.MaxJsonLength = [int]::MaxValue

function Load-Json([string]$file) {
    $text = [IO.File]::ReadAllText((Join-Path $InputDir $file), [Text.Encoding]::UTF8)
    return $serializer.DeserializeObject($text.TrimStart([char]0xFEFF))
}

Write-Host 'JSON 로드 중...'
$teams = Load-Json 'Teams_2026.json'
$players = Load-Json 'Players_2026.json'
$tournaments = Load-Json 'Matches_2026.json'
$results = Load-Json 'Match_Results_2026.json'
$details = Load-Json 'Match_Details_2026.json'
Write-Host ("teams {0} / players {1} / tournaments {2} / results {3} / details {4}" -f $teams.Count, $players.Count, $tournaments.Count, $results.Count, $details.Count)

#.// 결정적 GUID — md5(종류:외부키). 같은 입력이면 언제나 같은 GUID

$md5 = [System.Security.Cryptography.MD5]::Create()
function Get-KfaGuid([string]$kind, [string]$key) {
    $hash = [BitConverter]::ToString($md5.ComputeHash([Text.Encoding]::UTF8.GetBytes("kfa:${kind}:${key}"))).Replace('-', '')
    return '{0}-{1}-{2}-{3}-{4}' -f $hash.Substring(0, 8), $hash.Substring(8, 4), $hash.Substring(12, 4), $hash.Substring(16, 4), $hash.Substring(20, 12)
}

function SqlLit($value) {
    if ($null -eq $value -or '' -eq $value) { return 'NULL' }
    return "'" + ([string]$value).Replace("'", "''") + "'"
}

function SqlNum($value) {
    $parsed = 0
    if ($null -ne $value -and [int]::TryParse(([string]$value).Trim(), [ref]$parsed)) { return [string]$parsed }
    return 'NULL'
}

function AgeOf([string]$mgc) {
    if ($mgc.StartsWith('초등')) { return 'U12' }
    if ($mgc.StartsWith('중등')) { return 'U15' }
    return 'U18'
}

function TournamentStatus([string]$start, [string]$end) {
    if ($end -and $end -lt $Today) { return 'Completed' }
    if ($start -and $start -gt $Today) { return 'Scheduled' }
    return 'InProgress'
}

# KST 벽시계 → UTC datetime2 리터럴 ('yyyy-MM-ddTHH:mm:ss')
function KstToUtc([string]$dateStr, [string]$timeStr) {
    if (-not $dateStr) { return $null }
    $time = '00:00'
    if ($timeStr -and $timeStr.Trim() -match '^\d{1,2}:\d{2}$') { $time = $timeStr.Trim() }
    $wall = [DateTime]::ParseExact("$dateStr $time", 'yyyy-MM-dd H:mm', [Globalization.CultureInfo]::InvariantCulture)
    return $wall.AddHours(-9).ToString('s')
}

#.// 팀 해석 — (대회, 팀명) 우선. 같은 팀명이 대회마다 다른 TeamId를 가질 수 있다(24건)

$teamByTournamentName = @{}
$teamByName = @{}
foreach ($t in $teams) {
    $teamByTournamentName[$t['MatchIdx'] + '|' + $t['TeamName']] = $t
    if ($teamByName.ContainsKey($t['TeamName'])) {
        if ($null -ne $teamByName[$t['TeamName']] -and $teamByName[$t['TeamName']]['TeamId'] -ne $t['TeamId']) {
            $teamByName[$t['TeamName']] = $null   # 모호 — 대회 문맥으로만 해석
        }
    }
    else {
        $teamByName[$t['TeamName']] = $t
    }
}

function Resolve-Team([string]$matchIdx, [string]$name) {
    $key = $matchIdx + '|' + $name
    if ($teamByTournamentName.ContainsKey($key)) { return $teamByTournamentName[$key] }
    if ($teamByName.ContainsKey($name)) { return $teamByName[$name] }
    return $null
}

$showcaseIds = @{}
foreach ($t in $teams) { if ($t['TeamName'] -eq $ShowcaseKfaName) { $showcaseIds[$t['TeamId']] = $true } }

function TeamGuidOf($kfaTeam) { return Get-KfaGuid 'team' $kfaTeam['TeamId'] }
function DisplayName([string]$name) { if ($name -eq $ShowcaseKfaName) { return $ShowcaseNewName } return $name }

function EmblemUrl([string]$emblem) {
    if (-not $emblem -or $emblem -eq '|') { return $null }
    $parts = $emblem.Split('|')
    if ($parts.Count -lt 2 -or -not $parts[0] -or -not $parts[1]) { return $null }
    return "$KfaFiles/$($parts[0])/S/$($parts[1])"
}

#.// 선수 연결 — (KFA TeamId, 등번호) 우선, (KFA TeamId, 이름) 보조 (동명이인이면 포기)

$playerGuidByTeamEntry = @{}
$playerGuidByTeamName = @{}
foreach ($p in $players) {
    $guid = Get-KfaGuid 'player' ($p['TeamId'] + ':' + $p['EntryNo'] + ':' + $p['Name'])
    $playerGuidByTeamEntry[$p['TeamId'] + '|' + $p['EntryNo']] = $guid
    $nameKey = $p['TeamId'] + '|' + $p['Name']
    if ($playerGuidByTeamName.ContainsKey($nameKey)) { $playerGuidByTeamName[$nameKey] = $null }
    else { $playerGuidByTeamName[$nameKey] = $guid }
}

function Resolve-Player([string]$kfaTeamId, [string]$entryNo, [string]$name) {
    if (-not $kfaTeamId) { return $null }
    $entryKey = $kfaTeamId + '|' + $entryNo
    if ($entryNo -and $playerGuidByTeamEntry.ContainsKey($entryKey)) { return $playerGuidByTeamEntry[$entryKey] }
    $nameKey = $kfaTeamId + '|' + $name
    if ($playerGuidByTeamName.ContainsKey($nameKey)) { return $playerGuidByTeamName[$nameKey] }
    return $null
}

#.// SQL 파일 쓰기 — 다중 VALUES 배치 + GO. UTF-8(BOM)로 저장해야 sqlcmd -f 65001과 맞물린다

New-Item -ItemType Directory -Force $OutDir | Out-Null
$utf8Bom = New-Object System.Text.UTF8Encoding($true)

function Add-BatchInsert([Collections.Generic.List[string]]$lines, [string]$header, [Collections.Generic.List[string]]$rows, [int]$batch = 500) {
    for ($i = 0; $i -lt $rows.Count; $i += $batch) {
        $lines.Add($header)
        $lines.Add('VALUES')
        $upper = [Math]::Min($i + $batch, $rows.Count) - 1
        $chunk = $rows.GetRange($i, $upper - $i + 1)
        $lines.Add(($chunk -join ",`n") + ';')
        $lines.Add('GO')
    }
}

function Write-Seed([string]$name, [Collections.Generic.List[string]]$lines) {
    [IO.File]::WriteAllText((Join-Path $OutDir $name), ($lines -join "`r`n") + "`r`n", $utf8Bom)
    Write-Host "  $name"
}

function New-Lines { return New-Object 'Collections.Generic.List[string]' }
function New-Rows { return New-Object 'Collections.Generic.List[string]' }

#.// 00 — 재실행 정리 (KfaApi 소스만 지운다)

$lines = New-Lines
$lines.Add('SET NOCOUNT ON;')
$lines.Add("DELETE FROM [dbo].[SoccerMatchEvents] WHERE [MatchId] IN (SELECT [MatchId] FROM [dbo].[SoccerMatches] WHERE [DataSource] = 'KfaApi');")
$lines.Add("DELETE FROM [dbo].[SoccerMatchAppearances] WHERE [MatchId] IN (SELECT [MatchId] FROM [dbo].[SoccerMatches] WHERE [DataSource] = 'KfaApi');")
$lines.Add("DELETE FROM [dbo].[SoccerMatches] WHERE [DataSource] = 'KfaApi';")
$lines.Add("DELETE FROM [dbo].[SoccerTournamentStandings] WHERE [DataSource] = 'KfaApi';")
$lines.Add("DELETE FROM [dbo].[SoccerTournaments] WHERE [DataSource] = 'KfaApi';")
$lines.Add("DELETE FROM [dbo].[SoccerTeamPlayers] WHERE [PlayerId] IN (SELECT [PlayerId] FROM [dbo].[SoccerPlayers] WHERE [DataSource] = 'KfaApi');")
$lines.Add("DELETE FROM [dbo].[SoccerPlayers] WHERE [DataSource] = 'KfaApi';")
$lines.Add("DELETE FROM [dbo].[SoccerTeams] WHERE [DataSource] = 'KfaApi';")
$lines.Add('GO')
Write-Seed '00_CleanKfa.sql' $lines

#.// 01 — 팀 (쇼케이스 제외 — 그 자리는 07이 실제 TeamId로 치환)

$rows = New-Rows
foreach ($t in $teams) {
    if ($showcaseIds.ContainsKey($t['TeamId'])) { continue }
    if (-not $t['TeamId'] -or -not $t['TeamName']) { continue }   # 크롤러 빈 행 방어
    $rows.Add('(' + (SqlLit (TeamGuidOf $t)) + ',' + (SqlLit $t['TeamName']) + ',' + (SqlLit (AgeOf $t['MgcNm'])) + ',' + (SqlLit (EmblemUrl $t['Emblem'])) + ",0,'KfaApi'," + (SqlLit $t['TeamId']) + ')')
}
$lines = New-Lines
$lines.Add('SET NOCOUNT ON;')
Add-BatchInsert $lines 'INSERT INTO [dbo].[SoccerTeams] ([TeamId],[TeamName],[AgeGroup],[LogoUrl],[IsPublicProfile],[DataSource],[ExternalId])' $rows
Write-Seed '01_Teams.sql' $lines

#.// 02 — 선수 + 팀 소속 (로스터가 있는 팀만). 쇼케이스 로스터 사진은 S3 키로 치환

$photoManifest = New-Object 'Collections.Generic.List[object]'
$playerRows = New-Rows
$rosterRows = New-Rows
foreach ($p in $players) {
    $guid = Get-KfaGuid 'player' ($p['TeamId'] + ':' + $p['EntryNo'] + ':' + $p['Name'])
    $teamGuid = Get-KfaGuid 'team' $p['TeamId']
    $slugHash = [BitConverter]::ToString($md5.ComputeHash([Text.Encoding]::UTF8.GetBytes($guid))).Replace('-', '').Substring(0, 6).ToLower()
    $slug = $p['Name'] + '-kfa-' + $slugHash

    $photo = $null
    if ($p['PhotoPath'] -and $p['Photo']) { $photo = "$KfaFiles/$($p['PhotoPath'])/S/$($p['Photo'])" }
    if ($showcaseIds.ContainsKey($p['TeamId']) -and $photo) {
        $keyHash = [BitConverter]::ToString($md5.ComputeHash([Text.Encoding]::UTF8.GetBytes($guid + ':photo'))).Replace('-', '').ToLower()
        $key = "uploads/player-photo/kfa/$keyHash.jpg"
        $photoManifest.Add(@{ source = $photo; key = $key })
        $photo = '/' + $key
    }

    $status = "'Active'"
    if ($p['IsEnded'] -eq $true) { $status = "'Inactive'" }
    $playerRows.Add('(' + (SqlLit $guid) + ',' + (SqlLit $p['Name']) + ',' + (SqlLit $slug) + ',' + (SqlLit $photo) + ',' + (SqlLit (AgeOf $p['MgcNm'])) + ',' + (SqlNum $p['Height']) + ',' + (SqlNum $p['Weight']) + ',' + (SqlLit $teamGuid) + ",'KfaApi'," + (SqlLit ($p['TeamId'] + ':' + $p['EntryNo'])) + ')')
    $rosterRows.Add('(' + (SqlLit (Get-KfaGuid 'teamplayer' ($p['TeamId'] + ':' + $p['EntryNo'] + ':' + $p['Name']))) + ',' + (SqlLit $teamGuid) + ',' + (SqlLit $guid) + ',' + (SqlLit $p['EntryNo']) + ',' + (SqlLit $p['Position']) + ',' + (SqlLit (AgeOf $p['MgcNm'])) + ',' + $status + ')')
}
$lines = New-Lines
$lines.Add('SET NOCOUNT ON;')
Add-BatchInsert $lines 'INSERT INTO [dbo].[SoccerPlayers] ([PlayerId],[Name],[Slug],[PhotoUrl],[AgeGroup],[HeightCm],[WeightKg],[TeamId],[DataSource],[ExternalId])' $playerRows
Add-BatchInsert $lines 'INSERT INTO [dbo].[SoccerTeamPlayers] ([TeamPlayerId],[TeamId],[PlayerId],[JerseyNumber],[Position],[Grade],[Status])' $rosterRows
Write-Seed '02_Players.sql' $lines

#.// 03 — 대회

$resultsByTournament = @{}
foreach ($r in $results) {
    if (-not $resultsByTournament.ContainsKey($r['MatchIdx'])) {
        $resultsByTournament[$r['MatchIdx']] = New-Object 'Collections.Generic.List[object]'
    }
    $resultsByTournament[$r['MatchIdx']].Add($r)
}

$rows = New-Rows
foreach ($t in $tournaments) {
    $isLeague = $t['StyleNm'] -eq '리그'
    $region = $null
    if ($isLeague -and $t['Title'] -match '\[([^\]]+)\]\s*$') { $region = $Matches[1] }
    $scope = 'Regional'
    if ($isLeague -or $t['Title'].Contains('전국')) { $scope = 'National' }
    $format = 'Cup'
    if ($isLeague) { $format = 'League' }

    $teamCount = 'NULL'
    if ($resultsByTournament.ContainsKey($t['Idx'])) {
        $names = @{}
        foreach ($r in $resultsByTournament[$t['Idx']]) { $names[$r['HomeTeam']] = 1; $names[$r['AwayTeam']] = 1 }
        $teamCount = [string]$names.Count
    }

    $rows.Add('(' + (SqlLit (Get-KfaGuid 'tournament' $t['Idx'])) + ',2026,' + (SqlLit $t['Title']) + ',' + (SqlLit $format) + ',' + (SqlLit $scope) + ',' + (SqlLit (AgeOf $t['MgcNm'])) + ',' + (SqlLit $region) + ',' + (SqlLit (TournamentStatus $t['StartDate'] $t['EndDate'])) + ',' + (SqlLit $t['StartDate']) + ',' + (SqlLit $t['EndDate']) + ',' + $teamCount + ',' + (SqlLit $t['PlayingArea']) + ",'대한축구협회(KFA)','https://www.joinkfa.com','KfaApi'," + (SqlLit $t['Idx']) + ",'Synced')")
}
$lines = New-Lines
$lines.Add('SET NOCOUNT ON;')
Add-BatchInsert $lines 'INSERT INTO [dbo].[SoccerTournaments] ([TournamentId],[SeasonYear],[Name],[Format],[Scope],[AgeGroup],[RegionGroup],[Status],[StartDate],[EndDate],[TeamCount],[VenueText],[SourceName],[SourceUrl],[DataSource],[ExternalId],[SyncStatus])' $rows
Write-Seed '03_Tournaments.sql' $lines

#.// 04 — 경기 (results 전량 + details 병합)

$detailBySingle = @{}
foreach ($d in $details) { $detailBySingle[$d['SingleIdx']] = $d }
$tournamentById = @{}
foreach ($t in $tournaments) { $tournamentById[$t['Idx']] = $t }

function StageOf($tournament, [string]$matchGroup) {
    if ($tournament['StyleNm'] -eq '리그') { return @('League', $null) }
    if ($matchGroup) { return @('Group', ($matchGroup + '조')) }
    return @('Knockout', $null)
}

$rows = New-Rows
foreach ($r in $results) {
    $t = $tournamentById[$r['MatchIdx']]
    $d = $null
    if ($detailBySingle.ContainsKey($r['SingleIdx'])) { $d = $detailBySingle[$r['SingleIdx']] }
    $stage, $group = StageOf $t $r['MatchGroup']

    $homeKfa = Resolve-Team $r['MatchIdx'] $r['HomeTeam']
    $awayKfa = Resolve-Team $r['MatchIdx'] $r['AwayTeam']
    $homeTeamId = $null
    if ($homeKfa) { $homeTeamId = TeamGuidOf $homeKfa }
    $awayTeamId = $null
    if ($awayKfa) { $awayTeamId = TeamGuidOf $awayKfa }

    $homeScore = $r['HomeScore']; $awayScore = $r['AwayScore']
    $homePk = $r['HomePkScore']; $awayPk = $r['AwayPkScore']
    $firstHalfHome = $null; $firstHalfAway = $null; $referee = $null; $coachHome = $null; $coachAway = $null
    if ($d) {
        $homeScore = $d['HomeScoreFinal']; $awayScore = $d['AwayScoreFinal']
        $homePk = $d['HomeScorePk']; $awayPk = $d['AwayScorePk']
        $firstHalfHome = $d['HomeScoreFirstHalf']; $firstHalfAway = $d['AwayScoreFirstHalf']
        $referee = $d['RefereeMain']
        if ($d['CoachHome']) { $coachHome = $d['CoachHome'].Replace('(감독)', '').Trim() }
        if ($d['CoachAway']) { $coachAway = $d['CoachAway'].Replace('(감독)', '').Trim() }
    }

    $status = "'Scheduled'"
    if ($null -ne $homeScore -and '' -ne $homeScore) { $status = "'Completed'" }

    $rows.Add('(' + (SqlLit (Get-KfaGuid 'match' $r['SingleIdx'])) + ",'Official'," + (SqlLit (Get-KfaGuid 'tournament' $r['MatchIdx'])) + ',' + (SqlLit $stage) + ',' + (SqlLit $group) + ',' +
        (SqlLit $homeTeamId) + ',' + (SqlLit (DisplayName $r['HomeTeam'])) + ',' + (SqlLit $awayTeamId) + ',' + (SqlLit (DisplayName $r['AwayTeam'])) + ',' +
        (SqlNum $homeScore) + ',' + (SqlNum $awayScore) + ',' + (SqlNum $homePk) + ',' + (SqlNum $awayPk) + ',' +
        $status + ',' + (SqlLit (KstToUtc $r['MatchDate'] $r['Time'])) + ',' + (SqlLit $r['MatchArea']) + ',' +
        (SqlNum $firstHalfHome) + ',' + (SqlNum $firstHalfAway) + ',' + (SqlLit $referee) + ',' + (SqlNum $r['MatchNumber']) + ',' + (SqlLit $coachHome) + ',' + (SqlLit $coachAway) + ',' +
        "'KfaApi'," + (SqlLit $r['SingleIdx']) + ",'Synced')")
}
$lines = New-Lines
$lines.Add('SET NOCOUNT ON;')
Add-BatchInsert $lines ('INSERT INTO [dbo].[SoccerMatches] ([MatchId],[MatchType],[TournamentId],[StageType],[GroupName],[HomeTeamId],[HomeTeamName],[AwayTeamId],[AwayTeamName],' +
    '[HomeScore],[AwayScore],[HomePkScore],[AwayPkScore],[Status],[MatchedAt],[VenueName],' +
    '[FirstHalfHomeScore],[FirstHalfAwayScore],[RefereeName],[MatchSequence],[HomeCoachName],[AwayCoachName],[DataSource],[ExternalId],[SyncStatus])') $rows 250
Write-Seed '04_Matches.sql' $lines

#.// 05 — 이벤트 + 라인업 (상세 보유 경기만). 교체는 라인업 PlayTime으로 충분 — 이벤트 어휘 밖

function EventTypeOf($e) {
    switch ($e['EventCode']) {
        '11' { if ($e['IsPk'] -eq 'Y') { return 'PenaltyGoal' } return 'Goal' }
        '61' { return 'OwnGoal' }
        '31' { return 'YellowCard' }
        '41' { return 'RedCard' }
        default { return $null }
    }
}

$eventRows = New-Rows
$appearanceRows = New-Rows
$usedEventKeys = @{}   # 같은 분·같은 선수·같은 유형 중복(원본 8건) — 발생 횟수를 붙여 유일화
$usedAppKeys = @{}
foreach ($d in $details) {
    $matchGuid = Get-KfaGuid 'match' $d['SingleIdx']
    $homeKfa = Resolve-Team $d['MatchIdx'] $d['HomeTeam']
    $awayKfa = Resolve-Team $d['MatchIdx'] $d['AwayTeam']

    if ($d['Events']) {
        foreach ($e in $d['Events']) {
            $type = EventTypeOf $e
            if (-not $type) { continue }
            $kfa = $awayKfa
            $teamName = $d['AwayTeam']
            if ($e['Side'] -eq 'H') { $kfa = $homeKfa; $teamName = $d['HomeTeam'] }
            $teamId = $null; $playerId = $null
            if ($kfa) {
                $teamId = TeamGuidOf $kfa
                $playerId = Resolve-Player $kfa['TeamId'] $e['EntryNo'] $e['PlayerName']
            }
            $eventKey = $d['SingleIdx'] + ':' + $e['Side'] + ':' + $e['EventCode'] + ':' + $e['Time'] + ':' + $e['EntryNo'] + ':' + $e['PlayerName']
            if ($usedEventKeys.ContainsKey($eventKey)) {
                $usedEventKeys[$eventKey]++
                $eventKey += ':' + $usedEventKeys[$eventKey]
            }
            else { $usedEventKeys[$eventKey] = 1 }
            $eventRows.Add('(' + (SqlLit (Get-KfaGuid 'event' $eventKey)) + ',' + (SqlLit $matchGuid) + ',' + (SqlLit $teamId) + ',' + (SqlLit (DisplayName $teamName)) + ',' + (SqlLit $type) + ',' + (SqlLit $playerId) + ',' + (SqlLit $e['PlayerName']) + ',' + (SqlNum $e['EntryNo']) + ',' + (SqlNum $e['Time']) + ')')
        }
    }

    foreach ($side in @(
            @{ List = $d['HomeStarters']; Name = $d['HomeTeam']; Kfa = $homeKfa; Starter = 1 },
            @{ List = $d['HomeSubstitutes']; Name = $d['HomeTeam']; Kfa = $homeKfa; Starter = 0 },
            @{ List = $d['AwayStarters']; Name = $d['AwayTeam']; Kfa = $awayKfa; Starter = 1 },
            @{ List = $d['AwaySubstitutes']; Name = $d['AwayTeam']; Kfa = $awayKfa; Starter = 0 })) {
        if (-not $side.List) { continue }
        foreach ($a in $side.List) {
            $teamId = $null; $playerId = $null
            if ($side.Kfa) {
                $teamId = TeamGuidOf $side.Kfa
                $playerId = Resolve-Player $side.Kfa['TeamId'] $a['EntryNo'] $a['Name']
            }
            $captain = 0
            if ($a['IsCaptain'] -eq 'Y') { $captain = 1 }
            $appKey = $d['SingleIdx'] + ':' + $side.Name + ':' + $a['EntryNo'] + ':' + $a['Name']
            if ($usedAppKeys.ContainsKey($appKey)) {
                $usedAppKeys[$appKey]++
                $appKey += ':' + $usedAppKeys[$appKey]
            }
            else { $usedAppKeys[$appKey] = 1 }
            $appearanceRows.Add('(' + (SqlLit (Get-KfaGuid 'app' $appKey)) + ',' + (SqlLit $matchGuid) + ',' + (SqlLit $teamId) + ',' + (SqlLit (DisplayName $side.Name)) + ',' + (SqlLit $playerId) + ',' + (SqlLit $a['Name']) + ',' + (SqlNum $a['EntryNo']) + ',' + (SqlLit $a['Position']) + ',' + $captain + ',' + (SqlNum $a['PlayTime']) + ',' + $side.Starter + ')')
        }
    }
}
$lines = New-Lines
$lines.Add('SET NOCOUNT ON;')
Add-BatchInsert $lines 'INSERT INTO [dbo].[SoccerMatchEvents] ([EventId],[MatchId],[TeamId],[TeamName],[EventType],[PlayerId],[PlayerName],[JerseyNumber],[MinuteOfPlay])' $eventRows
Add-BatchInsert $lines 'INSERT INTO [dbo].[SoccerMatchAppearances] ([AppearanceId],[MatchId],[TeamId],[TeamName],[PlayerId],[PlayerName],[JerseyNumber],[Position],[IsCaptain],[MinutesPlayed],[IsStarter])' $appearanceRows
Write-Seed '05_MatchDetails.sql' $lines
Write-Host ("  events {0} / appearances {1}" -f $eventRows.Count, $appearanceRows.Count)

#.// 06 — 순위표 (완료 경기에서 계산 — 재계산 호출자가 아직 없어 시드가 만든다)

$rows = New-Rows
foreach ($t in $tournaments) {
    if (-not $resultsByTournament.ContainsKey($t['Idx'])) { continue }

    $tables = @{}   # "stage|group" → (팀명 → 집계)
    foreach ($r in $resultsByTournament[$t['Idx']]) {
        $d = $null
        if ($detailBySingle.ContainsKey($r['SingleIdx'])) { $d = $detailBySingle[$r['SingleIdx']] }
        $homeScore = $r['HomeScore']
        if ($d) { $homeScore = $d['HomeScoreFinal'] }
        if ($null -eq $homeScore -or '' -eq $homeScore) { continue }

        $stage, $group = StageOf $t $r['MatchGroup']
        if ($stage -eq 'Knockout') { continue }   # 토너먼트는 순위표 없음
        $key = $stage + '|' + $group

        if (-not $tables.ContainsKey($key)) { $tables[$key] = @{} }
        $table = $tables[$key]
        $awayScore = $r['AwayScore']
        if ($d) { $awayScore = $d['AwayScoreFinal'] }
        $hs = [int]$homeScore; $as = [int]$awayScore

        foreach ($pair in @(@($r['HomeTeam'], $hs, $as), @($r['AwayTeam'], $as, $hs))) {
            $name, $gf, $ga = $pair
            if (-not $table.ContainsKey($name)) { $table[$name] = @{ P = 0; W = 0; D = 0; L = 0; GF = 0; GA = 0 } }
            $s = $table[$name]
            $s.P++; $s.GF += $gf; $s.GA += $ga
            if ($gf -gt $ga) { $s.W++ } elseif ($gf -eq $ga) { $s.D++ } else { $s.L++ }
        }
    }

    foreach ($key in $tables.Keys) {
        $stage, $group = $key.Split('|')
        $ranked = $tables[$key].GetEnumerator() | ForEach-Object {
            [pscustomobject]@{ Name = $_.Key; P = $_.Value.P; W = $_.Value.W; D = $_.Value.D; L = $_.Value.L; GF = $_.Value.GF; GA = $_.Value.GA; Pts = $_.Value.W * 3 + $_.Value.D }
        } | Sort-Object @{ E = 'Pts'; Descending = $true }, @{ E = { $_.GF - $_.GA }; Descending = $true }, @{ E = 'GF'; Descending = $true }, 'Name'

        $rank = 0
        foreach ($s in $ranked) {
            $rank++
            $kfa = Resolve-Team $t['Idx'] $s.Name
            $teamId = $null
            if ($kfa) { $teamId = TeamGuidOf $kfa }
            # ExternalId는 VARCHAR(64) — 한글 팀명이 든 복합키가 넘치므로 md5(32자)로 접는다
            $extKey = $t['Idx'] + ':' + $key + ':' + $s.Name
            $extId = [BitConverter]::ToString($md5.ComputeHash([Text.Encoding]::UTF8.GetBytes($extKey))).Replace('-', '').ToLower()
            $rows.Add('(' + (SqlLit (Get-KfaGuid 'standing' $extKey)) + ',' + (SqlLit (Get-KfaGuid 'tournament' $t['Idx'])) + ',' + (SqlLit $stage) + ',' + (SqlLit $group) + ',' + (SqlLit $teamId) + ',' + (SqlLit (DisplayName $s.Name)) + ',' + $rank + ',' + $s.P + ',' + $s.W + ',' + $s.D + ',' + $s.L + ',' + $s.Pts + ',' + $s.GF + ',' + $s.GA + ",'KfaApi'," + (SqlLit $extId) + ",'Synced')")
        }
    }
}
$lines = New-Lines
$lines.Add('SET NOCOUNT ON;')
Add-BatchInsert $lines 'INSERT INTO [dbo].[SoccerTournamentStandings] ([StandingId],[TournamentId],[StageType],[GroupName],[TeamId],[TeamName],[TeamRank],[Played],[Won],[Drawn],[Lost],[Points],[GoalsFor],[GoalsAgainst],[DataSource],[ExternalId],[SyncStatus])' $rows
Write-Seed '06_Standings.sql' $lines
Write-Host ("  standings {0}" -f $rows.Count)

#.// 07 — 테스트 팀 연결: 검증fc를 플레이그라운드FC로 개명 + 쇼케이스 자리 GUID를 실제 TeamId로 치환

$pgFallbackGuid = Get-KfaGuid 'team' 'playgroundfc'
$lines = New-Lines
$lines.Add('SET NOCOUNT ON;')
$lines.Add("DECLARE @pg UNIQUEIDENTIFIER = (SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams] WHERE [TeamName] IN ('검증fc','$ShowcaseNewName') AND [DataSource] <> 'KfaApi' AND [DeletedAt] IS NULL);")
$lines.Add('-- 테스트 팀이 없는 DB(운영 등)에서는 플레이그라운드FC를 새로 만들어 검증fc 자리를 대체한다')
$lines.Add('IF @pg IS NULL')
$lines.Add('BEGIN')
$lines.Add("    SET @pg = '$pgFallbackGuid';")
$lines.Add("    INSERT INTO [dbo].[SoccerTeams] ([TeamId],[TeamName],[TeamType],[AgeGroup],[Slug],[IsPublicProfile],[DataSource],[ExternalId])")
$lines.Add("    VALUES (@pg, '$ShowcaseNewName', '클럽', 'U12', 'playgroundfc', 1, 'Seed', 'playgroundfc');")
$lines.Add('END')
$lines.Add("UPDATE [dbo].[SoccerTeams] SET [TeamName] = '$ShowcaseNewName', [UpdatedAt] = GETUTCDATE() WHERE [TeamId] = @pg;")
# 기존 친선·검증 시드 행에 문자열로 남은 옛 팀명(검증fc)도 함께 갱신한다 — 화면은 이 문자열을 그대로 보여준다
$lines.Add("UPDATE [dbo].[SoccerMatches] SET [HomeTeamName] = '$ShowcaseNewName' WHERE [HomeTeamId] = @pg AND [HomeTeamName] <> '$ShowcaseNewName';")
$lines.Add("UPDATE [dbo].[SoccerMatches] SET [AwayTeamName] = '$ShowcaseNewName' WHERE [AwayTeamId] = @pg AND [AwayTeamName] <> '$ShowcaseNewName';")
$lines.Add("UPDATE [dbo].[SoccerMatchEvents] SET [TeamName] = '$ShowcaseNewName' WHERE [TeamId] = @pg AND [TeamName] <> '$ShowcaseNewName';")
$lines.Add("UPDATE [dbo].[SoccerMatchAppearances] SET [TeamName] = '$ShowcaseNewName' WHERE [TeamId] = @pg AND [TeamName] <> '$ShowcaseNewName';")
$lines.Add("UPDATE [dbo].[SoccerTournamentStandings] SET [TeamName] = '$ShowcaseNewName' WHERE [TeamId] = @pg AND [TeamName] <> '$ShowcaseNewName';")
foreach ($id in $showcaseIds.Keys) {
    $guid = Get-KfaGuid 'team' $id
    $lines.Add("UPDATE [dbo].[SoccerMatches] SET [HomeTeamId] = @pg WHERE [HomeTeamId] = '$guid';")
    $lines.Add("UPDATE [dbo].[SoccerMatches] SET [AwayTeamId] = @pg WHERE [AwayTeamId] = '$guid';")
    $lines.Add("UPDATE [dbo].[SoccerMatchEvents] SET [TeamId] = @pg WHERE [TeamId] = '$guid';")
    $lines.Add("UPDATE [dbo].[SoccerMatchAppearances] SET [TeamId] = @pg WHERE [TeamId] = '$guid';")
    $lines.Add("UPDATE [dbo].[SoccerTournamentStandings] SET [TeamId] = @pg WHERE [TeamId] = '$guid';")
    $lines.Add("UPDATE [dbo].[SoccerPlayers] SET [TeamId] = @pg WHERE [TeamId] = '$guid';")
    $lines.Add("UPDATE [dbo].[SoccerTeamPlayers] SET [TeamId] = @pg WHERE [TeamId] = '$guid';")
}
$lines.Add('GO')
Write-Seed '07_PlaygroundFc.sql' $lines

#.// 08·09 — 테스트 계정: 로스터와 상세 경기를 둘 다 가진 팀 4개 + 소속 선수 6명 (총 10계정)
#    team1~4@playground.com(TeamAdmin) · player1~6@playground.com(Player) · 비밀번호 password123!
#    08은 Account DB, 09는 Soccer DB에 적용한다. 쇼케이스(플레이그라운드FC)는 기존 검증 계정이 관리자라 제외.

$detailCountByTeam = @{}
foreach ($d in $details) {
    foreach ($n in @($d['HomeTeam'], $d['AwayTeam'])) { $detailCountByTeam[$n] = 1 + $(if ($detailCountByTeam.ContainsKey($n)) { $detailCountByTeam[$n] } else { 0 }) }
}
$rosterByTeamName = @{}
foreach ($p in $players) {
    if (-not $rosterByTeamName.ContainsKey($p['TeamName'])) { $rosterByTeamName[$p['TeamName']] = New-Object 'Collections.Generic.List[object]' }
    $rosterByTeamName[$p['TeamName']].Add($p)
}

$selectedTeams = $rosterByTeamName.Keys |
    Where-Object { $_ -ne $ShowcaseKfaName -and $detailCountByTeam.ContainsKey($_) -and $null -ne $teamByName[$_] } |
    ForEach-Object { [pscustomobject]@{ Name = $_; Matches = $detailCountByTeam[$_]; Roster = $rosterByTeamName[$_].Count } } |
    Sort-Object @{ E = 'Matches'; Descending = $true }, @{ E = 'Roster'; Descending = $true }, 'Name' |
    Select-Object -First 4

$AccountHash = 'AQAAAAIAAYagAAAAEJzWUF0xvuIiMlaBopf5Np7aOJ8n4cseTx8BHeQMX4OnCIXUfErv9xub2VA1NwWRug=='
$accountRows = New-Rows
$linkLines = New-Lines
$linkLines.Add('SET NOCOUNT ON;')
$emails = New-Object 'Collections.Generic.List[string]'
$playerIndex = 0
$teamIndex = 0
$perTeamPlayers = @(2, 2, 1, 1)   # 4팀에서 2·2·1·1명 = 선수 6계정

foreach ($sel in $selectedTeams) {
    $teamIndex++
    $kfa = $teamByName[$sel.Name]
    $teamGuid = Get-KfaGuid 'team' $kfa['TeamId']
    $email = "team$teamIndex@playground.com"
    $userGuid = Get-KfaGuid 'user' $email
    $emails.Add($email)
    $accountRows.Add('(' + (SqlLit $userGuid) + ',' + (SqlLit $email) + ',1,' + (SqlLit $AccountHash) + ",'Local'," + (SqlLit ($sel.Name + ' 관리자')) + ",'TeamAdmin')")
    # 관리자 연결 + 공개홈 활성(슬러그는 로마자 파생 — 기존 값이 있으면 유지)
    $linkLines.Add("UPDATE [dbo].[SoccerTeams] SET [ManagerUserId] = '$userGuid', [IsPublicProfile] = 1, [Slug] = COALESCE([Slug], dbo.UfnRomanizeKoreanSlug([TeamName])) WHERE [TeamId] = '$teamGuid';")

    $sorted = $rosterByTeamName[$sel.Name] | Sort-Object { $v = 999; [int]::TryParse($_['EntryNo'], [ref]$v) | Out-Null; $v }
    foreach ($p in $sorted | Select-Object -First $perTeamPlayers[$teamIndex - 1]) {
        $playerIndex++
        $pEmail = "player$playerIndex@playground.com"
        $pUserGuid = Get-KfaGuid 'user' $pEmail
        $pGuid = Get-KfaGuid 'player' ($p['TeamId'] + ':' + $p['EntryNo'] + ':' + $p['Name'])
        $emails.Add($pEmail)
        $accountRows.Add('(' + (SqlLit $pUserGuid) + ',' + (SqlLit $pEmail) + ',1,' + (SqlLit $AccountHash) + ",'Local'," + (SqlLit $p['Name']) + ",'Player')")
        $linkLines.Add("UPDATE [dbo].[SoccerPlayers] SET [UserId] = '$pUserGuid' WHERE [PlayerId] = '$pGuid';")
        Write-Host ("  {0} = {1} {2} (#{3}, {4})" -f $pEmail, $sel.Name, $p['Name'], $p['EntryNo'], $p['Position'])
    }
    Write-Host ("  team$teamIndex@playground.com = {0} (상세 {1}경기 · 로스터 {2}명)" -f $sel.Name, $sel.Matches, $sel.Roster)
}

$lines = New-Lines
$lines.Add('SET NOCOUNT ON;')
$lines.Add('-- 테스트 계정 10종 — 비밀번호 password123! (검증 계정 공통 해시 재사용). 로컬 전용, 운영 배포 금지.')
$lines.Add("DELETE FROM [dbo].[Users] WHERE [Email] IN ('" + ($emails -join "','") + "');")
Add-BatchInsert $lines 'INSERT INTO [dbo].[Users] ([UserId],[Email],[EmailConfirmed],[PasswordHash],[AuthProvider],[DisplayName],[UserRole])' $accountRows
Write-Seed '08_Accounts.Account.sql' $lines

$linkLines.Add('GO')
Write-Seed '09_AccountLinks.sql' $linkLines

#.// 사진 업로드 목록

$manifestJson = $serializer.Serialize($photoManifest)
[IO.File]::WriteAllText((Join-Path $OutDir 'photo-manifest.json'), $manifestJson, $utf8Bom)
Write-Host ("photo manifest {0}건" -f $photoManifest.Count)
Write-Host "완료 → $OutDir"
