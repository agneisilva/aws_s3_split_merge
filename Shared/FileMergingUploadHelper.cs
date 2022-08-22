using Amazon.Lambda.Core;
using Amazon.S3.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared;

public class FileMergingUploadHelper
{
    private static int FIRST_PART = 1;
    private static bool REMOVE_HEADER = true;
    private static bool DO_NOT_REMOVE_HEADER = false;
    private static int MAXIMUM_THREAD_COUNT = 30;

    private S3Helper s3Helper;
    private ILambdaContext _context;

    public FileMergingUploadHelper(ILambdaContext context)
    {
        this.s3Helper = new S3Helper();
        _context = context;
    }

    public async Task<FileMergingUploadResponse> Upload(
            FileMergingUploadRequest fileMergingUploadRequest)
    {
        if (S3Utils.shouldUseMultiPartUpload(fileMergingUploadRequest.S3KeysInfo))
        {
            S3FileMergingUtils.Log($"starting MultiPartUpload");
            return await handleMultiPartUpload(fileMergingUploadRequest);
        }
        else
        {
            S3FileMergingUtils.Log($"starting MultiPartUpload");
            return await handleNormalUpload(fileMergingUploadRequest);
        }
    }

    private async Task<FileMergingUploadResponse> handleNormalUpload(
        FileMergingUploadRequest fileMergingUploadRequest)
    {
        var mergedFileS3Key =
            S3FileMergingUtils.generateMergedFileS3Key(fileMergingUploadRequest.MergedFileS3KeyPrefix,
                S3Utils.getFileFormat(fileMergingUploadRequest.S3KeysInfo[0].KeyName));

        var s3Keys = fileMergingUploadRequest.S3KeysInfo
            .Select(x => x.KeyName).ToList();


        //Deveria ser em paralelo
        var s3ObjectsContent = await getS3ObjectsContent(
            fileMergingUploadRequest.SourceBucketName, s3Keys);

        string mergedObject = getMergedObject(fileMergingUploadRequest.ShouldManageHeaders,
            s3ObjectsContent, FIRST_PART);

        await s3Helper.uploadObject(fileMergingUploadRequest.DestinationBucketName, mergedFileS3Key,
            mergedObject);

        return S3FileMergingUtils.generateFileMergingUploadResponse(
            fileMergingUploadRequest.DestinationBucketName, mergedFileS3Key);
    }

    private async Task<FileMergingUploadResponse> handleMultiPartUpload(FileMergingUploadRequest fileMergingUploadRequest)
    {
        string sourceBucketName = fileMergingUploadRequest.SourceBucketName;
        string destinationBucketName = fileMergingUploadRequest.DestinationBucketName;

        string mergedFileS3Key =
            S3FileMergingUtils.generateMergedFileS3Key(fileMergingUploadRequest.MergedFileS3KeyPrefix,
                S3Utils.getFileFormat(fileMergingUploadRequest.S3KeysInfo[0].KeyName));

        S3FileMergingUtils.Log($"mergedFileS3Key: {mergedFileS3Key}");

        S3Parts s3Parts = S3Utils.chunkBySize(fileMergingUploadRequest.S3KeysInfo);

        string uploadId = await s3Helper.startMultiPartUpload(destinationBucketName, mergedFileS3Key);

        S3FileMergingUtils.Log($"uploadId: {uploadId}");

        try
        {

            var largeS3KeysMultiPartResponse =
                await handleMultiPartUploadForKeysAboveMaximumPartSize(
                    S3FileMergingUtils.generateMultiPartUploadRequest(sourceBucketName,
                                                                        destinationBucketName,
                                                                        uploadId,
                                                                        s3Parts,
                                                                        1,
                                                                        fileMergingUploadRequest.ShouldManageHeaders),
                    mergedFileS3Key);

            var smallS3KeysMultiPartResponse =
                await handleMultiPartUploadForKeysBelowMaximumPartSize(
                    S3FileMergingUtils.generateMultiPartUploadRequest(sourceBucketName,
                                                                        destinationBucketName,
                                                                        uploadId,
                                                                        s3Parts,
                                                                        largeS3KeysMultiPartResponse.NextPartNumber,
                                                                        fileMergingUploadRequest.ShouldManageHeaders),
                    mergedFileS3Key);

            var partETags = getPartETags(smallS3KeysMultiPartResponse.PartETags,
                                            largeS3KeysMultiPartResponse.PartETags);

            s3Helper.completeMultiPartUpload(destinationBucketName,
                                                mergedFileS3Key,
                                                uploadId,
                                                partETags);

            return S3FileMergingUtils.generateFileMergingUploadResponse(destinationBucketName, mergedFileS3Key);
        }
        catch (Exception e)
        {
            S3FileMergingUtils.Log("Error:" + e.ToString());

            string errorMessage = $"Aborting the multipart upload for request {fileMergingUploadRequest}";
            S3FileMergingUtils.Log(errorMessage);

            await s3Helper.abortMultiPartUpload(destinationBucketName, mergedFileS3Key, uploadId);
            S3FileMergingUtils.Log("upload aborted");

            throw new Exception(e.ToString());
        }
    }

    private List<PartETag> getPartETags(List<PartETag> smallUploadPartETags, List<PartETag> largeUploadPartETags)
    {
        var partETags = new List<PartETag>();

        if (!smallUploadPartETags.IsEmpty())
        {
            partETags.AddRange(smallUploadPartETags);
        }

        if (!largeUploadPartETags.IsEmpty())
        {
            partETags.AddRange(largeUploadPartETags);
        }

        return partETags;
    }

    private async Task<MultiPartUploadResponse> handleMultiPartUploadForKeysAboveMaximumPartSize(
        MultiPartUploadRequest multiPartUploadRequest, string mergerFileS3Key)
    {
        var sourceBucketName = multiPartUploadRequest.SourceBucketName;
        var destinationBucketName = multiPartUploadRequest.DestinationBucketName;
        var partETags = new List<PartETag>();

        var s3Keys =
            multiPartUploadRequest
            .S3Parts
            .KeysAboveMaximumPartSize
            .Select(x => new { x.s3Key.KeyName, x.PartNumber })
            .ToList();

        string uploadId = multiPartUploadRequest.UploadId;
        //int partNumber = multiPartUploadRequest.StartPartNumber;

        foreach (var s3Key in s3Keys)
        {
            var s3Object = await s3Helper.getS3Object(sourceBucketName, s3Key.KeyName);

            long currentSize = 0L;
            var currentPart = new List<string>();


            using (var reader = new StreamReader(s3Object.ResponseStream))
            {
                while (true)
                {
                    var currentLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(currentLine)) break;

                    currentSize += Encoding.Default.GetBytes(currentLine).Length;

                    if (currentSize < CommonConstants.MAXIMUM_PART_SIZE)
                    {
                        currentPart.Add(currentLine);
                    }
                    else
                    {
                        var content = new List<List<string>>() { currentPart };

                        var mergedContent = getMergedObject(
                            multiPartUploadRequest.ShouldManageHeaders, content, s3Key.PartNumber);

                        partETags.Add(await s3Helper.uploadPart(destinationBucketName, mergerFileS3Key,
                            uploadId, s3Key.PartNumber, mergedContent));

                        currentSize = Encoding.Default.GetBytes(currentLine).Length;

                        currentPart.Add(currentLine);
                    }
                }
            }

            if (!currentPart.IsEmpty())
            {
                var content = new List<List<string>>() { currentPart };

                var mergedContent = getMergedObject(
                    multiPartUploadRequest.ShouldManageHeaders, content, s3Key.PartNumber);

                partETags.Add(await s3Helper.uploadPart(destinationBucketName, mergerFileS3Key,
                    uploadId, s3Key.PartNumber, mergedContent));
            }
        }
        return S3FileMergingUtils.generateMultiPartUploadResponse(partETags);
    }
        
    private async Task<MultiPartUploadResponse> handleMultiPartUploadForKeysBelowMaximumPartSize(
        MultiPartUploadRequest multiPartUploadRequest, string mergedFileS3Key)
    {
        var chunks = multiPartUploadRequest.S3Parts.KeysBelowMaximumPartSize;
        var uploadId = multiPartUploadRequest.UploadId;
            
        var partETags= new ConcurrentDictionary<PartETag, int>();

        var option = new ParallelOptions
        {
            MaxDegreeOfParallelism = Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 0.85) * 2.0))
        };

        await Parallel.ForEachAsync(chunks, option, async (chunk, token) =>
        {
            var s3Keys = chunk.Items.Select(x => x.KeyName).ToList();

            var s3ObjectsContent = await getS3ObjectsContent(
                multiPartUploadRequest.SourceBucketName, s3Keys);

            var mergedObject = getMergedObject(multiPartUploadRequest.ShouldManageHeaders,
                s3ObjectsContent, chunk.PartNumber);

            s3ObjectsContent = null;

            var partETag = await s3Helper.uploadPart(multiPartUploadRequest.DestinationBucketName,
                    mergedFileS3Key, uploadId, chunk.PartNumber, mergedObject);

            mergedObject = null;

            partETags.TryAdd(partETag, Environment.CurrentManagedThreadId);
        });

        //foreach (var chunk in chunks)
        //{
        //    try
        //    {
        //        var s3Keys = chunk.Items.Select(x => x.KeyName).ToList();

        //        var s3ObjectsContent = await getS3ObjectsContent(
        //            multiPartUploadRequest.SourceBucketName, s3Keys);

        //        var mergedObject = getMergedObject(multiPartUploadRequest.ShouldManageHeaders,
        //            s3ObjectsContent, chunk.PartNumber);

        //        s3ObjectsContent = null;

        //        var partETag = await s3Helper.uploadPart(multiPartUploadRequest.DestinationBucketName,
        //                mergedFileS3Key, uploadId, chunk.PartNumber, mergedObject);

        //        mergedObject = null;

        //        partETags.TryAdd(partETag, Environment.CurrentManagedThreadId);
        //    }
        //    catch (Exception er)
        //    {
        //        S3FileMergingUtils.Log(er.ToString());
        //        throw new Exception(er.ToString());
        //    }
        //}

        return S3FileMergingUtils.generateMultiPartUploadResponse(partETags.Select(x => x.Key).ToList());
    }

    private async Task<List<List<string>>> getS3ObjectsContent(string bucketName, List<string> s3Keys)
    {
        try
        {
            var results = new ConcurrentDictionary<List<string>, int>();

            var option = new ParallelOptions
            {
                MaxDegreeOfParallelism = Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 0.75) * 2.0))
            };

            await Parallel.ForEachAsync(s3Keys, option, async (key, token) =>
            {
                var lines = await getObjectContent(bucketName, key);

                results.TryAdd(lines, Thread.CurrentThread.ManagedThreadId);
            });

            return results.Select(x => x.Key).ToList();
        }
        catch (Exception e)
        {
            string errorMessage = $"Failed to get S3 objects";
            S3FileMergingUtils.Log(errorMessage);

            S3FileMergingUtils.Log(e.ToString());

            throw new Exception(e.ToString());
        }
    }

    private async Task<List<string>> getObjectContent(string bucketName, string s3Key)
    {
        try
        {
            using var s3Object = await s3Helper.getS3Object(bucketName, s3Key);

            using (var reader = new StreamReader(s3Object.ResponseStream))
            {
                var lines = new List<string>();
                while(true) 
                { 
                    var line = reader.ReadLine();
                    if (string.IsNullOrEmpty(line)) break;

                    lines.Add(line);
                }
                return lines;
            }
        }
        catch (Exception e)
        {
            string errorMessage = $"Failed to get the object with s3Key {s3Key} from bucket {bucketName}";

            S3FileMergingUtils.Log(errorMessage);
            S3FileMergingUtils.Log(e.ToString());

            throw new Exception(e.ToString());
        }
    }

    private string getMergedObject(bool shouldManageHeaders,
                                    List<List<string>> s3ObjectsContent,
                                    int partNumber)
    {
        if (shouldManageHeaders)
        {
            StringBuilder mergedContent = new StringBuilder();
            if (partNumber == FIRST_PART)
            {
                mergedContent.Append(S3FileMergingUtils.getFileHeader(s3ObjectsContent[0]));
            }
            mergedContent.Append(S3FileMergingUtils.mergeS3Files(s3ObjectsContent, REMOVE_HEADER));
            return mergedContent.ToString();
        }
        else
        {
            return S3FileMergingUtils.mergeS3Files(s3ObjectsContent, DO_NOT_REMOVE_HEADER);
        }
    }
}