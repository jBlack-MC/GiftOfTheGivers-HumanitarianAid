# Contributing

## Branch naming

- `feature/<short-description>` — new functionality
- `fix/<short-description>` — bug fixes

## Workflow

- Keep branches short-lived. Rebase on `main` before opening a PR.
- Keep PRs small enough to review in one sitting, and fill in the PR template.
- Before opening a PR, run locally:

  ```bash
  dotnet build GiftOfTheGivers.slnx
  dotnet test GiftOfTheGivers.slnx
  dotnet ef migrations has-pending-model-changes --project GiftOfTheGivers/GiftOfTheGivers.csproj --startup-project GiftOfTheGivers/GiftOfTheGivers.csproj
  ```

- Changing an entity? Add a migration in the same PR and say in the PR whether it needs a data backfill.
- Never edit a migration that has been applied to a shared database; add a new one instead.
- Write commit messages that describe the change ("Count only verified donations in project totals", not "good").

## Definition of done

- Behaviour covered by a test (integration test for anything touching money, roles or personal data).
- Staff actions that change data write an audit entry (`IAuditService.Record`).
- No secrets, real personal data or fabricated statistics in the change.
- README / docs updated when behaviour or configuration changes.

## Branch protection (main)

Require a pull request with one approval and the **CI** workflow (`Build, test, audit` and
`Apply migrations to SQL Server`) to pass before merging.

## Audits

Engineering audits are tracked in [docs/audits](docs/audits/README.md). When you fix an audit finding, reference
its ID (e.g. `F-03`) in the PR and update its status in `docs/audits/findings-register.json`.
