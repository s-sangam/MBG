using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MBGenerator.Services;
using Microsoft.AspNetCore.SignalR;

namespace MBGenerator
{
    public class ImageMarshallingHub : Hub
    {
        private ICloudOrders _cloudOrders;
        public ImageMarshallingHub(ICloudOrders cloudOrders)
        {
            _cloudOrders = cloudOrders;
        }
        public void CalcMB(int display_x, int display_y, double min_x, double max_x, double min_y, double max_y, int depth)
        {


            double x_step_size = (max_x - min_x) / 10;
            double y_step_size = (max_y - min_y) / 10;

            double image_x = min_x;
            double image_y = min_y;
            double image_x1 = 0;
            double image_y1 = 0;
            string connectionId = Context.ConnectionId;
            for (var y = 0; y < 10; y++)
            {
                image_y1 = image_y + y_step_size;

                image_x = min_x;

                for (var x = 0; x < 10; x++)
                {
                    image_x1 = image_x + x_step_size;
                    //put a request on kafka to build an image from image_x -> image_x1, image_y -> image_y1 at location in display x, y

                    //TODO - here
                    _cloudOrders.SendImageRequest(x, y, image_x, image_y, image_x1, image_y1, depth, connectionId);

                    image_x = image_x1;
                }
                image_y = image_y1;
            }
        }
    }
}