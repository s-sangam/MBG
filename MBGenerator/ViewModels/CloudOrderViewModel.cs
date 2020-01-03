using MBGenerator.Models;
using System.Collections.Generic;

namespace MBGenerator.ViewModels
{
    public class CloudOrderViewModel
    {
        public IEnumerable<CloudOrder> CloudOrders { get; set; }
        public string WelcomeText { get; set; }
    }
}
