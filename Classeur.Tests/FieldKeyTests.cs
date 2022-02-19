using System;
using System.Collections.Generic;
using System.Linq;
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
    public void Invalid(string name) => Assert.Throws<ArgumentException>(() => new FieldKey(name));

    [Theory]
    [InlineData("_aB_c", "_ab_c")]
    [InlineData("a##c", "a_c")]
    [InlineData("3abc", "_3abc")]
    [InlineData("a3c", "a3c")]
    [InlineData("_123456789_123456789_123456789_123456789", "_123456789_123456789_123456789_1")]
    [InlineData("_1234##6789_123456789_123456789_123456789", "_1234_6789_123456789_123456789_1")]
    public void Generation(string s, string expected) => Assert.Equal(new FieldKey(expected), actual: FieldKey.For(s));

    [Theory]
    [InlineData("abc", "abc", new string[0])]
    [InlineData("abc", "abc2", new[] {"abc", "abc1"})]
    [InlineData("a1", "a111", new[] {"a1", "a11", "a12", "a13", "a14", "a15", "a16", "a17", "a18", "a19", "a110" })]
    [InlineData("_123456789_123456789_123456789_1",
                "_123456789_123456789_123456789_2",
                new[] { "_123456789_123456789_123456789_1" })]
    public void Uniqueness(string key, string expected, string[] keys)
    {
        HashSet<FieldKey> set = keys.Select(k => new FieldKey(k)).ToHashSet();

        FieldKey unique = new FieldKey(key).MakeUniqueAmong(set);

        Assert.DoesNotContain(unique, set);
        Assert.Equal(new FieldKey(expected), actual: unique);
    }
}
