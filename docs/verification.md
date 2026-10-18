# Verification

Verified on **19 October 2026**, using the .NET 10 Linux SDK/runtime containers and the in-app Chromium browser.

## Build and tests

| Check | Result |
| --- | --- |
| Release build | Passed; zero warnings and zero errors |
| Domain tests | 57 passed |
| HTTP integration tests | 28 passed |
| Total | **85 passed, 0 failed, 0 skipped** |
| Runtime user | UID 1654; non-root |
| Runtime page, static assets, health and API | Responded successfully on the local Linux container |

The test suite covers reference conversions, every catalog unit pair in round trips, temperature offsets/absolute zero, invalid input and unit IDs, culture independence, numeric overflow/underflow, scientific presentation, API contracts, native Razor form posts, swap behavior and antiforgery rejection. The runner is Microsoft Testing Platform with xUnit v3 and Coverlet.MTP.

## Measured coverage

| Scope | Measured result |
| --- | --- |
| Application lines, including measured Razor/API/page code | **95.5%** — 276 / 289 |
| Application branches | **90.0%** — 135 / 150 |
| Conversion domain lines | **97.14%** — 136 / 140 unique source lines |

These figures are a snapshot of the passing local Linux run. The repository coverage badge represents this snapshot. GitHub Actions publishes the measurement for each run in its job summary and downloadable `test-results` artifact. JavaScript, CSS and browser interaction are not included in the Cobertura figures. Coverage measures executed code, not correctness by itself.

## Browser checks

- Converted 12.5 m to 41.010499 ft; swapping units recalculated 12.5 ft as 3.81 m.
- Converted 20 °C to 68 °F; copied the result and read back `68 °F` from the clipboard.
- A value below absolute zero produced a linked inline error, focused error summary, and no stale result.
- Editing a field marked the prior result as stale and disabled its copy action.
- Cleared history, created real entries, restored a conversion, and verified that history remained available across page loads.
- Checked the native help dialog, close action and focus behavior.
- Checked the default desktop viewport, 1487 × 1058 desktop layout and 390 × 844 phone layout. No horizontal page overflow was present; the active category remained visible after navigation. Phone swap targets measured 44 × 44 CSS pixels.
- Executed `POST /api/convert` through Swagger UI and received **HTTP 200**, including the expected exact numeric strings and formula. The console showed no application errors during the API check.

Visual review inspected heading/control typography, the off-white/white/mint palette, category icons and selection state, form alignment/spacing, result hierarchy, the open history list and mobile reflow. The main copy and category order match the interface specification. Empty history, exact formulas, approximation/rounding notes and validation are deliberate functional states. Long result symbols wrap separately from numeric values to avoid clipping.

The muted text color was darkened slightly after a palette contrast check. Against the mint result background, its calculated contrast increased from 4.42:1 to 4.55:1, meeting the 4.5:1 target for ordinary text. Other checked foreground/background pairs also exceeded 4.5:1; this check is limited to the inspected palette pairs.

Manual browser checks complement the tests; they do not establish complete accessibility conformance or cross-browser coverage.

## Screenshots

- [Desktop interface](screenshots/desktop.jpg)
- [Hosted interface](screenshots/live-demo.jpg)
- [Phone interface](screenshots/mobile.jpg)
- [Phone result and history](screenshots/mobile-result.jpg)
- [Swagger request](screenshots/swagger-request.jpg)
- [Swagger server response](screenshots/swagger-response.jpg)

Desktop, phone and Swagger screenshots show the actual local application; the hosted-interface screenshot shows the public Render service. History entries came from successful form submissions; the Swagger response came from the running server.

## Environment boundary

Windows Application Control blocked native application/test assembly loading with `0x800711C7`. Final runtime and coverage verification therefore used Linux containers. No machine policy was changed. The Docker run is a real application/runtime check; it is separate from hosted deployment verification.

## Hosted verification

- [GitHub Actions run 39172154459](https://github.com/batuhansacofficial/UnitConverter/actions/runs/39172154459) completed successfully, including Release build, 85 tests, coverage collection/report upload and non-root runtime container smoke checks. Downloaded CI reports independently confirmed 85 passed tests and the same line/branch coverage figures as the local Linux run.
- Initial Render deployment `dep-dbakeq0m7kps73974a40` reached **live**, deploying application source commit `f680feebaee574218a2870c2a698d9d775ab1612`. Subsequent code releases use the same CI and hosted checks.
- [Public demo](https://unitwise-batuhansac.onrender.com): the page loaded correctly, and a real form submission converted 20 °C to 68 °F. No browser application errors were observed during this hosted form check.
- Public HTTP checks: `/health` returned `healthy`; `/api/convert` returned `68` for 20 °C to °F; a Kelvin input of −1 returned HTTP 400 validation; `/swagger` and `/css/site.css` returned HTTP 200.
- The service uses Render's **free** plan in Frankfurt. It currently uses manual deployment and the platform's default TCP readiness check. The `/health` HTTP endpoint is available; the optional `render.yaml` Blueprint specifies it as the health-check path and configures deployment after passing CI checks.
- Documentation-only commits do not redeploy the application. Code releases are deployed manually after CI passes. Free services can sleep after inactivity; [Render documents the wake-up delay](https://render.com/docs/free#spinning-down-on-idle).
