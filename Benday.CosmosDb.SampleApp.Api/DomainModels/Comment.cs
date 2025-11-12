using Benday.CosmosDb.DomainModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.CosmosDb.SampleApp.Api.DomainModels;

/// <summary>
/// Represents a comment that can be attached to a Note or other entity.
/// Demonstrates the ParentedItemBase pattern.
/// </summary>
public class Comment : ParentedItemBase
{
    private string _parentDiscriminator = string.Empty;

    /// <summary>
    /// Discriminator/type of the parent entity
    /// </summary>
    public override string ParentDiscriminator
    {
        get => _parentDiscriminator;
        set => _parentDiscriminator = value;
    }

    public string Text { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
