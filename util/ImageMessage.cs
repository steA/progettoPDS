using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace pds2.Shared.Messages
{
    //public delegate void  ImageMessageDelegate(ImageMessage msg);
    [Serializable]
    public class ImageMessage : SendableObj<ImageMessage>
    {
        public Size img_size;
        public int style;
        public byte[] bitmap;
    }
}
