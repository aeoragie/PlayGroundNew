#!/usr/bin/env bash
#
# DB 백업 → 로컬 → S3.
#
# **Express에는 SQL Server Agent가 없다.** DB가 스스로 스케줄을 돌리지 못하므로
# cron으로 돌린다. 앱과 DB가 한 인스턴스에 있어 **로컬 디스크 백업은 백업이 아니다** —
# 인스턴스·EBS를 잃으면 같이 사라진다. 반드시 S3까지 올라가야 한다.
#
# 설치:
#   sudo cp Deploy/BackupDatabase.sh /usr/local/bin/playground-backup
#   sudo chmod 750 /usr/local/bin/playground-backup
#   sudo crontab -e
#     0 4 * * * /usr/local/bin/playground-backup >> /var/log/playground/backup.log 2>&1
#
# 전제:
#   - /etc/playground/backup.env 에 SA_PASSWORD, S3_BUCKET (chmod 600)
#   - 인스턴스에 S3 PutObject 권한 IAM 역할 (액세스 키를 파일에 두지 않는다)
#   - awscli 설치: sudo apt-get install -y awscli

set -euo pipefail

source /etc/playground/backup.env   # SA_PASSWORD, S3_BUCKET

BACKUP_DIR=/var/backups/playground
RETAIN_DAYS=7                        # 로컬은 짧게 — 장기 보관은 S3 수명주기 정책으로
STAMP=$(date +%Y%m%d-%H%M%S)

mkdir -p "$BACKUP_DIR"

for DB in PlayGround_Account PlayGround_Soccer; do
    FILE="$BACKUP_DIR/${DB}-${STAMP}.bak"
    echo "[backup] $DB → $FILE"

    /usr/local/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C -b -Q \
        "BACKUP DATABASE [$DB] TO DISK = N'$FILE' WITH INIT, COMPRESSION, CHECKSUM, STATS = 10;"

    # 복원 가능한 파일인지 즉시 확인한다 — 못 푸는 백업을 몇 달 쌓아 두는 사고를 막는다
    /usr/local/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C -b -Q \
        "RESTORE VERIFYONLY FROM DISK = N'$FILE';"

    echo "[backup] S3 업로드"
    aws s3 cp "$FILE" "s3://${S3_BUCKET}/db/${DB}/${STAMP}.bak" --only-show-errors
done

echo "[backup] 로컬 ${RETAIN_DAYS}일 초과분 정리"
find "$BACKUP_DIR" -name '*.bak' -mtime "+${RETAIN_DAYS}" -delete

echo "[backup] 완료 $(date '+%F %T')"
