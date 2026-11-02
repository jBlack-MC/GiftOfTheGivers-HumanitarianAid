---
number: 04
title: Design Brief
subtitle: Visual identity, components, content rules and accessibility requirements, documented from the implemented design system.
version: 2.0
status: Current — reflects the implemented UI
date: 2026-11-02
---

## 1. Design goals

1. **Trust first.** Donors give money to a cause they cannot see. Every screen must look credible and say
   exactly what will happen. In particular, payment state is always explicit: *Awaiting payment*, *Paid*, *Failed*.
2. **Urgency without manipulation.** Strong calls to action ("Donate Now") but no fake counters, invented
   statistics or dark patterns.
3. **Mobile first for the public.** Most visitors arrive from social media on phones. The donate form must work
   one-handed on a 390 px screen.
4. **Calm, efficient staff tools.** Dense tables, clear filters, confirmations for destructive actions, nothing
   decorative.

![Home page](assets/screenshots/home.png)

## 2. Brand tokens

All tokens live in `GiftOfTheGivers/wwwroot/css/site.css` (`:root`) and override Bootstrap 5 variables. Do not
hard-code colours in views; use the tokens or Bootstrap utilities mapped to them.

<div class="swatches">
<div class="swatch"><div style="background:#155E9E"></div><p><strong>Primary</strong><br>#155E9E · links, primary buttons, headers</p></div>
<div class="swatch"><div style="background:#0F4676"></div><p><strong>Primary dark</strong><br>#0F4676 · hover, emphasis</p></div>
<div class="swatch"><div style="background:#0A2138"></div><p><strong>Navy</strong><br>#0A2138 · footer, hero gradient</p></div>
<div class="swatch"><div style="background:#E8833A"></div><p><strong>Accent</strong><br>#E8833A · Donate Now, highlights</p></div>
<div class="swatch"><div style="background:#E7F0F8"></div><p><strong>Primary subtle</strong><br>#E7F0F8 · tints, table heads</p></div>
<div class="swatch"><div style="background:#1B2430"></div><p><strong>Ink</strong><br>#1B2430 · headings</p></div>
<div class="swatch"><div style="background:#3C4655"></div><p><strong>Body</strong><br>#3C4655 · body text</p></div>
<div class="swatch"><div style="background:#6B7684"></div><p><strong>Muted</strong><br>#6B7684 · secondary text</p></div>
<div class="swatch"><div style="background:#198754"></div><p><strong>Success</strong><br>Bootstrap green · Paid, approved</p></div>
<div class="swatch"><div style="background:#FFC107"></div><p><strong>Warning</strong><br>Bootstrap amber · Awaiting payment, test mode</p></div>
</div>

| Token | Value |
| --- | --- |
| Font | **Inter** (Google Fonts) with a system fallback stack; weights 400–800 |
| Radius | 0.375 / 0.55 / 0.85 / 1.1 rem (sm / default / lg / xl) |
| Focus ring | Primary at 25% opacity (Bootstrap focus ring) |
| Breakpoints | Bootstrap defaults; custom rules at ≥ 768 px |

**Contrast (measured, WCAG 2.x formula):** primary #155E9E on white 6.72:1, body #3C4655 on white 9.55:1, muted
#6B7684 on white 4.62:1 — all pass AA for normal text. **White text on the accent #E8833A is 2.71:1 and fails AA
even for large text (3:1).** Ink text #1B2430 on the accent is 5.77:1 and passes, so the *Donate Now* / accent buttons now use
ink text, with a lighter hover (#F09A5C, 7.07:1). This fixed design debt item D-03 during remediation.

## 3. Typography

| Use | Style |
| --- | --- |
| Page hero titles | `display-3/4`, uppercase, bold, white on primary band |
| Section titles | `h2`–`h3`, ink colour |
| Body | 1rem Inter, body colour, line-height ~1.6 |
| Data and references | Monospace (`font-monospace`, `<code>`) for transaction references |
| Money | Always through `Money.Format` / `Money.Zar`: "R 1,234.50", "$ 75.00", "€ 10.00". Never culture-dependent `ToString("C")` |

## 4. Layout

- **Public pages:** full-width coloured hero band, then content centred in `container` with max ~8 columns for
  forms (`col-lg-6` for the donate form), and a dark navy footer with three columns.
- **Staff pages:** `container my-5` with page title, lead sentence, filter pills or tabs, a summary card, then a
  responsive table (`table-responsive`) and the pager.
- **Grid:** Bootstrap 12 columns. Cards use `shadow-sm` (staff) and `shadow-lg` (primary public forms).

## 5. Components

| Component | Use | Rules |
| --- | --- | --- |
| Primary button | Main action per screen | One per form. Labels state the result: "CONTINUE TO PAYMENT", not "Submit" |
| Donate Now (accent) | Global navigation | Always visible; links to `/Home/Donate`; ink text on orange for contrast |
| Payment status badge | Anywhere a donation is shown | `_PaymentStatusBadge`: Paid (green), Awaiting payment (amber), Failed (red). Never infer status from other fields |
| Flash messages | After POST-redirect-GET | `_StatusMessages` in the layout, `role="status"`, dismissible |
| Validation summary | Every form | `asp-validation-summary="All"` with `role="alert"`; hidden when empty (`.validation-summary-valid`) |
| Consent checkbox | Every form collecting personal data | Links to the privacy notice; required on the server |
| Pager | Staff lists | `_Pager`: Previous/Next, "Page x of y · n records" |
| Confirmation | Destructive staff actions | `data-confirm="…"` on the button (handled in `site.js`). Never inline JavaScript with user data |
| Impact stats | Home and About | `ImpactStats` view component (band or cards); always labelled "Live figures from this system" |
| Demo badge | Footer, certificates | Shown while `Organisation:IsDemo` is true |

## 6. Content rules

1. **No invented numbers.** Statistics come from the database or are removed. Narrative examples are labelled
   *illustrative*.
2. **No borrowed identity.** Contact details, registration numbers and PBO references come from configuration.
   Placeholders show "Not configured" instead of fake values.
3. **Say what happens next.** Before payment: "You will be sent to PayFast… No real money is taken (test mode)."
   After payment: the actual status.
4. **Plain language** at about a Grade 8 reading level; sentence case for body text; uppercase only for hero titles.
5. **Monthly pledge, not "recurring donation"**, until automatic billing exists.

## 7. States

| State | Pattern | Example |
| --- | --- | --- |
| Empty | Neutral `alert-info` with what to do next | "No donations match this filter." |
| Loading | Server-rendered pages; no spinners needed. Long actions (PDF) open as downloads | — |
| Success | Green flash, or a receipt with a green check | Paid receipt |
| Pending | Amber hourglass and "Awaiting payment" | Receipt before the ITN arrives |
| Error (validation) | Summary at top + field messages | Donate form |
| Error (server) | `/Home/Error` with request id (production) | — |
| Forbidden | Identity Access Denied page | Donor opening a staff page |

## 8. Accessibility requirements (WCAG 2.2 AA target)

| Requirement | Implementation |
| --- | --- |
| Labels for every input | `asp-for` labels; radio groups wrapped in `fieldset` + `legend` |
| Errors announced | Validation summary has `role="alert"`; flash messages have `role="status"` |
| Keyboard | Native controls only; visible focus ring from Bootstrap |
| Icons | Decorative icons have `aria-hidden="true"`; icon-only buttons have visually hidden text |
| Live region | Rand equivalent on the donate form uses `aria-live="polite"` |
| Motion | Intro animation honours `prefers-reduced-motion` and has a Skip button |
| Tables | `scope="col"` headers on staff tables |
| Language | `lang="en"` |

Not yet verified with assistive technology or automated tools. An axe and Playwright suite is in the
implementation plan (T-2.5).

## 9. Screens

<div class="figrow">
<figure><img class="shot" src="assets/screenshots/projects.png" alt="Projects"><figcaption>Relief projects with funding progress (rand)</figcaption></figure>
<figure><img class="shot" src="assets/screenshots/privacy.png" alt="Privacy"><figcaption>Privacy notice with draft banner</figcaption></figure>
</div>

![Intro animation (first visit per session; skippable; reduced-motion aware)](assets/screenshots/intro.png)

## 10. Design debt

| ID | Item | Proposal |
| --- | --- | --- |
| D-01 | Inline styles and scripts in several views | Move to `site.css` / `site.js` (also enables a strict CSP) |
| D-02 | Hero bands use `bg-primary` with large uppercase headings on every page | Reduce on staff pages to save vertical space |
| D-03 | ~~White text on the accent button (2.71:1)~~ | **Fixed:** ink text (5.77:1), lighter hover (7.07:1) |
| D-04 | Staff tables on phones scroll horizontally | Card layout for narrow screens |
| D-05 | Fonts and icons come from third-party CDNs | Self-host (privacy, reliability) |
