using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Denali.IdealLibConverter
{


    class Program
    {
        static void Main()
        {
            string sourceDirectory = @"C:\iko\robot\denali";
            //string outputFile = @"C:\iko\robot\Denali.IdealLibConverter\lib.cs";
            string outputFile = @"C:\ideal\Lib.cs";
            string commonNamespace = "ideal";

            CombineClasses(sourceDirectory, outputFile, commonNamespace);

            Console.WriteLine("Classes combined successfully!");
        }

        static void CombineClasses(string sourceDirectory, string outputFile, string commonNamespace)
        {
            StringBuilder combinedContent = new StringBuilder();
            string[] files = Directory.GetFiles(sourceDirectory, "*.cs");

            foreach (var filePath in files)
            {
                string content = File.ReadAllText(filePath);
                combinedContent.AppendLine(content);
            }

            string combinedFileContent = "namespace ideal {"+ combinedContent.ToString() +"}";

            File.WriteAllText(outputFile, combinedFileContent);
        }
    }

}
