#!/usr/bin/env bash
set -euo pipefail

release=/tmp/hammertrackonline-auth-20261103
service_file=/tmp/hammertrackonline-auth-20261103.service
application=/var/www/vhosts/hammertrackonline.com/app
backup=/var/www/vhosts/hammertrackonline.com/app-backup-pre-auth-20261103
failed=/var/www/vhosts/hammertrackonline.com/app-failed-auth-20261103
application_user=hammertrackonline.co_4wggkn4o9rh

if [[ ! -f "$release/HammerTrackLive.Web.dll" ]]; then
    echo "The uploaded release is incomplete." >&2
    exit 1
fi

sudo systemctl stop hammertrackonline.service

if [[ -e "$backup" ]]; then
    echo "The expected backup path already exists; refusing to overwrite it." >&2
    sudo systemctl start hammertrackonline.service
    exit 1
fi

if [[ -e "$failed" ]]; then
    sudo mv "$failed" /tmp/hammertrackonline-auth-20261103-first-attempt
fi

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
    sudo mv "$application" "$failed"
    sudo mv "$backup" "$application"
    sudo systemctl start hammertrackonline.service
    echo "The release failed its health check and was rolled back." >&2
    exit 1
fi

sudo systemctl --no-pager --full status hammertrackonline.service
