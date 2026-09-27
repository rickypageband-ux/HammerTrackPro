# Customer user manual

The owner-requested documentation refresh targets the current application version, **0.7.60**, and is delivered in `output/pdf/HammerTrackPro-User-Manual.pdf`. The 36-page manual now includes Search Sold Items and adding another buyer at the recorded price, with a current screenshot on page 22.

The manual covers installation and pairing, main-license activation, auction setup, seller and bidder registration, handheld driver's-license scanning, lot preparation, Retail and Wholesale selling, pass-out entry, corrections, checkout, refunds, printing, settlements, End-of-Night, backups, and troubleshooting. The scanner instructions use the single **Scan Driver's License** action shipped in 0.7.59. Wholesale includes separate activation instructions, price-each / quantity-per-deal / deals examples, and screenshots of standard and pass-out entry. Printer selection, buyer search, and continuous live history are also documented.

## Rebuilding

1. Build the solution with `dotnet build .\HammerTrackPro2.sln`.
2. Run `dotnet run --project tools/manual_capture/ManualCapture.csproj` on Windows to render the actual application forms off-screen. The harness uses a new disposable database under `tmp/manual-capture-data`, fictional buyers/sellers, an in-memory sample printer, and a capture-only module-entitlement stub. It does not activate the installed application, import a license, print, or edit production data. Machine identifiers are replaced with a training label before rendering.
3. Run `python tools/generate_user_manual.py` with ReportLab and Pillow available. The PDF version is read from the application project rather than maintained separately.
4. Render every PDF page and inspect page breaks, screenshots, tables, contents, and links. Check extracted text for obsolete directions and orphaned sections. Run the existing `UserManualServiceTests`.
5. Build the release installer through the existing release process. `HammerTrackPro2.Installer/Package.wxs` already includes this PDF beside the executable, which is where Help / User Manual resolves it. Regenerating this PDF does not replace an already-built MSI or an installed copy.

The capture project is documentation tooling only and is not included in the solution or installed application. No application behavior, schema, migrations, or licensing rules change in this refresh. Physical scanner and printer operation still require checks with the customer's hardware; screenshots are not a hardware acceptance test.

## Verification

- Baseline solution build: passed, zero warnings and errors.
- Existing offline-manual tests: 2 passed.
- Screenshots regenerated from current source with fictional data.
- Final PDF checked through text extraction and rendered-page inspection.

The customer's existing README and CurrentSprint edits were preserved.
