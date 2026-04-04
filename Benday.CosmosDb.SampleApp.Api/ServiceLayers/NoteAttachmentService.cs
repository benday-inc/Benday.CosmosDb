using Benday.AzureStorage.Blobs;
using Benday.BlobStorage;
using Benday.CosmosDb.SampleApp.Api.DomainModels;
using Benday.CosmosDb.ServiceLayers;

namespace Benday.CosmosDb.SampleApp.Api.ServiceLayers;

public class NoteAttachmentService : INoteAttachmentService
{
    private readonly ITenantItemService<Note> _noteService;
    private readonly BlobBridge<Note> _blobBridge;

    public NoteAttachmentService(
        ITenantItemService<Note> noteService,
        IBlobRepository blobRepository)
    {
        _noteService = noteService;
        _blobBridge = new BlobBridge<Note>(blobRepository);
    }

    public async Task AttachFileAsync(string tenantId, string noteId, string filename, Stream content)
    {
        var note = await _noteService.GetByIdAsync(tenantId, noteId)
            ?? throw new InvalidOperationException($"Note '{noteId}' not found.");

        await _blobBridge.AttachAsync(note, filename, content);

        note.HasAttachment = true;
        await _noteService.SaveAsync(note);
    }

    public async Task<List<string>> ListAttachmentsAsync(string tenantId, string noteId)
    {
        var note = await _noteService.GetByIdAsync(tenantId, noteId)
            ?? throw new InvalidOperationException($"Note '{noteId}' not found.");

        var prefix = note.GetBlobPrefix();
        var filenames = new List<string>();

        await foreach (var blob in _blobBridge.ListAttachmentsAsync(note))
        {
            // Strip the prefix to get just the filename
            var name = blob.Name;
            if (name.StartsWith(prefix))
            {
                name = name.Substring(prefix.Length);
            }
            filenames.Add(name);
        }

        return filenames;
    }

    public async Task<byte[]> DownloadAsync(string tenantId, string noteId, string filename)
    {
        var note = await _noteService.GetByIdAsync(tenantId, noteId)
            ?? throw new InvalidOperationException($"Note '{noteId}' not found.");

        return await _blobBridge.DownloadAttachmentBytesAsync(note, filename);
    }

    public async Task DeleteAttachmentAsync(string tenantId, string noteId, string filename)
    {
        var note = await _noteService.GetByIdAsync(tenantId, noteId)
            ?? throw new InvalidOperationException($"Note '{noteId}' not found.");

        await _blobBridge.DeleteAttachmentAsync(note, filename);

        // Check if any attachments remain
        var hasRemaining = false;
        await foreach (var _ in _blobBridge.ListAttachmentsAsync(note))
        {
            hasRemaining = true;
            break;
        }

        note.HasAttachment = hasRemaining;
        await _noteService.SaveAsync(note);
    }

    public async Task DeleteAllAttachmentsAsync(string tenantId, string noteId)
    {
        var note = await _noteService.GetByIdAsync(tenantId, noteId)
            ?? throw new InvalidOperationException($"Note '{noteId}' not found.");

        await _blobBridge.DeleteAttachmentsAsync(note);

        note.HasAttachment = false;
        await _noteService.SaveAsync(note);
    }
}
