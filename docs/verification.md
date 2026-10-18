# Verification

Verified on **19 October 2026**, using the .NET 10 Linux SDK/runtime containers and Chromium.

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

The muted text color was darkened slightly after a palette contrast check. Against the mint result background, its calculated contrast increased from 4.42:1 to 4.55:1, meeting the 4.5:1 target for ordinary text. Other checked foreground/background pairs also exceeded 4.5:1; this check is limited to the inspected palette pairs.

Manual browser checks complement the tests; they do not establish complete accessibility conformance or cross-browser coverage.

## Screenshots

- [Desktop interface](screenshots/desktop.jpg)
- [Hosted interface](screenshots/live-demo.png)
- [Phone interface](screenshots/mobile.jpg)
- [Phone result and history](screenshots/mobile-result.jpg)
- [Swagger request](screenshots/swagger-request.jpg)
- [Swagger server response](screenshots/swagger-response.jpg)

Desktop, phone and Swagger screenshots show the actual local application; the hosted-interface screenshot shows the public Render service. History entries came from successful form submissions; the Swagger response came from the running server.

## Environment boundary

Windows Application Control blocked native application/test assembly loading with `0x800711C7`. Final runtime and coverage verification therefore used Linux containers. No machine policy was changed. The Docker run is a real application/runtime check; it is separate from hosted deployment verification.

## Hosted verification

- [GitHub Actions run 39172154459](https://github.com/batuhansacofficial/UnitConverter/actions/runs/39172154459) completed successfully, including Release build, 85 tests, coverage collection/report upload and non-root runtime container smoke checks. Downloaded CI reports independently confirmed 85 passed tests and the same line/branch coverage figures as the local Linux run.
- Render deployment `dep-dbalgjajnfac7386ba20` reached **live** on the free `unitwise` service (`srv-dbalgiqjnfac7386b8rg`), deploying source commit `15342d6bd66b00fac720f6837b5de61a9a0a14ee` after [CI run 39172800630](https://github.com/batuhansacofficial/UnitConverter/actions/runs/39172800630) passed. Application code is unchanged from `1ce7844c3ad837a5f1c16164b0f2976bc1b75ff5`, which includes the result-text contrast correction.
- [Public demo](https://unitwise.onrender.com): the page loaded correctly, and a real form submission converted 20 °C to 68 °F and added a browser history entry. The hosted screenshot was captured from this hostname. The requested hostname was assigned to a replacement service because renaming the previous service retained its original hostname. The previous service is suspended.
- Public HTTP checks: `/health` returned `healthy`; `/api/convert` returned `68` for 20 °C to °F; a Kelvin input of −1 returned HTTP 400 validation; `/swagger` and `/css/site.css` returned HTTP 200.
- The service uses Render's **free** instance in Frankfurt. Automatic deployment is configured as **After CI Checks Pass** for the connected GitHub repository's `main` branch (`autoDeployTrigger: checksPass`). It uses the platform's default TCP readiness check; the optional `render.yaml` Blueprint specifies the available `/health` HTTP endpoint as its health-check path.
- Future `main` commits deploy automatically after successful checks. Documentation-only commits can opt out with `[skip render]` while still running CI. Free services can sleep after inactivity; [Render documents the wake-up delay](https://render.com/docs/free#spinning-down-on-idle).

## Automatic deployment verification

On **19 October 2026**, the service's source was connected through the GitHub provider and Auto-Deploy was set to **After CI Checks Pass**. The workspace remained **Hobby** and the instance remained **free**.

- Pushed commit [`6c9f1aa`](https://github.com/batuhansacofficial/UnitConverter/commit/6c9f1aa1fdb596669bd71a6d7a2f18784b006ad1) after connecting the provider. Render had no deployment for that commit while CI was running.
- [GitHub Actions run 39178038155](https://github.com/batuhansacofficial/UnitConverter/actions/runs/39178038155) passed. Its build/test/package job completed at **02:49:22 UTC+03:00**.
- Render automatically created deployment `dep-dbalm1flk1mc73d0ulng` for the same commit at **02:49:25 UTC+03:00**, with trigger **`new_commit`**, and reached **live** at **02:50:13 UTC+03:00**. This release did not require a manual deployment.
- After the automatic release, the public page, Swagger and OpenAPI returned HTTP 200; `/health` returned `healthy`; the API returned `68` for 20 °C to °F and HTTP 400 for `-1` Kelvin.

The native integration requires no Render deploy secret in GitHub Actions. The existing unconditional CI job remains the release gate; the application code and test suite were unchanged during this check.
