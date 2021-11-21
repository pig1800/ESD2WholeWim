using System;
using System.IO;
using System.Runtime.InteropServices;
using ManagedWimLib;

namespace ESD2WholeWim
{
    class ESDProc
    {
        readonly string srcPath;
        readonly string dstPath;
        readonly string destPath;
        int imageCount;
        int currentImage;
        int fileCount;
        int currentFile;
        int cumulativeImages = 1;
        StreamWriter esdinfo;
        string currentMode;
        string[] files;

        public ESDProc(string srcPath, string dstPath, String fileListPath)
        {
            this.srcPath = srcPath;
            this.dstPath = Path.GetDirectoryName(dstPath);
            destPath = dstPath;
            files = File.ReadAllLines(fileListPath);
        }

        private void Init()
        {
            string arch = null;
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                    arch = "x86";
                    break;
                case Architecture.X64:
                    arch = "x64";
                    break;
                case Architecture.Arm:
                    arch = "armhf";
                    break;
                case Architecture.Arm64:
                    arch = "arm64";
                    break;
            }
            string libPath = Path.Combine(arch, "libwim-15.dll");

            if (!File.Exists(libPath))
                throw new PlatformNotSupportedException($"Unable to find native library [{libPath}].");

            Wim.GlobalInit(libPath);
        }

        private void Cleanup()
        {
            Wim.GlobalCleanup();
        }

        public void Start()
        {
            Init();

            string infopath = Path.Combine(dstPath, string.Format("{0}.txt", Path.GetFileNameWithoutExtension(destPath)));
            esdinfo = File.CreateText(infopath);

            fileCount = files.Length;
            for (int i = 0; i < fileCount; i++)
            {
                esdinfo.WriteLine("");
                currentFile = i + 1;
                Console.WriteLine("{0}/{1}: {2}", currentFile, fileCount, files[i]);
                Proc(files[i]);
                Console.WriteLine("", currentFile, fileCount, files[i]);
            }
            esdinfo.Flush();
            esdinfo.Close();
            esdinfo.Dispose();
            Cleanup();
        }

        private void Proc(String file)
        {
            string fullPath = Path.Combine(srcPath, file);
            var wim = Wim.OpenWim(fullPath, OpenFlags.None);
            Wim destwim;
            if (!File.Exists(destPath))
            {
                destwim = Wim.CreateNewWim(CompressionType.LZMS);
                destwim.Write(destPath, -1, WriteFlags.None, 4);
            }

            destwim = Wim.OpenWim(destPath, OpenFlags.None);
            destwim.RegisterCallback(Pcb);

            var info = wim.GetWimInfo();

            imageCount = (int)info.ImageCount;
            for (int i = 4; i <= info.ImageCount; i++)
            {
                currentImage = i;
                currentMode = "[IMPORT]";

                string vol = "";
                string volesd = "";
                if (file.Contains("_VOL_"))
                {
                    vol = " VOL";
                    volesd = "Volume";
                }
                else
                {
                    volesd = "Retail";
                }

                var newName = String.Format("{0} {1}{2}", wim.GetImageName(i), wim.GetImageProperty(i, "WINDOWS/LANGUAGES/DEFAULT"), vol);
                esdinfo.WriteLine("{0,-6}{1,-33}{2,-10}{3}", cumulativeImages, wim.GetImageName(i), volesd, wim.GetImageProperty(i, "WINDOWS/LANGUAGES/DEFAULT"));
                cumulativeImages++;
                wim.ExportImage(i, destwim, newName, wim.GetImageDescription(i), ExportFlags.None);
                destwim.Overwrite(WriteFlags.Solid, 4);
            }
        }

        CallbackStatus Pcb(ProgressMsg msg, object info, object progctx)
        {
            if (msg == ProgressMsg.WriteStreams)
            {
                WriteStreamsProgress progress = (WriteStreamsProgress)info;
                float percent = (float)progress.CompletedBytes / (float)progress.TotalBytes;
                Console.Write("\r                                                                                ");
                Console.Write("\r{5} {0}/{1}: {2}/{3} {4}", currentImage, imageCount,
                    progress.CompletedBytes, progress.TotalBytes, percent.ToString("P"), currentMode);
            }

            return CallbackStatus.Continue;
        }
    }
}
