# Local development and deployment

See the [README](../README.md) for the live demo, feature overview and quick start; [verification](verification.md) records measured test results and hosted checks.

## Run with the .NET SDK

Install Git and the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), then run:

```sh
git clone https://github.com/batuhansacofficial/UnitConverter.git
cd UnitConverter
dotnet run --project UnitConverter --launch-profile http
```

Open [http://localhost:5206](http://localhost:5206), or [Swagger UI](http://localhost:5206/swagger). The launch profile sets the port and development environment. No secrets, database migrations or frontend package installation are required.

## Run with Docker

From the repository root, with a Docker engine configured for Linux containers:

```sh
docker build -t unitwise .
docker run --rm -p 127.0.0.1:5206:8080 unitwise
```

The same local URLs apply. The image uses the ASP.NET Core 10 runtime and runs as a non-root user. Stop the foreground container with Ctrl+C.

## Tests and coverage

Build and run the suite from the repository root:

```sh
dotnet restore UnitConverter.slnx
dotnet build UnitConverter.slnx --configuration Release --no-restore
dotnet test --solution UnitConverter.slnx --configuration Release --no-build
```

To collect the same TRX and Cobertura reports as CI, replace the final command with:

```sh
dotnet test --solution UnitConverter.slnx --configuration Release --no-build --results-directory TestResults -- --report-trx --report-trx-filename tests.trx --coverlet --coverlet-output-format cobertura --coverlet-include "[UnitConverter]*"
```

The repository selects Microsoft Testing Platform in [global.json](../global.json). Tests use xUnit v3 and Coverlet.MTP. Reports appear under `TestResults/`; coverage filenames match `coverage.cobertura*.xml`. This directory is ignored by Git. Coverage includes measured application C# and Razor code; JavaScript, CSS and browser interaction are outside that measurement.

To run the suite and collect coverage inside the Linux SDK image:

```sh
docker build --target test -t unitwise-tests .
```

This target runs the tests during the build and stores reports at `/testresults` inside the test image. The separate runtime-image build does not run the test target; CI runs tests before building and checking the runtime image.

The recorded verification used Linux containers because Windows Application Control blocked native assembly loading on the development machine. See the [environment boundary](verification.md#environment-boundary) for the exact limitation. That observation applies to the recorded environment; it does not establish a general Windows incompatibility.

## Continuous integration

[GitHub Actions](https://github.com/batuhansacofficial/UnitConverter/actions/workflows/ci.yml) restores and builds the solution, runs tests with coverage, uploads TRX and Cobertura reports, then builds and smoke-tests the runtime image. The smoke check requests the home page and CSS/JavaScript assets and verifies that the container user is not root.

Each run publishes measured line, branch and conversion-domain coverage in its job summary. Download the `test-results` artifact for the underlying reports. Artifacts are retained for 14 days. The README coverage badge is the documented measurement snapshot; its CI badge reflects the workflow's status.

## Render deployment

The demo uses a free Docker web service in Frankfurt in the Hobby workspace. Render's **Auto-Deploy → After CI Checks Pass** setting watches the connected GitHub repository's `main` branch. To release a change:

1. Push or merge the intended revision to `main`.
2. GitHub Actions builds the solution, runs tests and coverage, and checks the runtime container. Render waits for the commit's CI checks, then builds and deploys it automatically. Failed or missing checks block the automatic deploy.
3. Confirm that the matching commit reaches **Live** in Render. Check `/health`, load the interface and Swagger UI, and submit `20 °C → °F` to verify a result of `68`. An API request converting Kelvin to Celsius with `value: "-1"` must return HTTP 400 validation.

The service uses Render's native GitHub integration; the workflow needs no deploy hook or Render API key. A passing CI run starts deployment; it does not by itself confirm that the new release is live. See [hosted verification](verification.md#hosted-verification) for recorded checks.

For a new service, choose **Git Provider** and select this repository through the connected GitHub account. A **Public Git Repository** URL alone supports manual deployment only. An existing service can connect its source through **Settings → Build → Source → Edit → Git Provider**; keep the branch as `main`, the runtime as Docker, and Auto-Deploy as **After CI Checks Pass**.

[render.yaml](../render.yaml) also specifies `autoDeployTrigger: checksPass`. It is an optional provisioning Blueprint rather than the source of the existing service's dashboard settings. The Blueprint specifies an HTTP `/health` check; the existing service uses Render's default TCP readiness check. The Docker image listens on the port configured by `ASPNETCORE_HTTP_PORTS`, set to `10000` in the manifest.

Documentation changes also deploy by default. For a documentation-only commit that should skip deployment, include `[skip render]` in its commit message; GitHub CI still runs. The service watches only `main`, so feature-branch pushes do not update the public demo. [Render's deployment documentation](https://render.com/docs/deploys#integrating-with-ci) describes the check handling and skip options.

Free services can sleep after inactivity, so the first request may wait for startup. [Render's free-service documentation](https://render.com/docs/free#spinning-down-on-idle) describes that behavior.
