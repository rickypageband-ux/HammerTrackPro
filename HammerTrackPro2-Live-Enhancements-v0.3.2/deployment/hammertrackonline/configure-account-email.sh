#!/usr/bin/env bash
set -euo pipefail

if [[ $EUID -ne 0 ]]; then
    echo "Run this setup with sudo." >&2
    exit 1
fi

read -r -s -p "IONOS password for sales@hammertrackonline.com: " smtp_password
echo
if [[ -z "$smtp_password" || "$smtp_password" == *$'\n'* || "$smtp_password" == *$'\r'* ]]; then
    echo "The password cannot be blank or contain a line break." >&2
    exit 1
fi

SMTP_PASSWORD="$smtp_password" python3 - <<'PY'
import os
import smtplib
import ssl

password = os.environ["SMTP_PASSWORD"]
with smtplib.SMTP("smtp.ionos.com", 587, timeout=20) as client:
    client.ehlo()
    client.starttls(context=ssl.create_default_context())
    client.ehlo()
    client.login("sales@hammertrackonline.com", password)
PY

escaped_password=${smtp_password//\\/\\\\}
escaped_password=${escaped_password//\"/\\\"}
install -d -m 0700 /etc/hammertrackonline
temporary_file=$(mktemp /etc/hammertrackonline/account-email.env.XXXXXX)
chmod 0600 "$temporary_file"
cat >"$temporary_file" <<EOF
AccountEmail__Host=smtp.ionos.com
AccountEmail__Port=587
AccountEmail__UseSsl=true
AccountEmail__Username=sales@hammertrackonline.com
AccountEmail__Password="$escaped_password"
AccountEmail__FromAddress=sales@hammertrackonline.com
AccountEmail__FromName="HammerTrack Online"
EOF
chown root:root "$temporary_file"
mv "$temporary_file" /etc/hammertrackonline/account-email.env
unset smtp_password escaped_password SMTP_PASSWORD

echo "IONOS authentication succeeded and the root-only account email configuration was saved."
