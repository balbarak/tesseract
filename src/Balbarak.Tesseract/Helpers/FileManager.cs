using Balbarak.Tesseract.Resx;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Balbarak.Tesseract.Helpers
{
    public class FileManager
    {
        public static FileManager Instance { get; } = new FileManager();

        private string _outputDir = @"C:\Users\balba\Desktop\workshop\tesseract-data";

        public string OutputDir => _outputDir;

        public FileManager()
        {
            _outputDir = Path.Combine(Environment.CurrentDirectory, "tesseract-data");
        }

        public string FileName
        {
            get
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    return "tesseract.exe";
                else
                    return "tesseract.exe";
            }
        }

        public void ExtractFiles()
        {

            var fileData = AppResources.Tesseract_win;

            if (!Directory.Exists(_outputDir))
                Directory.CreateDirectory(_outputDir);

            using (var ms = new MemoryStream())
            {
                ms.Write(fileData, 0, fileData.Length);
                ms.Position = 0;

                using (var zip = new ZipArchive(ms))
                {
                    zip.ExtractToDirectory(_outputDir);
                }
            }

            //ZipFile.ExtractToDirectory()
        }

        public bool IsFilesExist()
        {
            if (!Directory.Exists(_outputDir))
                return false;


            var files = Directory.GetFiles(_outputDir);

            return true;
        }

        public void DeleteFiles()
        {
            if (Directory.Exists(_outputDir))
                Directory.Delete(_outputDir, true);
        }

        public void EnsureFilesExist()
        {
            if (!IsFilesExist())
            {
                ExtractFiles();
            }
        }
    }
}
