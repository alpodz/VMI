namespace Interfaces
{
    public interface IInventory
    {
        void ExecuteMaint(IExchange email);
        void ExecuteWorkAgainstMailMessage(uint msg);
    }
}