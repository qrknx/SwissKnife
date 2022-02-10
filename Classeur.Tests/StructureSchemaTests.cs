using System.Linq;
using Classeur.Core;
using Classeur.Core.CustomizableStructure;
using Xunit;

namespace Classeur.Tests;

public class StructureSchemaTests
{
    [Fact]
    public void Smoke_Empty()
    {
        StructureSchema schema = new(IncoherentId.Generate());

        Assert.Empty(schema.Changes);
        Assert.Equal(0, schema.Latest.Version);
        Assert.Equal(0, schema.Latest.TotalFields);
        Assert.Empty(schema.Latest.Fields);
    }
}
