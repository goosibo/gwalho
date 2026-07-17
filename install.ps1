$ErrorActionPreference = "Stop"

$repo = "goosibo/gwalho"
$installDir = "$env:LOCALAPPDATA\gwalho"

Write-Host "gwalho 설치를 시작합니다..."

New-Item -ItemType Directory -Force -Path $installDir | Out-Null

$latest = Invoke-RestMethod "https://api.github.com/repos/$repo/releases/latest"
$asset = $latest.assets | Where-Object { $_.name -eq "gwalho.exe" }

if (-not $asset) {
    Write-Error "gwalho.exe를 최신 릴리스에서 찾을 수 없습니다."
    exit 1
}

Write-Host "다운로드 중: $($asset.name) ($('{0:N1}' -f ($asset.size / 1MB)) MB)"
Invoke-WebRequest -Uri $asset.browser_download_url -OutFile "$installDir\gwalho.exe"

$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($userPath -notlike "*$installDir*") {
    [Environment]::SetEnvironmentVariable("Path", "$userPath;$installDir", "User")
    Write-Host "PATH에 등록했습니다."
}

Write-Host ""
Write-Host "설치 완료! 새 터미널을 열고 'gwalho'를 입력해보세요."