# Repository guidance

## Working conventions

- Prefer Bash syntax for terminal commands, examples, scripts, and documentation.
- Use PowerShell or Command Prompt only when a command is specifically Windows-only and has no
  practical Bash equivalent, such as the documented `winget` installation for Grafana k6.
- Preserve the user's uncommitted changes. Do not commit, push, or otherwise publish changes; the
  user manages Git and GitHub.
- Keep edits focused on the requested work and avoid unrelated formatting or generated-file churn.

## Repository map

- `csharp-api/` contains the .NET 10 minimal API, SQL access, database seeding, and OpenTelemetry
  instrumentation.
- `load-test.js` exercises the artificial-delay connection-pool scenario.
- `load-test-scan.js` compares the same customer-order query before and after adding an index.
- `docker-compose.yaml` runs SQL Server, the API, Prometheus, and Grafana.
- `grafana/` and `prometheus/` contain provisioned observability configuration.
- `doc/` contains the detailed experiment and proposed SQL Server-side monitoring guidance.

## Experiment invariants

- Both table-scan comparison runs must call `GET /v1/orders/by-customer` and execute the same SQL.
  The presence of `IX_Orders_CustomerId` must be the only intentional difference between runs.
- Keep `POST /v1/add-index` and `POST /v1/drop-index` idempotent so experiments are repeatable.
- Keep `csharp-api/seed.sql` and `sqlserver-init/seed.sql` synchronized while both copies exist.
- When routes, environment variables, commands, or expected results change, update `README.md`,
  relevant files under `doc/`, and the comments in the matching k6 script together.
- Avoid changing load stages or thresholds silently; they are part of the documented experiment.

## Configuration and security

- Never add real credentials to tracked files. Put local values in `.env` and placeholders in
  `.env.example`; `.env` must remain ignored.
- When introducing a new container image, verify the latest LTS release available at the time of
  implementation and pin that explicit version by default. If the project does not publish an LTS
  channel, use its latest stable, production-ready release instead. Do not use floating tags such
  as `latest` when a versioned tag is available.
- Treat existing image pins as intentional. Do not update them merely because a newer release is
  available; evaluate compatibility and upgrade them in a separate, deliberate change.
- Read application secrets and connection strings through ASP.NET Core configuration rather than
  hard-coding them in C#.
- Keep Prometheus scraping usable without an OTLP collector. OTLP exporters should remain optional
  and enabled only when `OTEL_EXPORTER_OTLP_ENDPOINT` is configured.
- Treat this as a local-only demo. Do not imply that its exposed ports, unauthenticated management
  endpoints, SA account, or `TrustServerCertificate=True` are production-safe.

## Validation

Run checks appropriate to the files changed. The standard validation sequence is:

```bash
dotnet format csharp-api/PoolMonitoringApi.csproj --no-restore
dotnet build csharp-api/PoolMonitoringApi.csproj --no-restore
k6 inspect load-test.js
k6 inspect load-test-scan.js
docker compose --env-file .env.example config --quiet
git diff --check
```

- Do not run the actual k6 load tests automatically: they intentionally generate heavy local load.
- Do not run `docker compose down --volumes` without explicit approval because it deletes local
  database state.
- If Docker validation emits only a sandbox-related warning about the user's Docker config but
  exits successfully, report the warning rather than treating the configuration as invalid.
