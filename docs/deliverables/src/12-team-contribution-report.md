---
number: 12
title: Team Contribution Report
subtitle: Roles, responsibilities and evidence of each member's contribution, Parts 1–3.
version: 0.9
status: DRAFT — names, student numbers and roles to be completed and agreed by every member before submission
date: 2026-11-02
toc: false
---

## Members and roles

Each member confirms their own row. The git identities are taken from the repository history (section *Evidence*).

| Member (name, student number) | Git identity | Role(s) | Main responsibilities and deliverables |
| --- | --- | --- | --- |
| *[Full name, ST number]* | `jBlack-MC` | *[e.g. Lead developer]* | *[e.g. web app features (donation flow, tax-certificate PDF, ERD alignment), most pull requests]* |
| Clarity Masuku, ST10438928 | `MPCwGamer`, `ST10438928@rcconnect.edu.za` | *[e.g. DevOps / deployment lead]* | *[e.g. initial Azure Pipelines set-up (commit 2277bdf), Azure DevOps organisation and Artifacts feed]* |
| *[Full name, ST number]* | `Katlego-lab001` | *[e.g. Tester / documentation]* | *[e.g. co-authored 15 commits; testing, user manual]* |
| *[Full name, ST number]* | *[identity]* | *[role]* | *[responsibilities]* |
| *[Full name, ST number]* | *[identity]* | *[role]* | *[responsibilities]* |

Delete unused rows. The brief allows at most 5 members.

## Evidence (from `git log --all`, 2026-08-16 → 2026-11-02)

| Measure | Value |
| --- | --- |
| Commits (all branches) | 102, of which 36 are merge commits |
| Pull requests | 15 merged into `main` (#1–#18) |
| Commits authored by `jBlack-MC` | 86 (plus 3 from a second account, `MPCwGamer`) |
| Commits authored by Clarity Masuku | 1 (2277bdf, *Set up CI with Azure Pipelines*) |
| `Co-Authored-By` trailers | Katlego-lab001 × 15, Clarity Masuku (MPCwGamer) × 13, jBlack-MC × 16 |
| Automated agent commits | `copilot-swe-agent[bot]` × 11 (PRs #8, #10, #17); see the AI declaration in deliverable 09 |
| Other | 1 commit as `Student <student@example.com>` (5724267): the author must be identified |

Reproduce these numbers with:

```bash
git shortlog -sne --all
git log --all --format='%(trailers:key=Co-Authored-By,valueonly)' | sort | uniq -c
gh pr list --state all
```

## Contribution by Part 3 task

Tick who did each task, so the marker can link work to people.

| Part 3 task | Member(s) |
| --- | --- |
| A.1 Unit tests (`DonationServiceTests`, existing unit suites) | *[ ]* |
| A.2 Integration tests (`EmployeeWorkflowTests`, `DonorPortalTests`, `TaxCertificateFunctionTests`) | *[ ]* |
| A.3 Load and stress tests (`GiftOfTheGivers.LoadTests`, caching mitigation) | *[ ]* |
| A.4 Selenium UI tests (`GiftOfTheGivers.UiTests`) | *[ ]* |
| B Pipeline with tests and coverage (`azure-pipelines.yml`, `pipelines/templates`) | *[ ]* |
| C.1–C.3 Continuous deployment (web app, function, SQL) and Azure set-up | *[ ]* |
| D User manual (deliverable 10) | *[ ]* |
| Azure deployment guide (deliverable 11) and this report | *[ ]* |

## Observations for the next iteration

- Most commit messages are short (*good*, *test*, *updates*). Use descriptive messages such as *Add Selenium test for anonymous USD donation* so each member's work can be read from the history.
- Work done in pairs should be committed by the person who typed it, with a `Co-Authored-By:` line for the partner.

**Signed (each member):** ____________________ ____________________ ____________________ ____________________ ____________________
