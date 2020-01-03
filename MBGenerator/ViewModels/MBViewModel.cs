
namespace MBGenerator.ViewModels
{
    public class MBViewModel
    {
        public MBViewModel()
        {
            Min_x = -2;
            Max_x = 2;
            Min_y = -2;
            Max_y = 2;
            Depth = 1;
        }

        public double Min_x { get; set; }
        public double Max_x { get; set; }
        public double Min_y { get; set; }
        public double Max_y { get; set; }
        public int Depth { get; set; }

    }
}
