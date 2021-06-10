namespace PhotoReview.Payloads
{
    public class CopyBlobActivityPayload
    {
        public string SourceContainer { get; set; }
        public string DestinationContainer { get; set; }
        public string FileName { get; set; }
    }
}
