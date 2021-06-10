namespace PhotoReview.Payloads
{
    public class ReviewProcessOrchestratorPayload
    {
        public string ReviewerEmail { get; set; }
        public string SenderEmail { get; set; }
        public string FileName { get; set; }
        public string PhotoContainer { get; set; }
        public string FunctionsBaseUrl { get; set; }
        public string BasePhotosUrl { get; set; }
    }
}
