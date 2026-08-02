# 로컬 개발용 Redis(Windows) 내려받기.
# 바이너리는 저장소에 두지 않는다(용량) — 이 스크립트가 받아서 해시로 검증한다.
# 사용법:  .\Others\fetch-redis.ps1        (버전 고정본)
#          .\Others\fetch-redis.ps1 -Latest (최신 릴리스로 갱신 — 해시는 릴리스 노트에서 대조)

[CmdletBinding()]
param(
    # 고정 버전 — 올릴 때는 이 둘과 Others/README.md 를 함께 갱신한다.
    [string]$Version = "8.10.0",
    [string]$Sha256 = "525B2AC16F45943973F44B7C49A5DCE2FF5A21B8C1C93EC059F4902B1C5BD622",
    [switch]$Latest,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSCommandPath
$destination = Join-Path $root "Redis"
$repository = "redis-windows/redis-windows"

if ((Test-Path $destination) -and -not $Force) {
    $existing = Join-Path $destination "redis-server.exe"
    if (Test-Path $existing) {
        Write-Host "이미 있습니다: $destination"
        Write-Host "다시 받으려면 -Force 를 붙이세요."
        exit 0
    }
}

#.// 최신 버전 조회 (-Latest)

if ($Latest) {
    Write-Host "최신 릴리스 조회 중..."
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repository/releases/latest" -UseBasicParsing
    $Version = $release.tag_name

    # 릴리스 노트에 자산별 SHA256이 공개돼 있다 — 그 값을 기준으로 검증한다.
    # 노트 형식은 "Hash : <해시>" 바로 다음 줄이 "Path : ...<파일명>.zip" 이다.
    # 두 줄 사이에 다른 항목이 끼면 안 되므로 개행만 허용한다 —
    # `[\s\S]*?` 로 두면 앞선 다른 자산의 해시를 이 파일명과 짝지어 버린다(실제로 겪음).
    $pattern = 'Hash\s*:\s*([0-9A-Fa-f]{64})\s*\r?\n\s*Path\s*:\s*[^\r\n]*?(Redis-[^\r\n]*?msys2-with-Service\.zip)'
    $match = [regex]::Match($release.body, $pattern)
    if (-not $match.Success) {
        throw "릴리스 노트에서 msys2-with-Service 해시를 찾지 못했습니다. 수동으로 확인하세요: https://github.com/$repository/releases"
    }

    $Sha256 = $match.Groups[1].Value.ToUpperInvariant()
    Write-Host "최신 버전: $Version"
    Write-Host "공개 해시: $Sha256"
}

#.// 내려받기 + 검증

$asset = "Redis-$Version-Windows-x64-msys2-with-Service.zip"
$url = "https://github.com/$repository/releases/download/$Version/$asset"
$archive = Join-Path $env:TEMP $asset

Write-Host "내려받는 중: $url"
Invoke-WebRequest -Uri $url -OutFile $archive -UseBasicParsing

$actual = (Get-FileHash $archive -Algorithm SHA256).Hash.ToUpperInvariant()
if ($actual -ne $Sha256.ToUpperInvariant()) {
    Remove-Item $archive -Force
    throw "해시 불일치 — 받은 파일을 버렸습니다.`n  기대: $Sha256`n  실제: $actual"
}

Write-Host "해시 확인 OK"

#.// 펼치기 (중첩 폴더면 평탄화)

if (Test-Path $destination) {
    Remove-Item $destination -Recurse -Force
}

Expand-Archive -Path $archive -DestinationPath $destination -Force
Remove-Item $archive -Force

$inner = @(Get-ChildItem $destination -Directory)
if ($inner.Count -eq 1 -and @(Get-ChildItem $destination -File).Count -eq 0) {
    Get-ChildItem $inner[0].FullName | Move-Item -Destination $destination
    Remove-Item $inner[0].FullName -Recurse -Force
}

Write-Host ""
Write-Host "완료: $destination (Redis $Version)"
Write-Host "실행:  .\Others\Redis\redis-server.exe --port 6379 --save `"`""
Write-Host "확인:  .\Others\Redis\redis-cli.exe -p 6379 PING"

if ($Latest) {
    Write-Host ""
    Write-Host "버전을 올렸다면 이 스크립트의 기본값과 Others/README.md 를 갱신하세요:" -ForegroundColor Yellow
    Write-Host "  -Version $Version"
    Write-Host "  -Sha256  $Sha256"
}
