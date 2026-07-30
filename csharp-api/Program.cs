using Microsoft.Data.SqlClient;
using Dapper;

const string serviceName = "pool-monitoring-api";
var builder = WebApplication.CreateBuilder(args);

var connString = "Server=sqlserver2025;Database=master;User Id=sa;Password=YourSecurePassword123!;Max Pool Size=150;TrustServerCertificate=True;";

builder.AddAppTelemetryV2(serviceName);

var app = builder.Build();
app.UseOpenTelemetryPrometheusScrapingEndpoint();

// Endpoint Route 1
app.MapGet("/v1/data-endpoint", async () =>
{
    using var connection = new SqlConnection(connString);
    var result = await connection.QueryAsync<int>("WAITFOR DELAY '00:00:00.100'; SELECT 1;");
    return Results.Ok(new { status = "Success", data = result });
});

// Endpoint Route 2
app.MapGet("/v1/admin-report", async () =>
{
    using (var connection = new SqlConnection(connString))
    {
        var result = await connection.QueryAsync<int>("WAITFOR DELAY '00:00:00.050'; SELECT 2;");
        return Results.Ok(new { status = "Admin Success", data = result });
    }
});

app.Run();
