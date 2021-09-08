using common;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using static iText.Kernel.Colors.ColorConstants;
using iText.Kernel.Pdf.Canvas;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using iText.Kernel.Colors;
using Amazon.Textract.Model;
using Amazon.Textract;
using Newtonsoft.Json;

namespace TextractGeometry
{
    public class Box
    {
        public String _text;
        public DeviceRgb _color;
        public BoundingBox _box;
    }

    class Program
    {
        static void Usage()
        {
            Console.WriteLine("Usage: TextractGeometry <PDF file> <Output PDF file> Output a PDF annotaed with the bounding boxes as found by AWS Textract.");
            Console.WriteLine("Example: TextractGeometry invoice.pdf annotated_invoice.pdf.");
            WaitForKeyQ();
        }
        static void WaitForKeyQ()
        {
            ConsoleKeyInfo key;

            Console.WriteLine("");
            Console.WriteLine("Press 'q' to quit.");
            Console.WriteLine("");

            do
            {
                key = Console.ReadKey();
            } while (key.KeyChar != 'q' && key.KeyChar != 'Q');
        }

        static void OutputJson(String outputPdfFileName, List<Block> blocks)
        {
            String jsonFileName = System.IO.Path.GetDirectoryName(outputPdfFileName) + "\\" + System.IO.Path.GetFileNameWithoutExtension(outputPdfFileName) + ".json";

            String output = JsonConvert.SerializeObject(blocks);

            StreamWriter sw = new StreamWriter(jsonFileName);
            sw.WriteLine(output);
            sw.Close();
        }

        static void Main(string[] args)
        {
            String assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Console.WriteLine("Textract Geeometry tool v" + assemblyVersion);

            if (args.Length != 2)
            {
                Usage();
                return;
            }
            else
            {
                String inputPdfFileName = args[0];
                if(!File.Exists(inputPdfFileName))
                {
                    Console.WriteLine("Error: " + inputPdfFileName + " does not exists!");
                    return;
                }
                Settings settings = new Settings();

                String key = aws_api.UploadFile(settings._AccessKeyId, settings._SecretAccessKey, settings._BucketName, inputPdfFileName);
                List<Block> blocks = aws_api.AnalysePdf(settings._AccessKeyId, settings._SecretAccessKey, settings._BucketName, key);

                String outputPdfFileName = args[1];
                OutputJson(outputPdfFileName, blocks);

                BoundingBox boundingBoxPage = null;
                List<Box> boundingBoxLines = new List<Box>();

                foreach (Block block in blocks)
                {
                    BlockType blockType = block.BlockType;
                    BoundingBox box = block.Geometry.BoundingBox;

                    if (blockType == BlockType.PAGE)
                    {
                        boundingBoxPage = box;
                    }
                    else if (blockType == BlockType.LINE)
                    {
                        Box lineBox = new Box();
                        lineBox._color = (DeviceRgb) RED;
                        lineBox._box = box;
                        boundingBoxLines.Add(lineBox);
                    }
                }

                PdfWriter writer = new PdfWriter(outputPdfFileName);
                PdfDocument pdfDoc = new PdfDocument(new PdfReader(inputPdfFileName), writer);
                PdfPage page = pdfDoc.GetFirstPage();
                PdfCanvas canvas = new PdfCanvas(page);
                Rectangle rectPage = page.GetPageSize();
                float widthPage  = rectPage.GetWidth();
                float heightPage = rectPage.GetHeight();

                foreach(Box box in boundingBoxLines)
                {
                    float offset = (heightPage * box._box.Height) + (heightPage * box._box.Top);
                    Rectangle rect = new Rectangle(
                        widthPage * box._box.Left, heightPage - offset, 
                        widthPage * box._box.Width, heightPage * box._box.Height);
                    canvas.SetStrokeColor(box._color);
                    canvas.Rectangle(rect);
                    canvas.Stroke();
                }

                Console.WriteLine("Annotated PDF writted to '" + outputPdfFileName + "'.");

                pdfDoc.Close();
                WaitForKeyQ();
            }
        }
    }
}
