# DB 초기화 + 전체 시드 원샷 러너.
#
#   로컬:  .\ResetAndSeed.ps1
#   원격:  .\ResetAndSeed.ps1 -Server '54.180.64.167,47821' -User playgroundadmin -Password '<비번>'
#
# 순서: ① 두 DB 데이터 전량 삭제(ResetData — 스키마·프로시저 유지, 디버그 시계 제외)
#       ② 마스터 시드 재적용(랜딩 콘텐츠·강점 태그)
#       ③ KFA 샘플 시드 00~09 (08은 Account DB로 자동 분기 — 테스트 계정 10종 포함)
# 끝나면 플레이그라운드FC(슬러그 playgroundfc)가 테스트 팀으로 존재한다.

param(
    [string]$Server = '.\SQLEXPRESS',
    [string]$User,
    [string]$Password
)

$ErrorActionPreference = 'Stop'
$dbRoot = Join-Path $PSScriptRoot '..\..\Database'
$kfaDir = Join-Path $dbRoot 'Soccer\Seeds\Kfa'

function Invoke-Sql([string]$db, [string]$file) {
    $auth = @('-E')
    if ($User) { $auth = @('-U', $User, '-P', $Password, '-C') }   # 원격은 암호화 필수(-C)
    & sqlcmd -S $Server -d $db -b -f 65001 @auth -i $file
    if ($LASTEXITCODE -ne 0) { throw "실패: $db ← $file" }
}

Write-Host "== 초기화 (모든 데이터 삭제 — $Server)"
Invoke-Sql 'PlayGround_Account' (Join-Path $dbRoot 'Account\Maintenance\ResetData.sql')
Invoke-Sql 'PlayGround_Soccer' (Join-Path $dbRoot 'Soccer\Maintenance\ResetData.sql')

Write-Host '== 마스터 시드'
foreach ($f in Get-ChildItem (Join-Path $dbRoot 'Soccer\Seeds\*.sql') -File) {
    Write-Host "  $($f.Name)"
    Invoke-Sql 'PlayGround_Soccer' $f.FullName
}

Write-Host '== KFA 샘플 시드 (00~09)'
foreach ($f in Get-ChildItem (Join-Path $kfaDir '0*.sql') | Sort-Object Name) {
    $db = 'PlayGround_Soccer'
    if ($f.Name -like '*.Account.sql') { $db = 'PlayGround_Account' }
    Write-Host "  $($f.Name) → $db"
    Invoke-Sql $db $f.FullName
}

Write-Host '완료 — 확인: /records (대회 152) · /team/playgroundfc · team1~4·player1~6@playground.com 로그인'
