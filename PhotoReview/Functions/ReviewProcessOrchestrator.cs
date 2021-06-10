using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using PhotoReview.Constants;
using PhotoReview.Payloads;

namespace PhotoReview.Functions
{
    public static class ReviewProcessOrchestrator
    {
        private const int TIME_FOR_APPROVAL_IN_HOURS = 24;

        private const string APPROVED_PHOTOS_CONTAINER = "approved";
        private const string REJECTED_PHOTOS_CONTAINER = "rejected";

        private const string REVIEW_SUBJECT_TEMPLATE = "New photo \"{0}\" to review";

        private const string REVIEW_BODY_TEMPLATE =
            "Photo to review: <br/><br/><img src=\"{0}\"><br/><a href=\"{1}?instanceId={3}\">Approve</a><br/><a href=\"{2}?instanceId={3}\">Reject</a>";

        [FunctionName(nameof(ReviewProcessOrchestrator))]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger logger)
        {
            var input = context.GetInput<ReviewProcessOrchestratorPayload>();

            var photoUrl = $"{input.BasePhotosUrl}/{input.PhotoContainer}/{input.FileName}";

            var sendEmailPayload = new SendEmailActivityPayload
            {
                From = input.SenderEmail,
                To = input.ReviewerEmail,
                Subject = string.Format(REVIEW_SUBJECT_TEMPLATE, input.FileName),
                Body = string.Format(
                                    REVIEW_BODY_TEMPLATE, 
                                    photoUrl, 
                                    $"{input.FunctionsBaseUrl}/{nameof(ReviewPhoto.ApprovePhoto)}",
                                    $"{input.FunctionsBaseUrl}/{nameof(ReviewPhoto.RejectPhoto)}",
                                    context.InstanceId)
            };

            await context.CallActivityAsync(nameof(SendEmailActivity), sendEmailPayload);

            using var timeoutCancellationToken = new CancellationTokenSource();

            var dueTime = context.CurrentUtcDateTime.AddHours(TIME_FOR_APPROVAL_IN_HOURS);

            var timerTask = context.CreateTimer(dueTime, timeoutCancellationToken.Token);
            var approvalEventTask = context.WaitForExternalEvent(Events.PhotoApprovedEvent);
            var rejectionEventTask = context.WaitForExternalEvent(Events.PhotoRejectedEvent);

            var winner = await Task.WhenAny(timerTask, approvalEventTask, rejectionEventTask);

            if (winner == timerTask)
            {
                await CopyBlob(context, input.PhotoContainer, REJECTED_PHOTOS_CONTAINER, input.FileName);
                logger.LogInformation($"{input.FileName} was rejected due to timeout.");
                return;
            }

            if (winner == rejectionEventTask)
            {
                await CopyBlob(context, input.PhotoContainer, REJECTED_PHOTOS_CONTAINER, input.FileName);
                timeoutCancellationToken.Cancel();
                logger.LogInformation($"{input.FileName} was rejected by reviewer.");
                return;
            }

            await CopyBlob(context, input.PhotoContainer, APPROVED_PHOTOS_CONTAINER, input.FileName);
            timeoutCancellationToken.Cancel();
            logger.LogInformation($"{input.FileName} was approved by reviewer.");
        }

        private static async Task CopyBlob(IDurableOrchestrationContext context, string sourceDirectory, string destinationDirectory, string fileName)
        {
            await context.CallActivityAsync(nameof(CopyBlobActivity), new CopyBlobActivityPayload
            {
              FileName  = fileName,
              DestinationContainer = destinationDirectory,
              SourceContainer = sourceDirectory
            });
        }
    }
}