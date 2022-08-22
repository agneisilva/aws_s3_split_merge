using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Transfer;
using System.Text;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SplitFileFunction;

public class Function
{
    static string bucketName = "processed-tasks-bucket-sf";
    static string fileName = $"testfile_financial_data_500mb.csv";

    public class FunctionInput
    {
        public string Value { get; set; }
    }

    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task<string> FunctionHandler(FunctionInput input, ILambdaContext context)
    {
        await ReadingAnObjectFromS3AsAStream();

        return "Sucesso!";
    }

    async Task ReadingAnObjectFromS3AsAStream()
    {

        await CarryOutAWSTask(async () =>
        {
            var chunckFile = 1;

            var client = new AmazonS3Client();
            var fileTransferUtility = new TransferUtility(client);
            using (var fs = await fileTransferUtility.OpenStreamAsync(bucketName, fileName, CancellationToken.None))
            {
                using (var reader = new StreamReader(fs))
                {
                    string? line;
                    int count = 0;
                    var lines = new List<string>();
                    var tasks = new List<Task>();

                    while (true)
                    {
                        line = reader.ReadLine();

                        if (string.IsNullOrEmpty(line)) break;

                        count++;
                        //currentSize = Encoding.Default.GetBytes(currentLine).Length;

                        lines.Add(line);

                        if (lines.Count == 200_000)
                        {
                            // convert string to stream
                            var byteArray = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, lines));
                            var stream = new MemoryStream(byteArray);

                            var task = UploadFile(stream, $"processing/cf-{chunckFile}.csv");
                            tasks.Add(task);

                            chunckFile++;
                            lines.Clear();
                        }
                    }

                    await Task.WhenAll(tasks.ToArray());

                    Console.WriteLine($"Content of file {fileName} is");

                }
            }
        }, "Reading an Object from S3 as a Stream");
    }

    async Task UploadFile(Stream streamToUpload, string chunckfile)
    {
        await CarryOutAWSTask(async () =>
        {

            var client = new AmazonS3Client();
            var fileTransferUtility = new TransferUtility(client);

            var uploadRequest = new TransferUtilityUploadRequest()
            {
                InputStream = streamToUpload,
                Key = chunckfile,
                BucketName = bucketName,
                CannedACL = S3CannedACL.PublicRead
            };

            //If you are uploading large files, TransferUtility 
            //will use multipart upload to fulfill the request
            await fileTransferUtility.UploadAsync(uploadRequest);

        }, "Uploading new file");
    }

    async Task CarryOutAWSTask(Func<Task> taskToPerform, string op)
    {
        try
        {
            await taskToPerform();
        }
        catch (AmazonS3Exception amazonS3Exception)
        {
            if (amazonS3Exception.ErrorCode != null &&
                (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") ||
                amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
            {
                Console.WriteLine("Please check the provided AWS Credentials.");
                Console.WriteLine("If you haven't signed up for Amazon S3, please visit http://aws.amazon.com/s3");
            }
            else
            {
                Console.WriteLine($"An Error, number '{amazonS3Exception.ErrorCode}', " +
                                  $"occurred when '{op}' with the message '{amazonS3Exception.Message}'");
            }
        }
        catch (Exception err)
        {
            Console.WriteLine(err.ToString());
        }
    }
}
