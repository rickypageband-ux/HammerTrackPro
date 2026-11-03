#!/usr/bin/env bash
set -euo pipefail

if [[ $EUID -ne 0 ]]; then
    echo "Run this bootstrap with sudo." >&2
    exit 1
fi

application=/var/www/vhosts/hammertrackonline.com/app
application_user=hammertrackonline.co_4wggkn4o9rh
database_environment=/etc/hammertrackonline/database.env
email=sales@hammertrackonline.com
display_name="HammerTrack Administrator"
password="Aa1!$(openssl rand -hex 24)"
database_connection=$(sed -n 's/^HAMMERTRACKONLINE_DB=//p' "$database_environment")
if [[ "$database_connection" == \"*\" && "$database_connection" == *\" ]]; then
    database_connection=${database_connection:1:${#database_connection}-2}
elif [[ "$database_connection" == \'*\' && "$database_connection" == *\' ]]; then
    database_connection=${database_connection:1:${#database_connection}-2}
fi

if [[ -z "$database_connection" ]]; then
    echo "The database connection is unavailable." >&2
    exit 1
fi

sudo -u "$application_user" env \
    ASPNETCORE_ENVIRONMENT=Production \
    HAMMERTRACKONLINE_DB="$database_connection" \
    Bootstrap__Email="$email" \
    Bootstrap__DisplayName="$display_name" \
    Bootstrap__Password="$password" \
    /usr/bin/dotnet "$application/HammerTrackLive.Web.dll" --bootstrap-platform-administrator

unset password database_connection
echo "Use Forgot Password for sales@hammertrackonline.com to choose the administrator password."
