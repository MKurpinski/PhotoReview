using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using PhotoReview.Constants;
using PhotoReview.Payloads;

namespace PhotoReview.Functions
{
    public class CopyBlobActivity
    {
        private const string SOURCE_BINDING_EXPRESSION = "{data." + nameof(CopyBlobActivityPayload.SourceContainer) + "}" + "/{data." +  nameof(CopyBlobActivityPayload.FileName) + "}";
        private const string DESTINATION_BINDING_EXPRESSION = "{data." + nameof(CopyBlobActivityPayload.DestinationContainer) + "}" + "/{data." +  nameof(CopyBlobActivityPayload.FileName) + "}" + "-{rand-guid}";

        [FunctionName(nameof(CopyBlobActivity))]
        public static async Task Run(
            [ActivityTrigger] CopyBlobActivityPayload payload, 
            [Blob(SOURCE_BINDING_EXPRESSION, FileAccess.Read, Connection = ConnectionStrings.PhotosConnectionString)] Stream sourceBlob, 
            [Blob(DESTINATION_BINDING_EXPRESSION, FileAccess.Write, Connection = ConnectionStrings.PhotosConnectionString)] Stream destinationBlob)
        {
            await sourceBlob.CopyToAsync(destinationBlob);
        }
    }
}
