# Customer user manual

The owner-requested documentation refresh targets application version **0.7.60** and is delivered in `output/pdf/HammerTrackPro-User-Manual.pdf`. It includes Search Sold Items and adding another buyer at the recorded price, plus the owner-supplied software license agreement without pricing in chapter 26.

The September 30 agreement addition uses `Hammer_Track_Pro_Installment_Payment_and_Software_License_Agreement.pdf`, not the installer's different agreement. Its manual-only transcription and source hash are in `manual-license-agreement.json`. Per owner instruction, the purchase/installment section and schedule, additional-license fee wording, annual update price/fee wording, custom-work rate/cost wording, and payment reference in Entire Agreement are omitted. The remaining 13 sections are renumbered, and purchaser details and acceptance/signature fields are retained. The installer agreement and all other manual pricing/calculation examples are unchanged. No application or installer rebuild is required for this standalone PDF update.

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
