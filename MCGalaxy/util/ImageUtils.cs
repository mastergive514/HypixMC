﻿/*
    Copyright 2015 MCGalaxy
        
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace MCGalaxy.Util 
{
    public delegate Pixel PixelGet(int x, int y);
    public struct Pixel { public byte A, R, G, B; }

    public abstract class IBitmap2D : IDisposable
    {
        public int Width, Height;
        public PixelGet Get;

        public abstract void Decode(byte[] data);

        public abstract void Resize(int width, int height, bool highQuality);

        public abstract void LockBits();

        public abstract void UnlockBits();

        public abstract void Dispose();

        public static IBitmap2D Create() { return new GDIPlusBitmap(); }
    }

    unsafe sealed class GDIPlusBitmap : IBitmap2D
    {
        Image img;
        Bitmap bmp;
        BitmapData data;
        byte* scan0;
        int stride;

        public override void Decode(byte[] data) {
            Image tmp = Image.FromStream(new MemoryStream(data));
            SetBitmap(tmp);
        }

        public override void Resize(int width, int height, bool hq) {
            Bitmap resized = new Bitmap(width, height);
            // https://photosauce.net/blog/post/image-scaling-with-gdi-part-3-drawimage-and-the-settings-that-affect-it
            using (Graphics g = Graphics.FromImage(resized)) {
                g.InterpolationMode = hq ? InterpolationMode.HighQualityBicubic : InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode   = hq ? PixelOffsetMode.HighQuality          : PixelOffsetMode.None;
                g.DrawImage(bmp, 0, 0, width, height);
            }

            Dispose();
            SetBitmap(resized);
        }

        void SetBitmap(Image src) {
            img = src;
            // although rare, possible src might actually be a Metafile instead
            bmp = (Bitmap)src;

            // NOTE: sometimes Mono will return an invalid bitmap instance that
            //  throws ArgumentNullException when trying to access Width/Height
            Width  = src.Width;
            Height = src.Height;
        }

        public override void Dispose() {
            UnlockBits();
            if (img != null) img.Dispose();

            img = null;
            bmp = null;
        }


        public override void LockBits() {
            bool fastPath = bmp.PixelFormat == PixelFormat.Format32bppRgb
                         || bmp.PixelFormat == PixelFormat.Format32bppArgb
                         || bmp.PixelFormat == PixelFormat.Format24bppRgb;
            if (!fastPath) { Get = GetGenericPixel; return; }
            // We can only use the fast path for 24bpp or 32bpp bitmaps
            
            Rectangle r = new Rectangle(0, 0, bmp.Width, bmp.Height);
            data = bmp.LockBits(r, ImageLockMode.ReadOnly, bmp.PixelFormat);
            scan0 = (byte*)data.Scan0;
            stride = data.Stride;
            
            if (bmp.PixelFormat == PixelFormat.Format24bppRgb) {
                Get = Get24BppPixel;
            } else {
                Get = Get32BppPixel;
            }
        }
        
        Pixel GetGenericPixel(int x, int y) {
            Pixel pixel;
            int argb = bmp.GetPixel(x, y).ToArgb(); // R/G/B properties incur overhead            
            pixel.A = (byte)(argb >> 24);
            pixel.R = (byte)(argb >> 16);
            pixel.G = (byte)(argb >> 8);
            pixel.B = (byte)argb;
            return pixel;
        }
        
        Pixel Get24BppPixel(int x, int y) {
            Pixel pixel;
            byte* ptr = (scan0 + y * stride) + (x * 3);
            pixel.B = ptr[0]; pixel.G = ptr[1]; pixel.R = ptr[2]; pixel.A = 255;
            return pixel;
        }
        
        Pixel Get32BppPixel(int x, int y) {
            Pixel pixel;
            byte* ptr = (scan0 + y * stride) + (x * 4);            
            pixel.B = ptr[0]; pixel.G = ptr[1]; pixel.R = ptr[2]; pixel.A = ptr[3];
            return pixel;
        }

        public override void UnlockBits() {
            if (data != null) bmp.UnlockBits(data);
            data = null;
        }
    }
}
