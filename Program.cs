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
using Newtonsoft.Json.Linq;
using System.Text;

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

            JProperty pages = new JProperty("Pages", 1);
            JObject pagesObj = new JObject(pages);
            JProperty documentMetadata = new JProperty("DocumentMetadata", pagesObj);
            JProperty jobStatus = new JProperty("JobStatus", "SUCCEEDED");

            JArray array = new JArray();
            foreach(Block block in blocks)
            {
                StringBuilder sb = new StringBuilder();
                StringWriter sw = new StringWriter(sb);

                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName("BlockType");
                    writer.WriteValue(block.BlockType);

                    if (block.BlockType.Value.CompareTo("PAGE") != 0)
                    {
                        writer.WritePropertyName("Confidence");
                        writer.WriteValue(block.Confidence);
                    }
                    if (!String.IsNullOrEmpty(block.Text))
                    {
                        writer.WritePropertyName("Text");
                        writer.WriteValue(block.Text);
                    }
                    if (!String.IsNullOrEmpty(block.TextType))
                    {
                        writer.WritePropertyName("TextType");
                        writer.WriteValue(block.TextType);
                    }

                    if (block.BlockType.Value.CompareTo("CELL") == 0)
                    {
                        writer.WritePropertyName("RowIndex");
                        writer.WriteValue(block.RowIndex);

                        writer.WritePropertyName("ColumnIndex");
                        writer.WriteValue(block.ColumnIndex);

                        writer.WritePropertyName("RowSpan");
                        writer.WriteValue(block.RowSpan);

                        writer.WritePropertyName("ColumnSpan");
                        writer.WriteValue(block.ColumnSpan);
                    }

                    writer.WritePropertyName("Geometry");

                    writer.WriteStartObject();
                        writer.WritePropertyName("BoundingBox");
                        writer.WriteStartObject();
                        writer.WritePropertyName("Width");
                        writer.WriteValue(block.Geometry.BoundingBox.Width);
                        writer.WritePropertyName("Height");
                        writer.WriteValue(block.Geometry.BoundingBox.Height);
                        writer.WritePropertyName("Left");
                        writer.WriteValue(block.Geometry.BoundingBox.Left);
                        writer.WritePropertyName("Top");
                        writer.WriteValue(block.Geometry.BoundingBox.Top);
                        writer.WriteEndObject();

                        writer.WritePropertyName("Polygon");
                        writer.WriteStartArray();
                        foreach (Amazon.Textract.Model.Point point in block.Geometry.Polygon)
                        {
                            writer.WriteStartObject();
                            writer.WritePropertyName("X");
                            writer.WriteValue(point.X);
                            writer.WritePropertyName("Y");
                            writer.WriteValue(point.Y);
                            writer.WriteEndObject();
                        }
                        writer.WriteEnd();
                    writer.WriteEndObject();

                    writer.WritePropertyName("Id");
                    writer.WriteValue(block.Id);

                    if (block.Relationships.Count > 0)
                    {
                        writer.WritePropertyName("Relationships");
                        writer.WriteStartArray();
                        foreach (Relationship relation in block.Relationships)
                        {
                            writer.WriteStartObject();
                            writer.WritePropertyName("Type");
                            writer.WriteValue(relation.Type);

                            writer.WritePropertyName("Ids");
                            writer.WriteStartArray();
                            foreach (String id in relation.Ids)
                            {
                                writer.WriteValue(id);
                            }
                            writer.WriteEnd();
                            writer.WriteEndObject();
                        }
                        writer.WriteEnd();
                    }

                    if (block.EntityTypes.Count > 0)
                    {
                        writer.WritePropertyName("EntityTypes");
                        writer.WriteStartArray();
                        foreach (String entityType in block.EntityTypes)
                        {
                            writer.WriteValue(entityType);
                        }
                        writer.WriteEnd();
                    }

                    writer.WritePropertyName("Page");
                    writer.WriteValue(block.Page);
                } // foreach

                String sBlock = sb.ToString();
                JObject blockObj = JObject.Parse(sBlock);
                array.Add(blockObj);
            }
            JProperty jBlocks = new JProperty("Blocks", array);

            JProperty analyzeDocumentModelVersion = new JProperty("AnalyzeDocumentModelVersion", "1.0");

            JObject document = new JObject(documentMetadata, jobStatus, jBlocks, analyzeDocumentModelVersion);

            using (StreamWriter sw = new StreamWriter(jsonFileName))
            {
                JsonSerializer serializer = JsonSerializer.Create(
                    new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented
                    });
                serializer.Serialize(sw, document);
            }
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
