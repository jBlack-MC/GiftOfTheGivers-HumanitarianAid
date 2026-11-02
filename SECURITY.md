# Security Policy

## Reporting a vulnerability

Please **do not open a public issue** for security problems. Report privately through GitHub's
*Security → Report a vulnerability* (private vulnerability reporting) on this repository, including:

- what you found and where (URL, file, endpoint),
- steps to reproduce,
- the impact you believe it has.

We aim to acknowledge reports within 5 working days. This is a student project, so there is no bug bounty.

## Scope

In scope: the web application, the Azure Function, the CI/CD workflows and the deployment configuration in
this repository. Out of scope: PayFast, SendGrid, Azure and GitHub themselves.

## Handling secrets

- Never commit credentials. Use `dotnet user-secrets` locally and App Service / pipeline secrets in Azure.
- If a secret is committed, rotate it immediately — removing it from history is not enough.

## Security controls in place

Role-based authorization with admin-granted staff access, ownership checks on donor data, anti-forgery on all
forms, PayFast ITN signature + server validation + amount/merchant checks, per-IP rate limiting on POSTs,
security headers (CSP, frame-ancestors, nosniff), persisted Data Protection keys, NuGet audit with a blocking CI
gate, and an audit log of staff actions. See the engineering audit in `docs/deliverables`.
