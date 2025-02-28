using Benday.CosmosDb.DomainModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.CosmosDb.SampleApp.Api.DomainModels;

public class Note : OwnedItemBase
{
    public string Text { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
}
