# Coding Standards

## Principles

Favor correctness and operator safety over cleverness. Live-auction actions are time-sensitive and data mutations can affect money, inventory, and personal information. Keep changes small, explicit, reviewable, and compatible with existing data unless an approved migration says otherwise.

## Architecture

- Maintain `Forms -> IAuctionService -> IDataStore -> SQLite` dependency direction.
- Forms own layout, input collection, keyboard behavior, confirmations, and presentation only.
- `AuctionService` owns business validation and workflow rules.
- `SqliteDataStore` owns SQL, transactions, migrations, backups, and materialization.
- Models should remain simple data shapes. Do not introduce UI or database dependencies into them.
- Add a service/interface operation instead of down-casting or reaching through layers.

## C# conventions

- Use .NET 8, nullable reference types, and implicit usings as configured.
- Use PascalCase for types, methods, properties, and public members; `_camelCase` for private fields; camelCase for parameters and locals.
- Prefer file-scoped namespaces, explicit access modifiers, `sealed` classes when not designed for inheritance, and guard clauses.
- Validate arguments at public boundaries and give operators actionable error messages.
- Dispose connections, commands, readers, transactions, forms, and other disposable objects deterministically.
- Avoid broad exception swallowing. Catch only where adding context, recovery, or an appropriate user-facing boundary.
- Keep methods focused. Extract domain logic from event handlers when it becomes independently testable.
- Do not add a dependency when the platform or existing packages solve the need adequately; document the reason for any new package.

## Persistence and money

- Parameterize every SQL value; never concatenate user input into SQL.
- Use transactions for multi-step state changes and include the audit event in the same transaction.
- Preserve foreign-key enforcement and existing data by default.
- Use `decimal` for money. Match the current invariant-culture text representation unless an approved migration changes it.
- Write persisted timestamps as UTC ISO-8601 (`O`/round-trip format); localize only at display boundaries.
- Make schema changes idempotent and versioned. Document forward migration, compatibility, verification, and rollback/restore.
- Do not log driver’s-license values or unnecessary contact details.

## Windows Forms UI

- Keep essential live-auction paths keyboard accessible and do not break documented hotkeys.
- Require confirmation for destructive or high-risk actions; preserve the existing high-price verification behavior unless requirements change.
- Keep control updates on the UI thread. If I/O becomes asynchronous, provide progress and prevent duplicate submission.
- Use clear status feedback for success and failure without revealing raw SQL or sensitive data.
- Account for common DPI scaling and window resizing when changing layouts.

## Testing and verification

The repository has an xUnit test project. Every feature must add focused service/view-model and data-store integration coverage using disposable databases where persistence is involved.

Minimum verification for code changes:

1. `dotnet build .\HammerTrackPro2.sln` succeeds without new warnings.
2. The affected workflow works with a newly created disposable database.
3. Persistence changes also work against a representative pre-change database copy.
4. Relevant audit rows, foreign keys, backup behavior, and undo/close semantics are checked.
5. No generated output, database, secrets, or workstation-specific paths are committed.

## Documentation and commits

- Update README for shipped operator/developer behavior.
- Update DatabaseDesign for schema, data-location, migration, or integrity changes.
- Update Roadmap and CurrentSprint when scope or status changes; distinguish observed facts from proposals.
- Use concise commits that describe one logical change. Do not mix generated formatting or unrelated cleanup into feature/fix work.
