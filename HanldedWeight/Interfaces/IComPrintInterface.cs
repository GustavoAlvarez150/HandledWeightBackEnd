namespace HandledWeight.Interfaces
{
    public interface IComPrintInterface
    {
        Task<bool> PrintTextAsync(string portName, string text, int baudRate = 9600);
        Task<bool> SendBytesAsync(string portName, byte[] bytes, int baudRate = 9600);
        string[] GetAvailablePorts();
        string GenerateReceiptText(Entities.Receipts receipt, bool useEscPos = true);
    }
}
