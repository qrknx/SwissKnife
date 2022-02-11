using System;
using Classeur.Core.CustomizableStructure;
using Xunit;

namespace Classeur.Tests;

public class FieldKeyTests
{
    [Theory]
    [InlineData("_")]
    [InlineData("_1")]
    [InlineData("a_1B")]
    [InlineData("я")]
    public void Valid(string name)
    {
        FieldKey key = new(name);

        Assert.Equal(expected: name, key.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("b@c")]
    [InlineData("abcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghij")]
    public void Invalid(string name)
    {
        Assert.Throws<ArgumentException>(() => new FieldKey(name));
    }
}
