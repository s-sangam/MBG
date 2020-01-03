using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MBGenerator.Services
{
    public interface IMongoDB
    {
        public byte[] GetJpegById(string id);


    }
}
