# Contributing

## Branch naming

- `feature/<short-description>` — new functionality
- `fix/<short-description>` — bug fixes

## Workflow

- Keep branches short-lived. Pull/rebase `main` before starting new work and again before opening a PR, to minimize merge conflicts.
- Keep PRs small enough to review in one sitting. Prefer several small PRs over one large one.
- Make sure `dotnet build` and `dotnet test` pass locally before opening a PR.

## Branch protection (main)

Once the Azure Pipelines build is reliably green, add branch protection on `main` in Azure Repos:

- Require a pull request before merging (no direct pushes).
- Require the pipeline build to pass before merging.

Don't turn this on while the pipeline is still flaky/broken — enforcing a check that isn't passing just blocks everyone from merging.
