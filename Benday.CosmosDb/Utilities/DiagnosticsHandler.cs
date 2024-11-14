using Microsoft.Azure.Cosmos;

namespace Benday.CosmosDb.Utilities;


public class DiagnosticsHandler : RequestHandler
{
    public override async Task<ResponseMessage> SendAsync(RequestMessage request, CancellationToken cancellationToken)
    {
        ResponseMessage response = await base.SendAsync(request, cancellationToken);
        Console.WriteLine($"Diagnostics: {response.Diagnostics.ToString()}");
        return response;
    }
}