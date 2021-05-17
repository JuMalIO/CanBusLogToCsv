namespace CanBusLogToCsv.Models
{
    public class LogMessage
    {
        public static readonly int Size = 16;
        public static readonly int MaxDataLength = 8;

        public uint Tick { get; set; }
        public ushort Id { get; set; }
        public byte Length { get; set; }
        public byte[] Data { get; set; } = new byte[MaxDataLength];
        public byte Empty { get; set; }
    }
}
