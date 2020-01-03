using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MBGenerator.Models;

namespace MBGenerator.Services
{
    public interface ICloudOrders
    {
        IEnumerable<CloudOrder> GetAllCloudOrders();
        CloudOrder GetCloudOrderById(int Id);
        void SendImageRequest(int display_x, int display_y, double min_x, double min_y, double max_x, double max_y, int depth, string connectionId);
    }
}
