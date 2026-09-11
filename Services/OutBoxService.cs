using Dapper;
using OutBoxPattern.Model;

namespace OutBoxPattern.Services
{
    public class OutBoxService : BackgroundService
    {
        private readonly DbConnectionFactory dbConnectionFactory;

        public OutBoxService(DbConnectionFactory dbConnectionFactory)
        {
            this.dbConnectionFactory = dbConnectionFactory;
        }
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessOutboxAsync(stoppingToken);

                await Task.Delay(
                    TimeSpan.FromSeconds(10),
                    stoppingToken);
            }
        }

        private async Task ProcessOutboxAsync(CancellationToken stoppingToken)
        {

            using var connection = dbConnectionFactory.CreateConnection();
            connection.Open();
            using var updateToProcessingTransaction = connection.BeginTransaction();
            IEnumerable<OutBoxEntity> outboxMessages = new List<OutBoxEntity>();
            try
            {
                var getOutboxMessagesSQL = @"
                    SELECT TOP 10 *
                    FROM OutboxMessages  WITH (UPDLOCK, READPAST)
                    WHERE (OutBoxStatus = @OutBoxStatus OR (OutBoxStatus = @ProcessingOutBoxStatus AND ProcessingStartedAt  < @CutoffTime )) AND RetryCount < 5
                    ORDER BY CreatedAt ASC;
                ";
                outboxMessages = await connection.QueryAsync<OutBoxEntity>(getOutboxMessagesSQL, new { OutBoxStatus = OutBoxStatus.Pending, ProcessingOutBoxStatus = OutBoxStatus.Processing, CutoffTime = DateTime.UtcNow.AddMinutes(-5) }, updateToProcessingTransaction);

                var updateTelToProcessing = @"
                    UPDATE OutboxMessages
                    SET OutBoxStatus = @OutBoxStatus, ProcessingStartedAt = @ProcessingStartedAt, Error = NULL, RetryCount = RetryCount + 1
                    WHERE Id = @Id;
                ";
                await connection.ExecuteAsync(
                    updateTelToProcessing,
                    outboxMessages.Select(x => new
                    {
                        x.Id,
                        OutBoxStatus = OutBoxStatus.Processing,
                        ProcessingStartedAt = DateTime.UtcNow
                    }),
                    updateToProcessingTransaction);
                await updateToProcessingTransaction.CommitAsync();
            }
            catch (Exception)
            {

                await updateToProcessingTransaction.RollbackAsync();
                throw;
            }

            
            //send to event service
            foreach (var message in outboxMessages)
            {
                try
                {
                    var successed = await HandleEvent(message.Payload);

                    message.OutBoxStatus = successed
                        ? OutBoxStatus.Processed
                        : OutBoxStatus.Failed;

                    message.Error = successed
                        ? null
                        : "Failed to process event";
                }
                catch (Exception ex)
                {
                    message.OutBoxStatus = OutBoxStatus.Failed;
                    message.Error = ex.Message;
                }

            }

            var updateSql = @"
                UPDATE OutboxMessages
                SET OutBoxStatus = @OutBoxStatus, ProcessingCompletedAt = @ProcessingCompletedAt, Error = @Error
                WHERE Id = @Id;
                ";

            await connection.ExecuteAsync(
                updateSql,
                outboxMessages.Select(x => new
                {
                    x.Id,
                    x.OutBoxStatus,
                    ProcessingCompletedAt = DateTime.UtcNow,
                    x.Error
                })
            );
            
           
        }

        public async Task<bool> HandleEvent(string payload)
        {
            await Task.Delay(1000); // Simulate async operation
            Console.WriteLine(payload);
            return true;
        }
    }
}
