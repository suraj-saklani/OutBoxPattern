namespace OutBoxPattern.Model
{
    public class Telemetry
    {
        public int Id { get; set; }

        public int MachineId { get; set; }

        public double Temperature { get; set; }

        public double Pressure { get; set; }

        public double Vibration { get; set; }

        public double Speed { get; set; }

        public DateTime RecordedAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
