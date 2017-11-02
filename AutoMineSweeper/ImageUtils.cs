using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace AutoMineSweeper
{
    public static class ImageUtils
    {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int memcmp(IntPtr b1, IntPtr b2, long count);

        public static bool CompareMemCmp(BitmapInfo bmi, Bitmap b2)
        {
            if ((bmi == null) != (b2 == null)) return false;
            if (bmi.size != b2.Size) return false;

            var bd2 = b2.LockBits(new Rectangle(new Point(0, 0), b2.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                IntPtr bd1scan0 = bmi.bds;
                IntPtr bd2scan0 = bd2.Scan0;

                return memcmp(bd1scan0, bd2scan0, bmi.len) == 0;
            }
            finally
            {
                b2.UnlockBits(bd2);
            }
        }

        public static Point Center(this Rectangle rect)
        {
            return new Point(rect.Left + rect.Width / 2,
                             rect.Top + rect.Height / 2);
        }
    }

    public class BitmapInfo
    {
        public Size size;
        public IntPtr bds;
        public int len;
        public BitmapInfo(Bitmap bm)
        {
            var bd = bm.LockBits(new Rectangle(new Point(0, 0), bm.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            size = bm.Size;
            bds = bd.Scan0;
            len = bd.Stride * bm.Height;
        }
    }
}
