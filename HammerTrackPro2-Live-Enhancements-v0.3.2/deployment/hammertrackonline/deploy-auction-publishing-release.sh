#!/usr/bin/env bash
set -euo pipefail

release=/tmp/hammertrackonline-auction-publishing-release
migration=/tmp/003_auction_catalog_and_publishing.sql
service_file=/tmp/hammertrackonline-auction-publishing.service
application=/var/www/vhosts/hammertrackonline.com/app
backup=/var/www/vhosts/hammertrackonline.com/app-backup-pre-auction-publishing
failed=/tmp/hammertrackonline-auction-publishing-failed
database_environment=/etc/hammertrackonline/database.env
application_user=hammertrackonline.co_4wggkn4o9rh

if [[ ! -f "$release/HammerTrackLive.Web.dll" || ! -f "$migration" || ! -f "$service_file" ]]; then
    echo "The release, ordered migration, or service definition is missing." >&2
    exit 1
fi
if [[ -e "$backup" || -e "$failed" ]]; then
    echo "A deployment safety path already exists; refusing to overwrite it." >&2
    exit 1
fi

database_password=$(sed -n 's/.*Password=\([^;]*\).*/\1/p' "$database_environment")
if [[ ${#database_password} -ne 64 ]]; then echo "The database password is missing or malformed." >&2; exit 1; fi

install -d -m 0700 /var/backups/hammertrackonline
PGPASSWORD="$database_password" pg_dump --host=127.0.0.1 --username=hammertrackonline --dbname=hammertrackonline --format=custom --file=/tmp/hammertrackonline-pre-auction-publishing.dump
install -m 0600 /tmp/hammertrackonline-pre-auction-publishing.dump /var/backups/hammertrackonline/pre-auction-publishing.dump
rm /tmp/hammertrackonline-pre-auction-publishing.dump
PGPASSWORD="$database_password" psql --host=127.0.0.1 --username=hammertrackonline --dbname=hammertrackonline --set=ON_ERROR_STOP=1 --file="$migration"
unset database_password PGPASSWORD

systemctl stop hammertrackonline.service
mv "$application" "$backup"
install -d -m 0750 -o "$application_user" -g psacln "$application"
install -d -m 0750 -o "$application_user" -g psacln /var/lib/hammertrackonline/media
cp -a "$release/." "$application/"
cp "$backup/appsettings.Production.json" "$application/appsettings.Production.json"
chown -R "$application_user":psacln "$application"
install -m 0644 "$service_file" /etc/systemd/system/hammertrackonline.service
systemctl daemon-reload
systemctl start hammertrackonline.service

healthy=false
for _ in {1..15}; do
    if curl --fail --silent http://127.0.0.1:5080/health >/dev/null; then healthy=true; break; fi
    sleep 1
done
if [[ "$healthy" != true ]]; then
    systemctl stop hammertrackonline.service
    mv "$application" "$failed"
    mv "$backup" "$application"
    systemctl start hammertrackonline.service
    echo "The release failed its health check and application files were rolled back." >&2
    exit 1
fi
systemctl --no-pager --full status hammertrackonline.service
