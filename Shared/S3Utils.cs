using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared;

public static class S3Utils
{

    private static bool VALID = true;
    private static bool INVALID = false;
    private static bool USE_MULTIPART_UPLOAD = true;
    private static bool DO_NOT_USE_MULTIPART_UPLOAD = false;

    public static S3Parts chunkBySize(List<S3KeyInfo> s3KeysInfo)
    {
        if (validKeys(s3KeysInfo))
        {
            var keysAboveMaximumPartSize = getS3KeysAboveMaximumPartSize(s3KeysInfo);
            var keysBelowMaximumPartSize = getS3KeysBelowMaximumPartSize(s3KeysInfo);

            keysBelowMaximumPartSize = keysBelowMaximumPartSize.OrderByDescending(x => x.Size).ToList();

            var chunks = new List<KeysBelowMaximumPartSize>();
            var chunksPartNumber = 1;
            long currentSize = 0L;
            var currentList = new List<S3KeyInfo>();


            foreach (var s3KeyInfo in keysBelowMaximumPartSize)
            {
                currentSize += s3KeyInfo.Size;
                
                if (currentSize <= CommonConstants.MAXIMUM_PART_SIZE)
                {
                    currentList.Add(s3KeyInfo);
                }
                else
                {
                    chunks.Add(new KeysBelowMaximumPartSize() { PartNumber = chunksPartNumber++, Items = currentList });
                    currentList = new List<S3KeyInfo>();
                    currentList.Add(s3KeyInfo);
                    currentSize = s3KeyInfo.Size;
                }
            }
                
            if (currentSize != 0L)
            {
                chunks.Add(new KeysBelowMaximumPartSize() { PartNumber = chunksPartNumber++, Items = currentList });
            }

            return new S3Parts() {
                KeysAboveMaximumPartSize = keysAboveMaximumPartSize,
                KeysBelowMaximumPartSize = chunks
            };
        }
        else
        {
            throw new Exception($"Files should have size less than {CommonConstants.MAXIMUM_PART_SIZE}");
        }
    }

    private static List<S3KeyInfo> getS3KeysBelowMaximumPartSize(List<S3KeyInfo> s3KeysInfo)
    {
        return s3KeysInfo.Where(s3KeyInfo => s3KeyInfo.Size <= CommonConstants.MAXIMUM_PART_SIZE).ToList();
    }
    private static List<KeysAboveMaximumPartSize> getS3KeysAboveMaximumPartSize(List<S3KeyInfo> s3KeysInfo)
    {
        return s3KeysInfo.Where(s3KeyInfo => s3KeyInfo.Size > CommonConstants.MAXIMUM_PART_SIZE)
            .Select((s3Key, index)=> new KeysAboveMaximumPartSize() { s3Key = s3Key, PartNumber = index++}).ToList();
    }

    public static bool shouldUseMultiPartUpload(List<S3KeyInfo> s3KeysInfo)
    {
        long totalSize = s3KeysInfo.Select(s3 => s3.Size).Sum();
        if (totalSize < CommonConstants.MINIMUM_PART_SIZE)
        {
            return DO_NOT_USE_MULTIPART_UPLOAD;
        }
        else
        {
            return USE_MULTIPART_UPLOAD;
        }
    }

    public static string getFileFormat(string fileName)
    {
        return Path.GetExtension(fileName);
    }

    private static bool validKeys(List<S3KeyInfo> s3KeysInfo)
    {
        return isFileFormatConsistent(s3KeysInfo);
    }

    private static bool isFileFormatConsistent(List<S3KeyInfo> s3KeysInfo)
    {
        var fileFormat = getFileFormat(s3KeysInfo[0].KeyName);

        foreach (var s3KeyInfo in s3KeysInfo)
        {
            if (!fileFormat.Equals(getFileFormat(s3KeyInfo.KeyName)))
            {
                //log.error(String.format("Files %s are not of the same format", s3KeysInfo));
                return INVALID;
            }
        }
        return VALID;
    }

    public static bool IsEmpty<T>(this List<T> list)
    {
        if (list == null)
        {
            return true;
        }

        return !list.Any();
    }

}
