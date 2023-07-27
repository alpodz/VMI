using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDB
{
    internal class AzureQueue : Interfaces.IQueueService
    {
        public AzureQueue() 
        {
            
        }

        public void SendToService(IBase Item)
        {
            throw new NotImplementedException();
        }
    }
}
