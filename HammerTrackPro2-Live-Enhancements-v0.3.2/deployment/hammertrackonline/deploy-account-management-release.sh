#!/usr/bin/env bash
set -euo pipefail

release=/tmp/hammertrackonline-account-management-release
migration=/tmp/002_account_invitations.sql
service_file=/tmp/hammertrackonline-account-management.service
application=/var/www/vhosts/hammertrackonline.com/app
backup=/var/www/vhosts/hammertrackonline.com/app-backup-pre-account-management
database_environment=/etc/hammertrackonline/database.env
application_user=hammertrackonline.co_4wggkn4o9rh

if [[ ! -f "$release/HammerTrackLive.Web.dll" || ! -f "$migration" || ! -f "$service_file" ]]; then
    echo "The release, ordered migration, or service definition is missing." >&2
    exit 1
fi

if [[ -e "$backup" ]]; then
    echo "The application backup path already exists; refusing to overwrite it." >&2
    exit 1
fi

database_password=$(sudo sed -n 's/.*Password=\([^;]*\).*/\1/p' "$database_environment")
if [[ ${#database_password} -ne 64 ]]; then
    echo "The stored database password is missing or malformed." >&2
    exit 1
fi

sudo install -d -m 0700 /var/backups/hammertrackonline
PGPASSWORD="$database_password" pg_dump \
    --host=127.0.0.1 \
    --username=hammertrackonline \
    --dbname=hammertrackonline \
    --format=custom \
    --file=/tmp/hammertrackonline-pre-account-management.dump
sudo install -m 0600 /tmp/hammertrackonline-pre-account-management.dump \
    /var/backups/hammertrackonline/pre-account-management.dump
rm /tmp/hammertrackonline-pre-account-management.dump

PGPASSWORD="$database_password" psql \
    --host=127.0.0.1 \
    --username=hammertrackonline \
    --dbname=hammertrackonline \
    --set=ON_ERROR_STOP=1 \
    --file="$migration"
unset database_password PGPASSWORD

sudo systemctl stop hammertrackonline.service
sudo mv "$application" "$backup"
sudo install -d -m 0750 -o "$application_user" -g psacln "$application"
sudo cp -a "$release/." "$application/"
sudo cp "$backup/appsettings.Production.json" "$application/appsettings.Production.json"
sudo chown -R "$application_user":psacln "$application"
sudo install -m 0644 "$service_file" /etc/systemd/system/hammertrackonline.service
sudo systemctl daemon-reload
sudo systemctl start hammertrackonline.service

healthy=false
for _ in {1..15}; do
    if curl --fail --silent http://127.0.0.1:5080/health >/dev/null; then
        healthy=true
        break
    fi
    sleep 1
done

if [[ "$healthy" != true ]]; then
    sudo systemctl stop hammertrackonline.service
    sudo mv "$application" /tmp/hammertrackonline-account-management-failed
    sudo mv "$backup" "$application"
    sudo systemctl start hammertrackonline.service
    echo "The release failed its health check and the application files were rolled back." >&2
    exit 1
fi

sudo systemctl --no-pager --full status hammertrackonline.service
