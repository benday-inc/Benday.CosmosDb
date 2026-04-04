namespace Benday.CosmosDb.SampleApp.Api.ServiceLayers;

public interface INoteAttachmentService
{
    Task AttachFileAsync(string tenantId, string noteId, string filename, Stream content);
    Task<List<string>> ListAttachmentsAsync(string tenantId, string noteId);
    Task<byte[]> DownloadAsync(string tenantId, string noteId, string filename);
    Task DeleteAttachmentAsync(string tenantId, string noteId, string filename);
    Task DeleteAllAttachmentsAsync(string tenantId, string noteId);
}
