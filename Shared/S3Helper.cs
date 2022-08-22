using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using System.Text;

namespace Shared;

public class S3Helper
{
    private IAmazonS3 amazonS3;

    public S3Helper()
    {
        this.amazonS3 = new AmazonS3Client();
    }

    public async Task abortMultiPartUpload(string bucketName, string s3Key,
        string uploadId)
    {
        var abortMultipartUploadRequest =
            new AbortMultipartUploadRequest() 
            { BucketName = bucketName, 
                Key = s3Key, 
                UploadId = uploadId };
            
        await amazonS3.AbortMultipartUploadAsync(abortMultipartUploadRequest);
    }

    public async void completeMultiPartUpload(string bucketName, string s3Key,
        string uploadId, List<PartETag> partETags)
    {
        var completeMultipartUploadRequest =
            new CompleteMultipartUploadRequest()
            {
                UploadId = uploadId,
                BucketName = bucketName,
                Key = s3Key,
                PartETags = partETags
            };
        
        try
        {
            await amazonS3.CompleteMultipartUploadAsync(completeMultipartUploadRequest);
        }
        catch (Exception e)
        {
            S3FileMergingUtils.Log(e.ToString());
        }
    }

    public async Task<PartETag> uploadPart(string bucketName, string s3Key,
        string uploadId, int partNumber, string content)
    {

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var uploadPartRequest =
            new UploadPartRequest()
            {
                UploadId = uploadId,
                BucketName = bucketName,
                Key = s3Key, 
                PartNumber = partNumber, 
                InputStream = stream, 
                PartSize = stream.Length
            };
                
        var uploadPartResponse = await amazonS3.UploadPartAsync(uploadPartRequest);

        return new PartETag(uploadPartResponse);
    }

    public async Task<string> startMultiPartUpload(string bucketName, string s3Key)
    {
        var initiateMultipartUploadRequest =
            new InitiateMultipartUploadRequest() { BucketName = bucketName, Key = s3Key };
            
        var initiateMultipartUploadResult =
            await amazonS3.InitiateMultipartUploadAsync(initiateMultipartUploadRequest);

        return initiateMultipartUploadResult.UploadId;
    }

    public async Task<PutObjectResponse> uploadObject(string bucketName, string s3Key,
        string content)
    {
        var input = new PutObjectRequest()
        {
            BucketName = bucketName,
            Key = s3Key,
            InputStream = new MemoryStream(Encoding.UTF8.GetBytes(content))
        };
            
        return await amazonS3.PutObjectAsync(input);
    }

    public async Task<GetObjectResponse> getS3Object(string bucketName, string s3Key)
    {
        return await amazonS3.GetObjectAsync(bucketName, s3Key);
    }
}

