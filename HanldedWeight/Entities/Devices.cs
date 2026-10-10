namespace HandledWeight.Entities
{
    public class Devices
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int BaudRate { get; set; }
        public string Parity { get; set; } = string.Empty;
        public int DataBits { get; set; }
        public string Port { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public string? ControlFlow { get; set; }
        public string? StopBits { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
