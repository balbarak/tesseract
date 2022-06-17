using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Balbarak.Tesseract.Test
{
    public class TesseractClientTest
    {
        [Fact]
        public async Task Should_Read_Image_And_Output()
        {
            var inputFile = @"C:\Users\balba\Desktop\workshop\letters\1.jpg";
            var outputFile = @"C:\Users\balba\Desktop\workshop\output.txt";

            var client = new TesseractClient();

            var result = await client.Read(inputFile,outputFile);
        }

        [Fact]
        public async Task Should_Read_Image()
        {
            var inputFile = @"C:\Users\balba\Desktop\workshop\letters\1.jpg";

            var client = new TesseractClient();

            var result = await client.Read(inputFile);
        }


        [Fact]
        public async Task Should_Exceute_Raw_Command()
        {
            var client = new TesseractClient();

            var result = await client.Raw("--version");
        }

        [Fact]
        public async Task Should_Exceute_Raw_Command_To_Read()
        {
            var inputFile = @"C:\Users\balba\Desktop\workshop\letters\1.jpg";

            var client = new TesseractClient();

            //var result = await client.Raw($"{inputFile} - -l eng+ara --psm 6 -c preserve_interword_spaces=1");
            var result = await client.Raw($"{inputFile} - -l eng+ara tsv");
        }
    }
}
