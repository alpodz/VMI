namespace Interfaces
{
    public interface IQueueService
    {
        void SendToService(IBase Item);
        void SendToService(string Item);
    }
}