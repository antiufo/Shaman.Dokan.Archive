using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using SevenZip;
using System.IO;

namespace Shaman.Dokan
{
    class SevenZipProgram
    {
        static int Main(string[] args)
        {
            SevenZipExtractor.SetLibraryPath(Path.Combine(Path.GetDirectoryName(typeof(SevenZipProgram).Assembly.Location), "7z.dll"));
            var file = args.FirstOrDefault(x => !x.StartsWith("-"));
            if (file == null)
            {
                Console.WriteLine("Must specify a file.");
                return 1;
            }
            var mountpoint = new SevenZipFs(file).MountSimple(4);
            if (args.Contains("--open"))
                Process.Start(mountpoint);
            new TaskCompletionSource<bool>().Task.Wait();
            return 0;
        }
    }
}
