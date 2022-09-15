using Basler.Pylon;
using OpenCV.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bonsai.Pylon
{
    public class PylonDataFrame
    {
        public PylonDataFrame(IplImage image, IGrabResult grabResult)
        {
            Image = image;
            GrabResult = grabResult;
        }

        public IplImage Image { get; private set; }

        public IGrabResult GrabResult { get; private set; }
    }
}
