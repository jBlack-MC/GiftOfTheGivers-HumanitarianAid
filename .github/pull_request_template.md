## What and why

<!-- What does this change do, and why is it needed? Link the issue or audit finding (e.g. F-03). -->

## How it was tested

- [ ] `dotnet test GiftOfTheGivers.slnx` passes
- [ ] New/changed behaviour has a test (integration test for money, roles or personal data)
- [ ] Manually checked in the browser (describe below)

## Checklist

- [ ] Entity changes include a migration; `has-pending-model-changes` reports none
- [ ] Data-changing staff actions write an audit entry
- [ ] No secrets, real personal data or invented statistics
- [ ] README / docs / `docs/audits/findings-register.json` updated if relevant
