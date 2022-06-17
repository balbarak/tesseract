using Balbarak.Tesseract.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Balbarak.Tesseract.Test
{
    public class FileManagerTest
    {
        [Fact]
        public void Should_Extract_Files()
        {
            FileManager.Instance.ExtractFiles();

            Assert.True(FileManager.Instance.IsFilesExist());
        }

        [Fact]
        public void Should_Check_If_File_Exist()
        {
            FileManager.Instance.IsFilesExist();
        }

        [Fact]
        public void Should_Delete_Files()
        {
            FileManager.Instance.DeleteFiles();
        }

        [Fact]
        public void Should_Ensure_Files()
        {
            FileManager.Instance.EnsureFilesExist();
        }
    }
}
