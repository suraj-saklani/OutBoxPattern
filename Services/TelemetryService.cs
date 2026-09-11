using Dapper;
using Microsoft.AspNetCore.Connections;
using Microsoft.Data.SqlClient;
using OutBoxPattern.Model;

namespace OutBoxPattern.Services
{
    public class TelemetryService
    {
        private readonly DbConnectionFactory dbConnectionFactory;

        public TelemetryService( DbConnectionFactory dbConnectionFactory)
        {
            this.dbConnectionFactory = dbConnectionFactory;
        }
        private SqlConnection GetSqlConnection() => dbConnectionFactory.CreateConnection();

        public async Task InsertTelemetryAsync(Telemetry telemetry)
        {
            using var connection = GetSqlConnection();
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();
            var telemetrySQL = @"
                INSERT INTO Telemetry (MachineId, Temperature, Pressure, Vibration, Speed, RecordedAt, CreatedAt) 
                OUTPUT INSERTED.Id
                VALUES (@MachineId, @Temperature, @Pressure, @Vibration, @Speed, @RecordedAt, SYSUTCDATETIME());
            ";
            telemetry.Id = await connection.ExecuteScalarAsync<int>(telemetrySQL, telemetry, transaction);

            if (telemetry.Temperature > 90)
            {
                var payload = new
                {
                    Temperature = telemetry.Temperature,
                    Pressure = telemetry.Pressure,
                    Vibration = telemetry.Vibration,
                    Speed = telemetry.Speed
                };
                var outboxEvent = new OutBoxEntity
                {
                    EventType = EventType.OverHeatingTelemetry,
                    Payload = System.Text.Json.JsonSerializer.Serialize(payload),
                    CreatedAt = DateTime.UtcNow,
                    EventId = telemetry.Id,
                    OutBoxStatus = OutBoxStatus.Pending,
                };

                var outBoxSQL = @"
                INSERT INTO OutboxMessages (EventType, Payload, CreatedAt, EventId, OutBoxStatus)
                VALUES (@EventType, @Payload, SYSUTCDATETIME(), @EventId, @OutBoxStatus);
                ";
                await connection.ExecuteAsync(outBoxSQL, outboxEvent, transaction);
            }

            await transaction.CommitAsync();
        }
    }
}
