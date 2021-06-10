using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using PhotoReview.Constants;
using PhotoReview.Payloads;

namespace PhotoReview.Functions
{
    public static class PhotoReceiver
    {
        private const string INITIAL_PHOTOS_CONTAINER = "requests";

        private static readonly string BasePhotosUrl = Environment.GetEnvironmentVariable(nameof(BasePhotosUrl));
        private static readonly string AdminEmail = Environment.GetEnvironmentVariable(nameof(AdminEmail));
        private static readonly string SenderEmail = Environment.GetEnvironmentVariable(nameof(SenderEmail));
        private static readonly string FunctionsBaseUrl = Environment.GetEnvironmentVariable(nameof(FunctionsBaseUrl));
        
        [FunctionName(nameof(PhotoReceiver))]
        public static async Task Run(
            [BlobTrigger(
            INITIAL_PHOTOS_CONTAINER + "/{name}", Connection = ConnectionStrings.PhotosConnectionString)] Stream photo, 
            string name,
            [DurableClient] IDurableOrchestrationClient durableClient)
        {
            var payload = new ReviewProcessOrchestratorPayload
            {
                BasePhotosUrl = BasePhotosUrl,
                ReviewerEmail = AdminEmail,
                FileName = name,
                PhotoContainer = INITIAL_PHOTOS_CONTAINER,
                SenderEmail = SenderEmail,
                FunctionsBaseUrl = FunctionsBaseUrl
            };

            await durableClient.StartNewAsync(nameof(ReviewProcessOrchestrator), payload);
        }
    }
}
