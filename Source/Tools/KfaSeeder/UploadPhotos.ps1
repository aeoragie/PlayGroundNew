# 쇼케이스 로스터 사진을 KFA 파일 서버에서 받아 개발 S3 버킷에 올린다.
# GenerateKfaSeed.ps1이 만든 photo-manifest.json(원본 URL → S3 키)을 소비한다.
# 선행: aws configure (이 PC의 IAM 사용자 자격 증명).

param(
    [string]$ManifestPath = (Join-Path $PSScriptRoot '..\..\Database\Soccer\Seeds\Kfa\photo-manifest.json'),
    [string]$Bucket = 'dev-playgroundsport-images-599615474479-ap-northeast-2-an'
)

$ErrorActionPreference = 'Stop'
$manifest = Get-Content $ManifestPath -Encoding UTF8 | ConvertFrom-Json
$temp = Join-Path $env:TEMP 'kfa-photos'
New-Item -ItemType Directory -Force $temp | Out-Null

$ok = 0
foreach ($item in $manifest) {
    $file = Join-Path $temp ([IO.Path]::GetFileName($item.key))
    try {
        Invoke-WebRequest -Uri $item.source -OutFile $file -TimeoutSec 30 | Out-Null
        aws s3 cp $file "s3://$Bucket/$($item.key)" --content-type image/jpeg --no-progress | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "aws s3 cp failed" }
        $ok++
        Write-Host "OK  $($item.key)"
    }
    catch {
        # 실패한 사진은 화면에서 이니셜 아바타로 폴백된다 — 전체를 멈추지 않는다
        Write-Host "SKIP $($item.source) — $($_.Exception.Message)"
    }
}
Write-Host ("업로드 {0}/{1}건 완료 → s3://{2}" -f $ok, $manifest.Count, $Bucket)
