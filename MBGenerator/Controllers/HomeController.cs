using System;
using Microsoft.AspNetCore.Mvc;
using MBGenerator.Services;
using MBGenerator.ViewModels;

namespace MBGenerator.Controllers
{
    public class HomeController : Controller
    {

        private ICloudOrders _cloudOrders;
        private IMongoDB _mongoDB;

        public HomeController(ICloudOrders cloudOrders, IMongoDB mongoDB)
        {
            _cloudOrders = cloudOrders;
            _mongoDB = mongoDB;
        }

        public IActionResult Index(MBViewModel mBViewModel = null)
        {
            return View(mBViewModel);
        }


        public FileContentResult GetJpegById(String id)
        {
            var jpeg = _mongoDB.GetJpegById(id);
            return File(jpeg, "image/jpeg");
        }

        public void CalcMB(int display_x, int display_y, double min_x, double max_x, double min_y, double max_y, int depth)
        {
            var mBViewModel = new MBViewModel();
            mBViewModel.Min_x = min_x;
            mBViewModel.Max_x = max_x;
            mBViewModel.Min_y = min_y;
            mBViewModel.Max_y = max_y;
          
            double x_step_size = (max_x - min_x) / 10;
            double y_step_size = (max_y - min_y) / 10;

            double image_x = min_x;
            double image_y = min_y;
            double image_x1 = 0;
            double image_y1 = 0;
            for (var y = 0; y < 10; y++)
            {
                image_y1 = image_y + y_step_size;

                image_x = min_x;
              
                for(var x = 0; x < 10; x++)
                {
                    image_x1 = image_x + x_step_size;
                    //put a request on kafka to build an image from image_x -> image_x1, image_y -> image_y1 at location in display x, y

                    //TODO - here
                    _cloudOrders.SendImageRequest(x, y, image_x, image_y, image_x1, image_y1, depth, "delete me");

                    image_x = image_x1;
                }
                image_y = image_y1;
            }  
        }
    }
}