# HammerTrack Pro one-time demo download setup

1. In Plesk Files, open the domain root—the folder that contains both
   `httpdocs` and `private`. Do not start inside `httpdocs`.
2. Upload and extract the one-time-download ZIP in that domain root. Allow it
   to merge the included `httpdocs` and `private` folders and overwrite the
   listed website files.
3. In Plesk, open **Password-Protected Directories** and protect
   `/download-admin`. Create a private username and password for the site owner.
4. Visit `https://hammertrackpro.com/download-admin/` and sign in.
5. Enter a customer/reference name, choose when the unused link should expire,
   and select **Generate one-time link**.
6. Copy the displayed link and send it to that customer. The link opens a safe
   confirmation page so an email scanner cannot consume it. Pressing **Download
   installer** downloads the MSI once; every later attempt shows that the link
   is unavailable.

The MSI download does not start the seven-day application demo. Issue the
customer's signed `.htplicense` key separately when they are ready to begin.
Never upload the private license-signing key, license issuer, or issued customer
license files to the web server.
