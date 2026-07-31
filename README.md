# api-load-test-example

This is an example project illustrating load testing and associated observability topics.

## Install `grafana.k6`

- On Windows, from an elevated (administrator) command-prompt run:
- Note the second one worked:

```bash
winget install --id GrafanaLabs.k6 -e
winget install grafana.k6
```

## Run load test

```bash
k6 run load-test.js
```

## SQL Queries

```bash
 winpty docker exec -it sqlserver2025 //opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourSecurePassword123!" -C -Q "SELECT session_id, wait_type, wait_time, status, blocking_session_id FROM sys.dm_exec_requests WHERE session_id > 50 ORDER BY wait_time DESC"

```
