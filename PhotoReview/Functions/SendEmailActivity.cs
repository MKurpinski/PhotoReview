using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using PhotoReview.Constants;
using PhotoReview.Payloads;
using SendGrid.Helpers.Mail;

namespace PhotoReview.Functions
{
    public static class SendEmailActivity
    {
        private const string HTML_MIME_TYPE = "text/html";

        [FunctionName(nameof(SendEmailActivity))]
        public static async Task Run(
            [ActivityTrigger] SendEmailActivityPayload payload,
            [SendGrid(ApiKey = ConnectionStrings.SendGridApiKey)] IAsyncCollector<SendGridMessage> messageCollector)
        {
            var message = new SendGridMessage();
            message.AddTo(payload.To);
            message.AddContent(HTML_MIME_TYPE, payload.Body);
            message.SetFrom(payload.From);
            message.SetSubject(payload.Subject);

            await messageCollector.AddAsync(message);
        }
    }
}
