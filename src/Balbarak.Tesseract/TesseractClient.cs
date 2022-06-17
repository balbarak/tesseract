using Balbarak.Tesseract.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Balbarak.Tesseract
{
    public class TesseractClient : ITesseractClient, IDisposable
    {
        private string _bashName;

        public TesseractClient()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                _bashName = "cmd.exe";
            else
                _bashName = "cmd.exe";
        }

        public async Task<string> Read(string inputFile,string output)
        {
            string result = "";

            FileManager.Instance.EnsureFilesExist();

            using (var client = new TerminalClient())
            {
                var workingDir = FileManager.Instance.OutputDir;
                var fileName = FileManager.Instance.FileName;

                var args = CreateCommand($"\"{inputFile}\" \"{output}\" -l eng+ara");

                result = await client.ExcuteAndReadOutputAsync(FileManager.Instance.OutputDir, _bashName, args);

                if (string.IsNullOrEmpty(result))
                    result = "Done";
            }

            return result;
        }

        public async Task<string> Read(string inputFile)
        {
            string result = "";

            FileManager.Instance.EnsureFilesExist();

            using (var client = new TerminalClient())
            {
                var workingDir = FileManager.Instance.OutputDir;
                var fileName = FileManager.Instance.FileName;

                var args =  CreateCommand($"\"{inputFile}\" - -l eng+ara");

                result = await client.ExcuteAndReadOutputAsync(FileManager.Instance.OutputDir, _bashName, args);
            }

            return result;
        }

        public async Task<string> Raw(string cmd)
        {
            string result = "";

            FileManager.Instance.EnsureFilesExist();

            using (var client = new TerminalClient())
            {
                var workingDir = FileManager.Instance.OutputDir;
                var fileName = FileManager.Instance.FileName;

                var args = CreateCommand(cmd);

                result = await client.ExcuteAndReadOutputAsync(FileManager.Instance.OutputDir, _bashName, args);
            }

            return result;
        }

        private string CreateCommand(string cmd)
        {
            var fileName = FileManager.Instance.FileName;

            return $"/c {fileName} {cmd}";
        }

        public void Dispose()
        {
            
        }
    }
}
