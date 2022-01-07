using System;
using System.IO;
using System.Diagnostics;

namespace ImageProcess
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("");

            Console.WriteLine(string.Format("{0} - [v{1}.{2}.{3}.{4}]",
                Utility.pAssembly,
                Utility.pVersion.Major, Utility.pVersion.Minor,
                Utility.pVersion.Build, Utility.pVersion.Revision));

            if (args.Length == 1)
            {
                try
                {
                    // Get list of files in the specific directory from command line
                    string[] files =
                        Directory.GetFiles(args[0], "*.jpg", SearchOption.AllDirectories);

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    // Iterate all the files in the returned set
                    foreach (string src in files)
                    {
                        try
                        {
                            string dst = Path.ChangeExtension(src, "png");
                            Console.WriteLine($"File: {src}");
                            Console.WriteLine($"Dest: {dst}");

                            using (BitmapWin bitmap = new BitmapWin(src))
                            using (FileStream file = new FileStream(dst, FileMode.Create))
                            {
                                // Do all required transformations
                                bitmap.SetResampleMode(ResampleMode.BiCubic);
                                bitmap.Resize(25);
                                bitmap.Rotate180();
                                bitmap.Grayscale_8Bpp();

                                // Re-encode the new transformed image and output it
                                byte[] data = bitmap.EncodePng();
                                file.Write(data, 0, data.Length);
                                file.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                        }
                    }

                    stopwatch.Stop();
                    Console.WriteLine($"Time: {stopwatch.ElapsedMilliseconds}ms");

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine(@"Simple image processing example using BitmapWin() library.");
                Console.WriteLine(@"Take a load of JPEGs, transform and sling them out as PNGs.");
                Console.WriteLine(@"Syntax : ImageProcess <jpeg_path>");
                Console.WriteLine(@"Example: ImageProcess C:\Temp\Images");
            }
        }
    }
}
