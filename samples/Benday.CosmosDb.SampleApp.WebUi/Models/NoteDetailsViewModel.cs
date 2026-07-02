using Benday.CosmosDb.SampleApp.Api.DomainModels;

namespace Benday.CosmosDb.SampleApp.WebUi.Models;

public class NoteDetailsViewModel
{
    public Note Note { get; set; } = null!;
    public List<string> Attachments { get; set; } = new();
}
