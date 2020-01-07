using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace MBGenerator.Services
{
    public interface ICloudOrders
    {
        void SendImageRequest(int display_x, int display_y, double min_x, double min_y, double max_x, double max_y, int depth, string connectionId);
    }
}
