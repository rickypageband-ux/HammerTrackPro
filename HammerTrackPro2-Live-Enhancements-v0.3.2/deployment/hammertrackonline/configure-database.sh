#!/usr/bin/env bash
set -euo pipefail

environment_file=/etc/hammertrackonline/database.env
database_password=$(sudo sed -n 's/.*Password=\([^;]*\).*/\1/p' "$environment_file")

if [[ ${#database_password} -ne 64 ]]; then
    echo "The stored database password is missing or malformed." >&2
    exit 1
fi

sudo -u postgres psql -v ON_ERROR_STOP=1 --set=database_password="$database_password" <<'SQL'
ALTER ROLE hammertrackonline PASSWORD :'database_password';
SQL

PGPASSWORD="$database_password" psql \
    --host=127.0.0.1 \
    --username=hammertrackonline \
    --dbname=hammertrackonline \
    --tuples-only \
    --command='SELECT current_user;'

unset database_password PGPASSWORD
