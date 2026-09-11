using Dapper;
using Microsoft.Data.SqlClient;

namespace OutBoxPattern
{
    public class Migrations
    {
        private readonly DbConnectionFactory dbConnectionFactory;
        public Migrations(DbConnectionFactory dbConnectionFactory)
        {
            this.dbConnectionFactory = dbConnectionFactory;
        }

        public async Task CreateTables()
        {
            await CreateTelemetryTable();
            await CreateOutboxTable();
        }
        private SqlConnection GetSqlConnection() => dbConnectionFactory.CreateConnection();
        

        public async Task CreateTelemetryTable()
        {
            using var connection = GetSqlConnection();
            const string sql = """
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.tables
                    WHERE name = 'Telemetry'
                )
                BEGIN
                    CREATE TABLE Telemetry
                    (
                        Id INT IDENTITY(1,1) NOT NULL
                            CONSTRAINT PK_Telemetry PRIMARY KEY,

                        MachineId INT NOT NULL,

                        Temperature FLOAT NOT NULL,

                        Pressure FLOAT NOT NULL,

                        Vibration FLOAT NOT NULL,

                        Speed FLOAT NOT NULL,

                        RecordedAt DATETIME2 NOT NULL,

                        CreatedAt DATETIME2 NOT NULL
                            CONSTRAINT DF_Telemetry_CreatedAt
                            DEFAULT SYSUTCDATETIME()
                    );

                    CREATE INDEX IX_Telemetry_MachineId
                    ON Telemetry (MachineId);

                    CREATE INDEX IX_Telemetry_RecordedAt
                    ON Telemetry (RecordedAt);
                END;
            """;
                
            await connection.ExecuteAsync(sql);
        }
        public async Task CreateOutboxTable()
        {
            using var connection = GetSqlConnection();
            const string sql = """
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.tables
                    WHERE name = 'OutboxMessages'
                )
                BEGIN
                    CREATE TABLE OutboxMessages
                    (
                        Id INT IDENTITY(1,1) NOT NULL
                            CONSTRAINT PK_OutboxMessages PRIMARY KEY,

                        EventId INT NOT NULL,

                        OutBoxStatus INT NOT NULL,
            
                        EventType NVARCHAR(200) NOT NULL,

                        Payload NVARCHAR(MAX) NOT NULL,

                        CreatedAt DATETIME2 NOT NULL
                            CONSTRAINT DF_OutboxMessages_CreatedAt
                            DEFAULT SYSUTCDATETIME(),

                        ProcessingStartedAt DATETIME2 NULL,
            
                        ProcessingCompletedAt DATETIME2 NULL,

                        RetryCount INT NOT NULL
                            CONSTRAINT DF_OutboxMessages_RetryCount
                            DEFAULT 0,

                        Error NVARCHAR(MAX) NULL,

                        CONSTRAINT UQ_OutboxMessages_EventId
                            UNIQUE (EventId)
                    );

                END;
            """;

            await connection.ExecuteAsync(sql);
        }
    }
}
