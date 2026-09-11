# Product Backlog

This is the approved release sequence. Do not pull work forward from a later release without an explicit product decision. Functional architecture, database integrity, business logic, and tests take priority over UI styling until the feature set is complete.

## Version 0.5

- [x] Consignors
- [x] Auction Setup
- [x] Premiums

## Version 0.6

- [x] Cashier
- [x] Payments
- [x] Invoice printing

## Version 0.7

- [x] Settlements (calculation, ledgered payouts, and printable settlement statement)
- [ ] Check printing
- [ ] Pickup verification
- [x] Optional licensed Wholesale Auction module with deal-based pass-out entry (owner-approved early module slice)
- [x] Continuous Retail/Wholesale live-sale history with audited transaction correction (owner-approved operational usability slice)
- [x] Per-workstation printer selection with saved preference and test page (owner-approved operational usability slice)

## Version 0.8

- [ ] Reports
- [ ] Financial closeout
- [ ] Daily reconciliation

## Version 0.9

- [ ] General barcode scanning (a separately approved PDF417 driver's-license registration slice shipped early for camera and HID handheld scanners in 0.7.28-0.7.30)
- [ ] Receipt-printer-specific formatting and hardware integration (basic Windows printer selection shipped early in 0.7.55)
- [ ] Touchscreen mode

## Version 1.0

- [ ] Production release
- [x] Installer (WiX MSI; production code signing remains)
- [ ] User manual
- [ ] Backup/restore
- [ ] Multi-workstation testing

## Scope rule

Version 0.6 Cashier, Payments, and Invoice printing are implemented. The separately approved Version 0.7 settlement slice is implemented; check-stock printing and pickup verification remain pending. Reports and other later-release items remain out of scope until separately approved.
