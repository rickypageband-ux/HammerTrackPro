#!/usr/bin/env bash
set -euo pipefail

application=/var/www/vhosts/hammertrackonline.com/app
backup=/var/www/vhosts/hammertrackonline.com/app-backup-pre-existing-administrator-fix
failed=/tmp/hammertrackonline-existing-administrator-fix-failed
release=/tmp/hammertrackonline-existing-administrator-fix-release
application_user=hammertrackonline.co_4wggkn4o9rh

if [[ ! -d "$application" || ! -f "$release/HammerTrackLive.Web.dll" ]]; then
    echo "The current application or staged correction is missing." >&2
    exit 1
fi

if [[ -e "$backup" || -e "$failed" ]]; then
    echo "A correction safety path already exists; refusing to overwrite it." >&2
    exit 1
fi

systemctl stop hammertrackonline.service
mv "$application" "$backup"
install -d -m 0750 -o "$application_user" -g psacln "$application"
cp -a "$release/." "$application/"
cp "$backup/appsettings.Production.json" "$application/appsettings.Production.json"
chown -R "$application_user":psacln "$application"
systemctl start hammertrackonline.service

healthy=false
for _ in {1..15}; do
    if curl --fail --silent http://127.0.0.1:5080/health >/dev/null; then
        healthy=true
        break
    fi
    sleep 1
done

if [[ "$healthy" != true ]]; then
    systemctl stop hammertrackonline.service
    mv "$application" "$failed"
    mv "$backup" "$application"
    systemctl start hammertrackonline.service
    echo "The correction failed its health check and was rolled back." >&2
    exit 1
fi

systemctl --no-pager --full status hammertrackonline.service
