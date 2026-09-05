using Xunit;

namespace Piml.Tests;

public class NodeModelTests
{
    [Fact]
    public void Object_keeps_insertion_order()
    {
        var o = new PimlObject { { "b", 1 }, { "a", 2 }, { "c", 3 } };
        Assert.Equal(new[] { "b", "a", "c" }, o.Keys);
        Assert.Equal(3, o.Count);
    }

    [Fact]
    public void Object_Add_rejects_duplicate_key()
    {
        var o = new PimlObject { { "a", 1 } };
        Assert.Throws<ArgumentException>(() => o.Add("a", 2));
    }

    [Fact]
    public void Object_Set_replaces_in_place_and_appends_new()
    {
        var o = new PimlObject { { "a", 1 }, { "b", 2 } };
        o.Set("a", "x");
        o["c"] = true;
        Assert.Equal(new[] { "a", "b", "c" }, o.Keys);
        Assert.Equal("x", ((PimlString)o["a"]).Value);
        Assert.True(((PimlBoolean)o["c"]).Value);
    }

    [Fact]
    public void Object_Remove_drops_key_and_order_entry()
    {
        var o = new PimlObject { { "a", 1 }, { "b", 2 } };
        Assert.True(o.Remove("a"));
        Assert.False(o.Remove("zzz"));
        Assert.Equal(new[] { "b" }, o.Keys);
        Assert.False(o.ContainsKey("a"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("a(b")]
    [InlineData("a)b")]
    public void Object_rejects_invalid_keys(string key)
    {
        var o = new PimlObject();
        Assert.Throws<ArgumentException>(() => o.Add(key, 1));
    }

    [Fact]
    public void Implicit_conversions_produce_expected_kinds()
    {
        PimlNode s = "text";
        PimlNode i = 42;
        PimlNode l = 42L;
        PimlNode d = 1.5;
        PimlNode b = false;
        Assert.Equal(PimlNodeKind.String, s.Kind);
        Assert.Equal(PimlNodeKind.Integer, i.Kind);
        Assert.Equal(PimlNodeKind.Integer, l.Kind);
        Assert.Equal(PimlNodeKind.Float, d.Kind);
        Assert.Equal(PimlNodeKind.Boolean, b.Kind);
        Assert.Same(PimlNull.Instance, PimlNode.Null);
    }

    [Fact]
    public void Null_string_converts_to_PimlNull()
    {
        string? none = null;
        PimlNode n = none!;
        Assert.Equal(PimlNodeKind.Null, n.Kind);
    }

    [Fact]
    public void DeepEquals_compares_structurally()
    {
        var a = new PimlObject { { "x", 1 }, { "list", new PimlArray { "p", 2.5, PimlNull.Instance } } };
        var b = new PimlObject { { "list", new PimlArray { "p", 2.5, PimlNull.Instance } }, { "x", 1 } };
        var c = new PimlObject { { "x", 1 }, { "list", new PimlArray { "p", 2.5 } } };
        Assert.True(PimlNode.DeepEquals(a, b));   // key order does not matter for equality
        Assert.False(PimlNode.DeepEquals(a, c));
        Assert.False(PimlNode.DeepEquals(new PimlInteger(1), new PimlFloat(1.0)));
        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Array_behaves_like_a_list()
    {
        var arr = new PimlArray { 1, 2 };
        arr.Add("three");
        arr.Insert(0, PimlNull.Instance);
        Assert.Equal(4, arr.Count);
        Assert.Equal(PimlNodeKind.Null, arr[0].Kind);
        Assert.Equal(PimlNodeKind.String, arr[3].Kind);
        arr.RemoveAt(0);
        Assert.Equal(PimlNodeKind.Integer, arr[0].Kind);
        Assert.Equal(PimlNodeKind.Array, arr.Kind);
    }
}
