using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace MongooseSoftware.Robotics.RobotLib.Video
{
    public static class YuyvUtilities
    {
        public static Bitmap ConvertYUYVToColorBitmap(int width, int height, byte[] data)
        {
            var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            int x = 0, y = 0;
            for (int i = 0; i < data.Length; i+=4)
            {
                byte y1 = data[i];
                byte u = data[i + 1];
                byte y2 = data[i + 2];
                byte v = data[i + 3];

                bitmap.SetPixel(x++, y, ConvertYuv2ToRgb(y1, u, v));
                bitmap.SetPixel(x++, y, ConvertYuv2ToRgb(y2, u, v));

                if (x == width)
                {
                    x = 0;
                    y++;
                }
            }

            return bitmap;
        }
        
        static Color ConvertYuv2ToRgb(byte y, byte u, byte v)
        {
            var blue =  Clamp(1.164 * (y - 16.0)                      + 2.018 * (u - 128.0));
            var green = Clamp(1.164 * (y - 16.0) - 0.813 * (v - 128.0) - 0.391 * (u - 128.0));
            var red =   Clamp(1.164 * (y - 16.0) + 1.596 * (v - 128.0));
            return Color.FromArgb(red, green, blue);
        }

        static byte Clamp(double f)
        {
            f = Math.Round(f);
            if (f < 0) return 0;
            if (f > 255) return 255;
            return (byte)f;
        }

    }
}
