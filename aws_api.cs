using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Textract;
using Amazon.Textract.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TextractGeometry
{
    public class aws_api
    {
        static public String UploadFile(String accessKeyId, String secret, String bucketName, String filePath)
        {
            AmazonS3Client client = new AmazonS3Client(accessKeyId, secret, RegionEndpoint.EUWest1);
            PutObjectRequest putRequest = null;

            try
            {
                putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = Path.GetFileName(filePath),
                    FilePath = filePath
                };

                PutObjectResponse response = client.PutObject(putRequest);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                    ||
                    amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    throw new Exception("Check the provided AWS Credentials.");
                }
                else
                {
                    throw new Exception("Error occurred: " + amazonS3Exception.Message);
                }
            }

            return putRequest == null ? "" : putRequest.Key;
        }
        static public List<Block> AnalysePdf(String accessKeyId, String secret, String s3Bucket, String key)
        {
            Task<List<Block>> result = AnalysePdfAsync(accessKeyId, secret, s3Bucket, key);
            return result.Result;
        }

        static async private Task<List<Block>> AnalysePdfAsync(String accessKeyId, String secret, String s3Bucket, String key)
        {
            using (AmazonTextractClient textractClient = new AmazonTextractClient(accessKeyId, secret, RegionEndpoint.EUWest1))
            {
                StartDocumentAnalysisResponse startResponse = null;
                try
                {
                    startResponse = await textractClient.StartDocumentAnalysisAsync(
                        new StartDocumentAnalysisRequest
                        {
                            DocumentLocation = new DocumentLocation
                            {
                                S3Object = new Amazon.Textract.Model.S3Object
                                {
                                    Bucket = s3Bucket,
                                    Name = key
                                }
                            },
                            FeatureTypes = new List<string>
                            { 
                                "FORMS",
                                "TABLES",
                            }
                        }
                    );
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                GetDocumentAnalysisRequest getDetectionRequest = new GetDocumentAnalysisRequest
                {
                    JobId = startResponse.JobId
                };

                Console.WriteLine("");
                Console.WriteLine("Waiting for Job ID " + startResponse.JobId + " to complete.");
                Console.WriteLine("");

                GetDocumentAnalysisResponse getDetectionResponse = null;
                do
                {
                    Thread.Sleep(1000);
                    getDetectionResponse = await textractClient.GetDocumentAnalysisAsync(getDetectionRequest);
                } while (getDetectionResponse.JobStatus == JobStatus.IN_PROGRESS);

                if (getDetectionResponse.JobStatus == JobStatus.SUCCEEDED)
                {
                    do
                    {
                        if (String.IsNullOrEmpty(getDetectionResponse.NextToken))
                        {
                            break;
                        }

                        getDetectionRequest.NextToken = getDetectionResponse.NextToken;
                        getDetectionResponse = await textractClient.GetDocumentAnalysisAsync(getDetectionRequest);

                    } while (!String.IsNullOrEmpty(getDetectionResponse.NextToken));
                }
                else
                {
                    Console.WriteLine($"Job failed with message: {getDetectionResponse.StatusMessage}");
                }

                return getDetectionResponse.Blocks;
            }
        }
    }
}
