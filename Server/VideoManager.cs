using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using pds2.Shared;
using pds2.Shared.Messages;
using System.Threading;
using System.Windows;
using System.Collections.Concurrent;
using System.IO;


namespace Server
{
    public class VideoManager
    {
        private int captureStyle;
        private volatile bool _shouldStop;
        private int width_s, height_s;
        private Point point_s;
        Pen pen;
        protected static IntPtr m_HBitmap;
        private BlockingCollection<ImageMessage> videoQueue;

        Cursor arrow = Cursors.Arrow;
        Point p;

        public const Int32 CURSOR_SHOWING = 0x00000001;

        public VideoManager(int style, BlockingCollection<ImageMessage> videoQueue)
        {
            captureStyle = style;
            pen = new Pen(Brushes.Red);
            pen.Width = 2.0F;
            setStart();
            this.videoQueue = videoQueue;
        }

        public void setStyle(int style)
        {
            captureStyle = style;
        }

        public void UpdateValuesOfScreen(int p_x, int p_y, int w, int h)
        {
            point_s = new Point(p_x, p_y);
            width_s = w;
            height_s = h;
        }

        public void DoWork()
        {
            MemoryStream ms = new MemoryStream();
            MemoryStream msOld = new MemoryStream();
            
            //compressione dell'immagine
            ImageCodecInfo jgpEncoder = GetEncoder(ImageFormat.Jpeg);
            // Crea un oggetto di tipo Encoder per definire la qualità (e spazio) dell'immagine
            System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
            // Crea un oggetto EncoderParameter per gestire i parametri di compressione immagine
            EncoderParameters myEncoderParameters = new EncoderParameters(1);
            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 45L);
            myEncoderParameters.Param[0] = myEncoderParameter;

            while (!_shouldStop)
            {
                
                p = Cursor.Position;
                Graphics gfxScreenshot = null;
                Bitmap bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                                 Screen.PrimaryScreen.Bounds.Height,
                                 PixelFormat.Format32bppArgb);

                //catturo tutto lo schermo
                if (captureStyle == 2)
                {
                    // Create a graphics object from the bitmap. 
                    gfxScreenshot = Graphics.FromImage(bmpScreenshot);
                    // Take the screenshot from the upper left corner to the right bottom corner. 
                    try
                    {
                        gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                                    Screen.PrimaryScreen.Bounds.Y,
                                                    0,
                                                    0,
                                                    Screen.PrimaryScreen.Bounds.Size,
                                                    CopyPixelOperation.SourceCopy);
                    }
                    catch
                    {
                        continue;
                    }
                    creaBitmapCursore(ref gfxScreenshot, p.X, p.Y);
                }

                //catturo la finestra attiva in questo istante
                if (captureStyle == 1)
                {
                    RECT rct = GetForegroundWindow();

                    Size sz = new Size(Math.Abs(rct.Left - rct.Right), Math.Abs(rct.Bottom - rct.Top));
   
                    if (rct.Bottom > Screen.PrimaryScreen.WorkingArea.Bottom)
                        sz.Height = Math.Abs(Screen.PrimaryScreen.WorkingArea.Bottom - rct.Top);

                    // Create a graphics object from the bitmap. 
                    gfxScreenshot = Graphics.FromImage(bmpScreenshot);
                    // Take the screenshot from the upper left corner to the right bottom corner. 
                    gfxScreenshot.CopyFromScreen(rct.Left,
                                                rct.Top,
                                                0,
                                                0,
                                                sz,
                                                CopyPixelOperation.SourceCopy);

                    gfxScreenshot = Graphics.FromImage(bmpScreenshot);
                    if (SetCursor(ref p, rct.Left, rct.Top, sz.Width, sz.Height))
                        creaBitmapCursore(ref gfxScreenshot, p.X, p.Y);
                }

                //catturo una porzione dello schermo
                if (captureStyle == 3)
                { 
                    Size sz = new Size(width_s, height_s);
                    // Create a graphics object from the bitmap. 
                    gfxScreenshot = Graphics.FromImage(bmpScreenshot);
                    // Take the screenshot from the upper left corner to the right bottom corner. 
                    gfxScreenshot.CopyFromScreen(point_s.X,
                                                point_s.Y,
                                                0,
                                                0,
                                                sz,
                                                CopyPixelOperation.SourceCopy);

                    if (SetCursor(ref p, point_s.X, point_s.Y, sz.Width, sz.Height))

                    creaBitmapCursore(ref gfxScreenshot, p.X, p.Y);
                }


                gfxScreenshot.Dispose();
                IntPtr hBitmap = bmpScreenshot.GetHbitmap();

                try
                {

                ms.SetLength(0);
                bmpScreenshot.Save(ms, jgpEncoder, myEncoderParameters);

                if (!imageIsEqual(ms, msOld))
                {
                    ImageMessage msg = new ImageMessage();
                    msg.bitmap = ms.ToArray();
                    videoQueue.Add(msg);
                    ms.Position = msOld.Position = 0;
                    ms.CopyTo(msOld);
                    
                }
                }
                finally 
                {
                    WIN32_API.DeleteObject(hBitmap);
                }
                Thread.Sleep(30);
            }

        }

        private bool SetCursor(ref Point cursor, int x_new, int y_new, int w_new, int h_new)
        {

            if ((cursor.X >= x_new) && (cursor.Y >= y_new) && (cursor.X <= (x_new + w_new)) && (cursor.Y <= (y_new + h_new)))
            {
                cursor.X = cursor.X - x_new;
                cursor.Y = cursor.Y - y_new;
                return true;
            }
            return false;
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        //
        private void creaBitmapCursore(ref Graphics g, int cursorX, int cursorY)
        {

            Rectangle rCursor = new Rectangle(cursorX, cursorY, arrow.Size.Width, arrow.Size.Height);
            arrow.Draw(g, rCursor);
           
        }


     /*   public void setStop()
        {
            _shouldStop = true;
        }*/

      
        public void setStart()
        {
            _shouldStop = false;
        }


        public static RECT GetForegroundWindow()
        {
            IntPtr hwnd = WIN32_API.GetForegroundWindow();
            RECT rct;
            WIN32_API.GetWindowRect(hwnd, out rct);
            return rct;
        }

        public bool imageIsEqual(MemoryStream m1, MemoryStream m2)
        {

            if (m1.Length != m2.Length)
                return false;

            m1.Position = 0;
            m2.Position = 0;

            var m1Array = m1.ToArray();
            var m2Array = m2.ToArray();

            return m1Array.SequenceEqual(m2Array);
                
          
      
        }

    }
}
