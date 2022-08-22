using Amazon.Lambda.Core;
using Amazon.S3.Model;
using System.Text;

namespace Shared;

public static class S3FileMergingUtils
{

    public static string mergeS3Files(List<List<string>> s3ObjectsContent, bool shouldRemoveHeader)
    {

        var list = s3ObjectsContent.Select(s3ObjectContent => getContentAsString(s3ObjectContent,
                shouldRemoveHeader)).ToList();

        return string.Join(Environment.NewLine, list);
    }

    public static string getFileHeader(List<string> s3ObjectContent)
    {
        return new StringBuilder().Append(s3ObjectContent[0]).Append(Environment.NewLine).ToString();
    }

    private static string getContentAsString(List<string> s3KeyContent,
            bool shouldRemoveHeader)
    {
        if (shouldRemoveHeader)
        {
            return string.Join(Environment.NewLine, s3KeyContent.Skip(1));
        }
        else
        {
            return string.Join(Environment.NewLine, s3KeyContent);
        }
    }

    public static string generateMergedFileS3Key(string mergedFileS3KeyPrefix,
        string fileFormat)
    {
        return new StringBuilder().Append(mergedFileS3KeyPrefix)
            .Append(Guid.NewGuid().ToString()).Append(fileFormat).ToString();
    }

    //public static S3FileMergingResponse getS3FileMergingResponse(string bucketName,
    //    string resultFilePath) {
    //    return new S3FileMergingResponse() { BucketName = bucketName, ResultFilePath = resultFilePath };
    //}

    public static MultiPartUploadRequest generateMultiPartUploadRequest(
        string sourceBucketName, string destinationBucketName, string uploadId,
        S3Parts s3Parts, int startPartNumber, bool shouldManageHeaders)
    {

        return new MultiPartUploadRequest()
        {
            SourceBucketName = sourceBucketName,
            DestinationBucketName = destinationBucketName,
            UploadId = uploadId,
            S3Parts = s3Parts,
            StartPartNumber = startPartNumber,
            ShouldManageHeaders = shouldManageHeaders
        };
    }

    public static MultiPartUploadResponse generateMultiPartUploadResponse(List<PartETag> partETags)
    {

        return new MultiPartUploadResponse()
        {
            PartETags = partETags
        };
    }

    public static FileMergingUploadResponse generateFileMergingUploadResponse(
        string bucketName, string s3Key)
    {

        return new FileMergingUploadResponse()
        {
            BucketName = bucketName,
            S3Key = s3Key
        };
    }



    public static void Log(string log)
    {
        LambdaLogger.Log(log);
        Console.WriteLine(log);
    }
}