# Installer and Offline Licensing

## Installer behavior

The WiX 5 MSI is a self-contained Windows x64 package. It installs application binaries and the offline user manual under `%ProgramFiles%\HammerTrack Pro`, creates Desktop and Start Menu shortcuts with the embedded HammerTrack Pro icon, registers `.htpbackup`, and displays version `0.7.50` in its installation UI and Windows installed-app list.

The installer displays the approved Hammer Track Pro Software License and Ownership Terms and requires explicit acceptance before installation can continue. The installer source of record is `HammerTrackPro2.Installer/Eula.rtf`; approved legal changes must update that file without paraphrasing the agreement.

The stable UpgradeCode supports in-place major upgrades. Each release must increase `ProductVersion`; users must not uninstall the previous release first. Application components are replaced, while `%ProgramData%\HammerTrackPro` is a permanent data root and is not removed on uninstall.

The installer creates and grants normal users access to:

```text
%ProgramData%\HammerTrackPro
├── hammertrack.db
├── Backups
├── Logs
└── Settings
    ├── workstation.json
    └── license.htplicense
```

Each workstation can select its HammerTrack printer from **Printer Setup**. The selected Windows printer name is saved in that PC's `workstation.json`; it is not stored in the shared auction database and does not affect another workstation. Existing configuration files remain compatible because a missing printer name continues to use the current Windows default until an operator saves a choice. If a saved printer is later removed or renamed, HammerTrack stops the print with an actionable message instead of silently sending the document to another printer.

On first start, the application copies a missing legacy database, WAL/SHM companions, backups, and workstation configuration from the prior `HammerTrackPro2` locations. Existing destination files are never overwritten, and legacy files remain as a recovery copy.

## Build the MSI

```powershell
dotnet restore .\HammerTrackPro2.Installer\HammerTrackPro2.Installer.wixproj
dotnet build .\HammerTrackPro2.Installer\HammerTrackPro2.Installer.wixproj -c Release
```

Output: `HammerTrackPro2.Installer\bin\x64\Release\HammerTrackPro-0.7.50-x64.msi`.

The package publishes HammerTrack as self-contained `win-x64`, so target PCs do not require a separate .NET runtime. The MSI is currently unsigned; production distribution should code-sign the executable and MSI with the publisher's trusted code-signing certificate.

## License workflow

HammerTrack requires a signed offline license whose Machine ID list contains the current PC. The activation screen displays and copies the Machine ID. Gather the IDs from the authorized host and cashier PCs, then issue one license containing those IDs.

The private key is intentionally outside Git and the installer at `%LocalAppData%\HammerTrackPro\LicenseIssuer\hammertrack-license-private.pem`. Keep that file private and back it up securely. Anyone with it can issue licenses. The corresponding public key is compiled into HammerTrack and can only validate signatures.

Issue a license from the repository root:

```powershell
dotnet run --project .\HammerTrackPro2.LicenseIssuer\HammerTrackPro.LicenseIssuer.csproj -c Release -- issue --key "$env:LOCALAPPDATA\HammerTrackPro\LicenseIssuer\hammertrack-license-private.pem" --customer "Customer or Auction Company" --machines "FIRST-MACHINE-ID,SECOND-MACHINE-ID" --output ".\Customer.htplicense"
```

An optional `--expires 2027-12-31` limits the license lifetime. Send the `.htplicense` file to the customer; each approved PC imports the same file from the activation screen. Adding another PC requires issuing a replacement license containing the complete approved Machine ID list.

Issue the optional Wholesale Auction module key with the same protected private key:

```powershell
dotnet run --project .\HammerTrackPro2.LicenseIssuer\HammerTrackPro.LicenseIssuer.csproj -c Release -- issue-module --key "$env:LOCALAPPDATA\HammerTrackPro\LicenseIssuer\hammertrack-license-private.pem" --module Wholesale --customer "Customer or Auction Company" --machines "FIRST-MACHINE-ID,SECOND-MACHINE-ID" --output ".\Wholesale-activation.txt"
```

The generated text is pasted into the Wholesale Auction activation window. The signed activation is saved under `%ProgramData%\HammerTrackPro\Settings`; its signature prevents alteration. It does not contain or expose the private issuer key.

## Demo distribution (0.7.61)

The same installer supports paid and issuer-keyed seven-day demo licenses. Distribute the MSI and customer-specific demo `.htplicense` only, never the issuer or private key. See [Demo issuing and upgrade instructions](DemoLicensing.md). Production code signing remains pending.
