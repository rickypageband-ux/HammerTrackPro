# Current Sprint

## Status

No sprint dates or owner are present. The additive Consignor/Auction/Lot ownership slice is implemented through schema version 6. The remaining section is a **proposed planning baseline**, not a commitment.

Sprint 1 is function-first. Do not add UI styling in this sprint; limit presentation work to functional CRUD and setup wiring while architecture, database integrity, business rules, and tests are stabilized.

## Proposed sprint goal

Establish confidence in the v0.3.2 data path and define the supported deployment model before extending cashier or multi-workstation functionality.

## Proposed scope and acceptance criteria

### 1. Establish automated test infrastructure

- Add a .NET test project using disposable temporary databases.
- Cover bidder/lot validation, standard sale, pass-out multiple buyers, close lot, and latest-sale undo.
- Cover fresh schema creation and legacy sales-table upgrade without modifying production data.
- Acceptance: tests are repeatable locally and the solution build/test commands are documented.

### 2. Resolve fresh-schema/index consistency

- Decide whether `IX_Sales_LotNumber` is required for all version-3 databases.
- If approved, add it through an idempotent migration rather than an untracked startup edit.
- Acceptance: both new and upgraded databases report the intended schema and preserve sample data.

### 3. Define database deployment and recovery

- Decide whether the supported v1 topology is local database only or a named class of network share.
- Document required folder permissions, backup ownership, restore steps, and behavior during disconnect/contention.
- Run representative concurrent-workstation and interruption tests if network use is approved.
- Acceptance: README/DatabaseDesign state one supported topology and a tested recovery procedure; unsupported configurations are explicit.

### 4. Triage renumbering and historical-record behavior

- Decide whether bidder/lot numbers can change after sales reference them.
- Add regression coverage for the approved policy; do not lose or orphan sales.
- Acceptance: service/UI behavior, foreign-key behavior, and documentation agree.

## Explicitly out of scope for this proposed sprint

- Cashier/payment implementation
- Taxes, buyer premiums, receipts, settlement, or accounting exports
- New database engine or server component
- Authentication/roles
- Installer, broad UI redesign, or unrelated feature work

## Decisions needed from the maintainer

1. Approve or replace the proposed goal and scope; assign owner and dates.
2. Choose the supported database topology and concurrency target.
3. Approve a test framework.
4. Define the policy for renumbering/deleting records with sale history.
5. Rank cashier workflow, multi-auction scoping, privacy controls, and packaging for the next roadmap phase.

## Definition of done

- Acceptance criteria are met and reviewed.
- `dotnet build` and all tests pass.
- Manual live-auction smoke checks use disposable data.
- Relevant README and `docs/` pages are updated in the same change.
- No live database, backup, personal data, generated output, or workstation-specific configuration is committed.
