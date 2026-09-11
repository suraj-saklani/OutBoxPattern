namespace OutBoxPattern.Model
{
    public class OutBoxEntity
    {
        public int Id { get; set; }
        public EventType EventType { get; set; }
        public int EventId { get; set; }
        public string Payload { get; set; }
        public OutBoxStatus OutBoxStatus { get; set; }
        public DateTime CreatedAt { get; set; }     
        public DateTime? ProcessingStartedAt { get; set; }
        public DateTime? ProcessingCompletedAt { get; set; }

        public int RetryCount { get; set; }
        public string Error { get; set; }
    }

    public enum OutBoxStatus
    {
        Pending,
        Processing,
        Processed,
        Failed
    }

    public enum EventType
    {
        OverHeatingTelemetry
    }
}
