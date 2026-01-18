namespace AlgoRhythm.Shared.Dtos.Common;

public class BlobFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public DateTime LastModified { get; set; }
    public string Url { get; set; } = string.Empty;
}