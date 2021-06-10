using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using PhotoReview.Constants;

namespace PhotoReview.Functions
{
    public static class ReviewPhoto
    {
        [FunctionName(nameof(ApprovePhoto))]
        public static Task<IActionResult> ApprovePhoto(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Get), Route = null)]
            HttpRequest request,
            [DurableClient] IDurableOrchestrationClient durableClient) =>
            Handle(durableClient, request, Events.PhotoApprovedEvent);

        [FunctionName(nameof(RejectPhoto))]
        public static Task<IActionResult> RejectPhoto(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Get), Route = null)]
            HttpRequest request,
            [DurableClient] IDurableOrchestrationClient durableClient) =>
            Handle(durableClient, request, Events.PhotoRejectedEvent);

        private static async Task<IActionResult> Handle(IDurableOrchestrationClient durableClient, HttpRequest request, string eventToPublish)
        {
            string instanceId;

            if (!request.Query.TryGetValue(nameof(instanceId), out var instanceIdValue))
            {
                return new BadRequestObjectResult($"{nameof(instanceId)} is required.");
            }

            instanceId = instanceIdValue.ToString();

            var durableFunction = await durableClient.GetStatusAsync(instanceId);

            if (durableFunction.RuntimeStatus == OrchestrationRuntimeStatus.Unknown ||
                durableFunction.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
            {
                return new BadRequestObjectResult("Already reviewed.");
            }

            await durableClient.RaiseEventAsync(instanceId, eventToPublish);

            return new NoContentResult();
        }
    }
}
