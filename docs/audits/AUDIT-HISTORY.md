# Audit History

| Audit | Type | Date | Commit | Weighted score | Reported score | Maturity | Critical open | High open | Findings (fixed / partial / open) | Report |
| --- | --- | --- | --- | ---: | ---: | --- | ---: | ---: | --- | --- |
| AUD-2026-11-01 | Baseline | 2026-11-02 | `efa2b02` | 4.07 | 3.9* | Functional prototype | 3 | 8 | 0 / 0 / 39 | [Baseline](../deliverables/08-professional-engineering-audit.html) |
| AUD-2026-11-01-R1 | Remediation review | 2026-11-02 | `921e5d8` | 7.03 | 6.5 (capped) | Beta | 0 | 1 | 32 / 7 / 7 | [Addendum](../deliverables/08-professional-engineering-audit-addendum.html) |

\* The baseline report normalised by 100, but its weights sum to 105. The register stores the corrected value.

Test evidence over time:

| Audit | Tests | Line coverage (hand-written code) | Vulnerable packages | Migrations on empty SQL Server |
| --- | ---: | --- | --- | --- |
| AUD-2026-11-01 | 19 | Not measured | 1 (Pkcs, High + Moderate) | Not tested (later found to fail, F-21) |
| AUD-2026-11-01-R1 | 98 | 67.9% (branches 52.5%) | 0 | Pass (upgrade, rollback, re-apply) |

**Next audit must cover:** the Android app in `app/` (added after AUD-2026-11-01-R1 started), first CI run results, and the open items in the register.
