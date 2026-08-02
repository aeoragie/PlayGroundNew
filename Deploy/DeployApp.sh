#!/usr/bin/env bash
#
# 앱 배포 — 서버에서 실행한다. CI가 만든 publish 산출물(zip)을 받아 교체하고 재시작한다.
#
#   sudo playground-deploy /tmp/playground-app.zip
#
# **직전 버전을 남겨 두고 바꾼다** — 되돌릴 게 없으면 롤백이 아니라 재배포가 되고,
# 그때는 이미 서비스가 내려가 있다.

set -euo pipefail

ARCHIVE="${1:?사용법: playground-deploy <publish.zip>}"

APP_DIR=/var/www/playground
PREV_DIR=/var/www/playground.prev
STAGE_DIR=/var/www/playground.new
SERVICE=playground

log() { echo "[deploy] $*"; }

[[ -f "$ARCHIVE" ]] || { echo "산출물이 없다: $ARCHIVE" >&2; exit 1; }

#.// 산출물 검사 — 로컬 시크릿이 섞여 들어오면 운영 환경변수를 덮어쓴다
log "산출물 검사"
rm -rf "$STAGE_DIR"
mkdir -p "$STAGE_DIR"
unzip -q "$ARCHIVE" -d "$STAGE_DIR"

if [[ -f "$STAGE_DIR/appsettings.Local.json" ]]; then
    echo "산출물에 appsettings.Local.json이 있다 — 개발 시크릿이 운영 설정을 덮어쓴다. 중단." >&2
    rm -rf "$STAGE_DIR"
    exit 1
fi

[[ -f "$STAGE_DIR/PlayGround.Server.dll" ]] || {
    echo "PlayGround.Server.dll이 없다 — 산출물이 잘못됐다. 중단." >&2
    rm -rf "$STAGE_DIR"
    exit 1
}

#.// 교체
log "서비스 중지"
systemctl stop "$SERVICE"

log "직전 버전 보관"
rm -rf "$PREV_DIR"
[[ -d "$APP_DIR" ]] && mv "$APP_DIR" "$PREV_DIR"

mv "$STAGE_DIR" "$APP_DIR"
chown -R playground:playground "$APP_DIR"

log "서비스 시작"
systemctl start "$SERVICE"

#.// 살아났는지 확인 — 안 되면 즉시 되돌린다
log "기동 확인"
for i in $(seq 1 30); do
    if curl -fsS -o /dev/null http://127.0.0.1:5000/api/soccer/landing/contents; then
        log "정상 — 배포 완료"
        exit 0
    fi
    sleep 2
done

echo "[deploy] 기동 확인 실패 — 직전 버전으로 되돌린다" >&2
systemctl stop "$SERVICE"
rm -rf "$APP_DIR"
mv "$PREV_DIR" "$APP_DIR"
systemctl start "$SERVICE"
echo "[deploy] 롤백 완료. 로그: journalctl -u $SERVICE -n 100" >&2
exit 1
