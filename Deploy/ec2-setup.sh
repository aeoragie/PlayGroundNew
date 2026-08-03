#!/usr/bin/env bash
#
# EC2 최초 셋업 (Ubuntu 22.04 LTS) — user-data에 그대로 붙여 넣는다.
#
# **시크릿을 넣지 않는다.** user-data는 인스턴스 메타데이터로 조회되므로
# sa 비밀번호 같은 값은 SSH 접속 후 대화형으로 설정한다(Deploy/AwsSetup.md "SQL Server 초기 설정").
#
# 로그: /var/log/cloud-init-output.log

set -euo pipefail

log() { echo "[setup] $*"; }

#.// 시간대 — UTC (2026-08-02 결정).
# 서버 시간대로 애플리케이션 로직을 맞추지 않는다. 호스트 설정에 의존하는 코드는
# 어디서 도는지에 따라 결과가 달라지고, 그 사실이 코드에 드러나지 않는다.
# 한국 시각은 코드·DB의 명시적 래퍼가 책임진다(Docs/Development/ReleasePlan.md "시각 기준").
log "시간대 UTC"
timedatectl set-timezone UTC

log "기본 패키지"
export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y curl gnupg ca-certificates apt-transport-https software-properties-common unzip

#.// Microsoft 패키지 서명 키 (SQL Server·도구·.NET 공통)
log "Microsoft 저장소 키"
curl -fsSL https://packages.microsoft.com/keys/microsoft.asc \
  | gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg

#.// SQL Server 2022 — Ubuntu 24.04용 저장소는 없다(2025만 제공). 22.04를 쓰는 이유.
log "SQL Server 2022 저장소"
echo "deb [arch=amd64,arm64,armhf signed-by=/usr/share/keyrings/microsoft-prod.gpg] \
https://packages.microsoft.com/ubuntu/22.04/mssql-server-2022 jammy main" \
  > /etc/apt/sources.list.d/mssql-server-2022.list

#.// 도구(sqlcmd·bcp)와 .NET은 prod 저장소
log "Microsoft prod 저장소"
echo "deb [arch=amd64,arm64,armhf signed-by=/usr/share/keyrings/microsoft-prod.gpg] \
https://packages.microsoft.com/ubuntu/22.04/prod jammy main" \
  > /etc/apt/sources.list.d/microsoft-prod.list

apt-get update

log "SQL Server 설치 (초기 설정은 SSH 접속 후 대화형)"
apt-get install -y mssql-server

log "sqlcmd·bcp"
ACCEPT_EULA=Y apt-get install -y mssql-tools18 unixodbc-dev
# 로그인 셸에서 sqlcmd를 바로 쓰도록
ln -sf /opt/mssql-tools18/bin/sqlcmd /usr/local/bin/sqlcmd
ln -sf /opt/mssql-tools18/bin/bcp /usr/local/bin/bcp

log "ASP.NET Core 런타임"
apt-get install -y aspnetcore-runtime-10.0

log "Redis"
apt-get install -y redis-server
# 외부에 열지 않는다 — 같은 박스의 앱만 쓴다
sed -i 's/^# *bind .*/bind 127.0.0.1 ::1/' /etc/redis/redis.conf
sed -i 's/^bind .*/bind 127.0.0.1 ::1/' /etc/redis/redis.conf
sed -i 's/^supervised .*/supervised systemd/' /etc/redis/redis.conf
systemctl enable redis-server
systemctl restart redis-server

log "Nginx"
apt-get install -y nginx
systemctl enable nginx

log "certbot (Let's Encrypt)"
apt-get install -y certbot python3-certbot-nginx

#.// 한글 폰트 — OG 카드가 SkiaSharp로 한글을 그린다.
# 없으면 글자가 통째로 깨진다(Windows에는 있어서 개발 중엔 드러나지 않는다).
# fontconfig를 명시한다 — 서버 우분투엔 기본으로 없어서 fc-cache가 command not found가 된다.
log "한글 폰트"
apt-get install -y fontconfig fonts-nanum fonts-noto-cjk
fc-cache -f

log "앱 디렉터리"
useradd --system --no-create-home --shell /usr/sbin/nologin playground || true
mkdir -p /var/www/playground /var/log/playground /var/backups/playground
chown -R playground:playground /var/www/playground /var/log/playground

log "방화벽 (보안그룹과 이중 방어)"
ufw allow OpenSSH
ufw allow 'Nginx Full'
# SQL Server 원격 접속(SSMS·스키마 배포). **접속 IP 제한은 보안 그룹이 한다** —
# ufw는 "어느 포트를 여는가"만 담당하는 2차 방어다. 여기서 빠뜨리면 보안 그룹이
# 열려 있어도 호스트에서 막혀 연결이 조용히 시간 초과된다.
ufw allow 47821/tcp comment 'SQL Server'
ufw --force enable

log "완료 — 다음: Deploy/AwsSetup.md \"SQL Server 초기 설정\""
