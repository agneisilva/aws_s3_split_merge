using Amazon.S3.Model;


namespace Shared;

public static class CommonConstants
{
    public static string NEW_LINE = Environment.NewLine;
    public static long MB = 1000000;
    public static long MAXIMUM_PART_SIZE = 100 * MB;
    public static long MINIMUM_PART_SIZE = 5 * MB;
}

public class FileMergingUploadRequest
{
    public string SourceBucketName { get; set; }
    public string DestinationBucketName { get; set; }
    public List<S3KeyInfo> S3KeysInfo { get; set; }
    public string MergedFileS3KeyPrefix { get; set; }
    public bool ShouldManageHeaders { get; set; }
}

public class FileMergingUploadResponse
{
    public string BucketName { get; set; }
    public string S3Key { get; set; }
}

public class MultiPartUploadRequest
{
    public string SourceBucketName { set; get; }
    public string DestinationBucketName { set; get; }
    public string UploadId { set; get; }
    public S3Parts S3Parts { set; get; }
    public int StartPartNumber { set; get; }
    public bool ShouldManageHeaders { set; get; }
}

public class MultiPartUploadResponse
{
    public int NextPartNumber { get; set; }
    public List<PartETag> PartETags { get; set; }
}

public class S3FileMergingRequest
{
    public string SourceBucketName { get; set; }
    public string DestinationBucketName { get; set; }
    public List<string> S3Keys { get; set; }
    public string S3FilePrefix { get; set; }
    /**
        * Prefix of the new file that is a concatenation of the contents of the given files.
        */
    public string MergedFileS3KeyPrefix { get; set; }
    /**
        * Should be set to true if the lambda should use the s3 file prefix to get the files from S3
        * and then merge those files.
        */
    public bool UseS3FilePrefix { get; set; }
    /**
        * Should be set to true if the older files should be deleted after merging.
        */
    public bool DeleteAfterMerge { get; set; }
    /**
        * This option is relevant for files in .csv or .tsv format.
        * If set to false, the files will be merged as they are line after line.
        * If set to true, only the header of the first file will be retained, and headers of other
        * files will be dropped. If this option is used, the assumption is that all the files should
        * have the same headers and in the same order.
        */
    public bool ShouldManageHeaders { get; set; }
}

public class S3FileMergingResponse
{
    public string BucketName { get; set; }
    public string ResultFilePath { get; set; }
}

public class S3KeyInfo
{
    public string KeyName { get; set; }
    public long Size { get; set; }
}

public class S3Parts
{
    public List<KeysAboveMaximumPartSize> KeysAboveMaximumPartSize { get; set; }
    public List<KeysBelowMaximumPartSize> KeysBelowMaximumPartSize { get; set; }
}

public class KeysAboveMaximumPartSize
{
    public S3KeyInfo s3Key { get; set; }
    public int PartNumber { get; set; }
}

public class KeysBelowMaximumPartSize
{
    public List<S3KeyInfo> Items { get; set; }
    public int PartNumber { get; set; }
}
