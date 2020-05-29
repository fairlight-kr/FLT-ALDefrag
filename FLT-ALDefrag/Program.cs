using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using ImageMagick;

namespace FLT_ALDefrag
{
    class Program
    {
        public class FragmentDataInfo
        {
            public int F1 { get; set; }
            public int F2 { get; set; }
            public int F3 { get; set; }
        };

        public static void Main(string[] args)
        {
            List<string> pngList = new List<string>();
            List<string> objList = new List<string>();

            List<string> pngSkinList = new List<string>();
            List<string> objSkinList = new List<string>();

            string meshPath = String.Empty;
            string pngPath = String.Empty;

            if ((args.Length % 2) != 0)
            {
                Console.WriteLine("Please drag-and-drop set of [scrambled PNG + mesh OBJ] to this program.");
                Console.WriteLine("Press any key to exit.");
                char input = Console.ReadKey().KeyChar;
                return;
            }
            else
            {
                foreach (string param in args)
                {
                    if (Path.GetExtension(param) == ".png")
                    {
                        if (Path.GetFileName(param).Contains("_n"))
                        {
                            pngSkinList.Add(param);
                        }
                        else
                        {
                            pngList.Add(param);
                        }
                    }
                    else if (Path.GetExtension(param) == ".obj")
                    {
                        if (Path.GetFileName(param).Contains("_n"))
                        {
                            objSkinList.Add(param);
                        }
                        else
                        {
                            objList.Add(param);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Please insert valid file. (.png + .obj)");
                        Console.WriteLine("Press any key to exit.");
                        char input = Console.ReadKey().KeyChar;
                        return;
                    }
                }
            }
            
            pngList.Sort();
            objList.Sort();
            pngSkinList.Sort();
            objSkinList.Sort();

            for (int i = 0; i < pngList.Count; i++)
            {
                Console.WriteLine("Processing: " + Environment.NewLine +
                    "PNG=" + Path.GetFileName(pngList[i]) + Environment.NewLine +
                    "OBJ=" + Path.GetFileName(objList[i]));

                Process(pngList[i], objList[i]);
            }

            for (int i = 0; i < pngSkinList.Count; i++)
            {
                Console.WriteLine("Processing: " + Environment.NewLine +
                    "PNG=" + Path.GetFileName(pngSkinList[i]) + Environment.NewLine +
                    "OBJ=" + Path.GetFileName(objSkinList[i]));

                Process(pngSkinList[i], objSkinList[i]);
            }
        }

        private static void Process(string png, string mesh)
        {
            var rebuildCoord = new List<Point>();
            var rebuildMeta = new List<FragmentDataInfo>();
            var cropCoord = new List<PointF>();

            string[] txtRaw = File.ReadAllLines(mesh);
            int nLine = 0;
            int widthLow = 0;
            int widthHigh = 0;
            int heightLow = 0;
            int heightHigh = 0;

            for (nLine = 0; nLine < txtRaw.Length; nLine++)
            {
                // Start of coord data
                if (txtRaw[nLine][0] == 'v')
                {
                    break;
                }
            }

            while (true)
            {
                int i = 0;

                string[] tmp = txtRaw[nLine + i + 0].Split(' ');

                Point rd;
                PointF cd;

                if (tmp[0][0] == '#') break;

                rd = new Point(Math.Abs(Convert.ToInt32(Convert.ToDouble(tmp[1]))), Convert.ToInt32(Convert.ToDouble(tmp[2])));
                rebuildCoord.Add(rd);

                if (widthLow > Convert.ToInt32(Convert.ToDouble(tmp[1]))) widthLow = Convert.ToInt32(Convert.ToDouble(tmp[1]));
                if (widthHigh < rd.X) widthHigh = rd.X;
                if (heightLow > rd.Y) heightLow = rd.Y;
                if (heightHigh < rd.Y) heightHigh = rd.Y;

                tmp = txtRaw[nLine + i + 1].Split(' ');
                cd = new PointF(float.Parse(tmp[1]), 1.0f - float.Parse(tmp[2]));
                cropCoord.Add(cd);

                nLine += 2;
            }

            for (; nLine < txtRaw.Length; nLine++)
            {
                // Start of meta data
                if (txtRaw[nLine][0] == 'f')
                {
                    break;
                }
            }

            while (true)
            {
                int i = 0;

                string[] tmp = txtRaw[nLine + i + 0].Split(' ', '/');

                FragmentDataInfo fdi = new FragmentDataInfo();

                if (tmp[0] == String.Empty || nLine > txtRaw.Length) break;

                fdi.F1 = Math.Abs(Convert.ToInt32(Convert.ToDouble(tmp[1])));
                fdi.F2 = Math.Abs(Convert.ToInt32(Convert.ToDouble(tmp[3])));
                fdi.F3 = Math.Abs(Convert.ToInt32(Convert.ToDouble(tmp[5])));

                rebuildMeta.Add(fdi);

                nLine++;
            }

            MagickImage mi = new MagickImage(png);

            int baseWidth = mi.Width;
            int baseHeight = mi.Height;
            int padding = 1024;

            MagickImage ri = new MagickImage(MagickColor.FromRgb(255, 255, 255), 4096, 4096);

            ri.Resize(4096, 4096);
            ri.Transparent(MagickColors.White);

            for (int i = 0; i < rebuildMeta.Count; i++)
            {
                Point[] rebuildInfo = {
                    new Point(rebuildCoord[rebuildMeta[i].F1 - 1].X,
                              rebuildCoord[rebuildMeta[i].F1 - 1].Y),
                    new Point(rebuildCoord[rebuildMeta[i].F2 - 1].X,
                              rebuildCoord[rebuildMeta[i].F2 - 1].Y),
                    new Point(rebuildCoord[rebuildMeta[i].F3 - 1].X,
                              rebuildCoord[rebuildMeta[i].F3 - 1].Y)
                };

                PointF[] cropInfo = {
                    new PointF(baseWidth * cropCoord[rebuildMeta[i].F1 - 1].X, baseHeight * cropCoord[rebuildMeta[i].F1 - 1].Y),
                    new PointF(baseWidth * cropCoord[rebuildMeta[i].F2 - 1].X, baseHeight * cropCoord[rebuildMeta[i].F2 - 1].Y),
                    new PointF(baseWidth * cropCoord[rebuildMeta[i].F3 - 1].X, baseHeight * cropCoord[rebuildMeta[i].F3 - 1].Y)
                };

                int cropStartX = (int)Math.Round(Math.Min(Math.Min(cropInfo[0].X, cropInfo[1].X), cropInfo[2].X));
                int cropStartY = (int)Math.Round(Math.Min(Math.Min(cropInfo[0].Y, cropInfo[1].Y), cropInfo[2].Y));
                int cropEndX = (int)Math.Round(Math.Max(Math.Max(cropInfo[0].X, cropInfo[1].X), cropInfo[2].X));
                int cropEndY = (int)Math.Round(Math.Max(Math.Max(cropInfo[0].Y, cropInfo[1].Y), cropInfo[2].Y));

                int rebuildStartX = Math.Min(Math.Min(rebuildInfo[0].X, rebuildInfo[1].X), rebuildInfo[2].X);
                int rebuildStartY = baseWidth - Math.Min(Math.Min(rebuildInfo[0].Y, rebuildInfo[1].Y), rebuildInfo[2].Y)
                    - (cropEndY - cropStartY);

                IMagickImage clone = mi.Clone();
                MagickGeometry mg = new MagickGeometry
                {
                    X = cropStartX,
                    Y = cropStartY,
                    Width = cropEndX - cropStartX,
                    Height = cropEndY - cropStartY
                };

                clone.Crop(mg, Gravity.Northwest);
                ri.Composite(clone, rebuildStartX + padding, rebuildStartY + padding, CompositeOperator.Copy, Channels.All);

                Console.Write(((double)i / (double)rebuildMeta.Count * 100.0d).ToString("0.00"));
                Console.Write("\r");
            }

            //ri.Transparent(MagickColor.FromRgb(0, 0, 0));
            ri.BackgroundColor = MagickColors.Transparent;
            ri.Trim();
            ri.Write(Path.GetFileName(png) + "_d.png", MagickFormat.Png32);
            mi.Dispose();
            ri.Dispose();

            Console.Write("100.0");
            Console.WriteLine(Environment.NewLine +
                "Output: " + Environment.NewLine +
                "PNG=" + Path.GetFileName(png) + "_d.png" + Environment.NewLine);

            return;
        }
    }
}