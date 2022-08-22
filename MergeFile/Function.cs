using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Shared;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MergeFileFunction;

public class Function
{
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
    public async Task<FileMergingUploadResponse> FunctionHandler(FunctionInput input, ILambdaContext context)
    {
        try
        {
            S3FileMergingUtils.Log("Starting without looping");
            
            return await MergeAndUploadFile(context);
        }
        catch (Exception er)
        {
            S3FileMergingUtils.Log($"Erro: {er}");
            throw;
        }
        finally
        {
            S3FileMergingUtils.Log("Finished");
        }

    }

    async Task<FileMergingUploadResponse> MergeAndUploadFile(ILambdaContext context)
    {
        string bucketName = "processed-tasks-bucket-sf";

        var client = new AmazonS3Client();

        var request = new ListObjectsRequest() { BucketName = bucketName, Prefix = "processing/cf" };
        var s3KeysInfo = new List<S3KeyInfo>();

        var responseFile = await client.ListObjectsAsync(request);

        foreach (S3Object entry in responseFile.S3Objects)
        {
            var s3KeyInfo = new S3KeyInfo() { KeyName = entry.Key, Size = entry.Size };
            s3KeysInfo.Add(s3KeyInfo);
        }

        var mergeRequest = new FileMergingUploadRequest()
        {
            DestinationBucketName = bucketName,
            SourceBucketName = bucketName,
            S3KeysInfo = s3KeysInfo,
            ShouldManageHeaders = false
        };

        S3FileMergingUtils.Log($"DestinationBucketName:{bucketName}, SourceBucketName{bucketName}, S3KeysInfo{s3KeysInfo.Count()}");

        return await new FileMergingUploadHelper(context).Upload(mergeRequest);
    }
}
