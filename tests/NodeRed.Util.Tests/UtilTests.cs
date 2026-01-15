// ============================================================
// Tests for NodeRed.Util.Util
// These tests are translated from Node-RED test files
// ============================================================

using Xunit;
using NodeRed.Util;

namespace NodeRed.Util.Tests;

public class UtilTests
{
    [Fact]
    public void GenerateId_ReturnsValidHexString()
    {
        // Act
        var id = Util.GenerateId();

        // Assert
        Assert.NotNull(id);
        Assert.Equal(16, id.Length);
        Assert.True(System.Text.RegularExpressions.Regex.IsMatch(id, "^[0-9a-f]{16}$"));
    }

    [Fact]
    public void GenerateId_ReturnsUniqueIds()
    {
        // Act
        var id1 = Util.GenerateId();
        var id2 = Util.GenerateId();

        // Assert
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void EnsureString_WithString_ReturnsString()
    {
        // Act
        var result = Util.EnsureString("hello");

        // Assert
        Assert.Equal("hello", result);
    }

    [Fact]
    public void EnsureString_WithByteArray_ReturnsString()
    {
        // Arrange
        var bytes = System.Text.Encoding.UTF8.GetBytes("hello");

        // Act
        var result = Util.EnsureString(bytes);

        // Assert
        Assert.Equal("hello", result);
    }

    [Fact]
    public void EnsureString_WithNumber_ReturnsString()
    {
        // Act
        var result = Util.EnsureString(123);

        // Assert
        Assert.Equal("123", result);
    }

    [Fact]
    public void EnsureBuffer_WithString_ReturnsBytes()
    {
        // Act
        var result = Util.EnsureBuffer("hello");

        // Assert
        Assert.Equal(System.Text.Encoding.UTF8.GetBytes("hello"), result);
    }

    [Fact]
    public void EnsureBuffer_WithByteArray_ReturnsSameArray()
    {
        // Arrange
        var bytes = new byte[] { 1, 2, 3 };

        // Act
        var result = Util.EnsureBuffer(bytes);

        // Assert
        Assert.Same(bytes, result);
    }

    [Fact]
    public void CompareObjects_WithEqualStrings_ReturnsTrue()
    {
        Assert.True(Util.CompareObjects("hello", "hello"));
    }

    [Fact]
    public void CompareObjects_WithDifferentStrings_ReturnsFalse()
    {
        Assert.False(Util.CompareObjects("hello", "world"));
    }

    [Fact]
    public void CompareObjects_WithEqualByteArrays_ReturnsTrue()
    {
        var a = new byte[] { 1, 2, 3 };
        var b = new byte[] { 1, 2, 3 };
        Assert.True(Util.CompareObjects(a, b));
    }

    [Fact]
    public void CompareObjects_WithDifferentByteArrays_ReturnsFalse()
    {
        var a = new byte[] { 1, 2, 3 };
        var b = new byte[] { 1, 2, 4 };
        Assert.False(Util.CompareObjects(a, b));
    }

    [Fact]
    public void CompareObjects_WithNull_ReturnsFalse()
    {
        Assert.False(Util.CompareObjects("hello", null));
        Assert.False(Util.CompareObjects(null, "hello"));
    }

    [Fact]
    public void CompareObjects_WithBothNull_ReturnsTrue()
    {
        // Both null returns true via ReferenceEquals check (obj1 === obj2)
        Assert.True(Util.CompareObjects(null, null));
    }

    [Fact]
    public void CreateError_CreatesExceptionWithCode()
    {
        // Act
        var error = Util.CreateError("TEST_CODE", "Test message");

        // Assert
        Assert.Equal("TEST_CODE", error.Code);
        Assert.Equal("Test message", error.Message);
    }

    [Fact]
    public void NormalisePropertyExpression_SimpleProperty_ReturnsParts()
    {
        // Act
        var result = Util.NormalisePropertyExpression("foo");

        // Assert
        Assert.Single(result);
        Assert.Equal("foo", result[0]);
    }

    [Fact]
    public void NormalisePropertyExpression_DottedProperty_ReturnsParts()
    {
        // Act
        var result = Util.NormalisePropertyExpression("foo.bar");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("foo", result[0]);
        Assert.Equal("bar", result[1]);
    }

    [Fact]
    public void NormalisePropertyExpression_ArrayIndex_ReturnsParts()
    {
        // Act
        var result = Util.NormalisePropertyExpression("foo[0]");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("foo", result[0]);
        Assert.Equal(0, result[1]);
    }

    [Fact]
    public void NormalisePropertyExpression_QuotedProperty_ReturnsParts()
    {
        // Act
        var result = Util.NormalisePropertyExpression("foo[\"bar\"]");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("foo", result[0]);
        Assert.Equal("bar", result[1]);
    }

    [Fact]
    public void NormalisePropertyExpression_EmptyString_ThrowsException()
    {
        // Act & Assert
        var ex = Assert.Throws<NodeRedException>(() => Util.NormalisePropertyExpression(""));
        Assert.Equal("INVALID_EXPR", ex.Code);
    }

    [Fact]
    public void NormaliseNodeTypeName_WithDashes_ReturnsCamelCase()
    {
        // Act
        var result = Util.NormaliseNodeTypeName("my-node-type");

        // Assert
        Assert.Equal("myNodeType", result);
    }

    [Fact]
    public void NormaliseNodeTypeName_WithSpaces_ReturnsCamelCase()
    {
        // Act
        var result = Util.NormaliseNodeTypeName("my node type");

        // Assert
        Assert.Equal("myNodeType", result);
    }

    [Fact]
    public void ParseContextStore_WithStore_ReturnsStorAndKey()
    {
        // Act
        var result = Util.ParseContextStore("#:(file)::foo");

        // Assert
        Assert.Equal("file", result.Store);
        Assert.Equal("foo", result.Key);
    }

    [Fact]
    public void ParseContextStore_WithoutStore_ReturnsKeyOnly()
    {
        // Act
        var result = Util.ParseContextStore("foo");

        // Assert
        Assert.Null(result.Store);
        Assert.Equal("foo", result.Key);
    }

    [Fact]
    public void CloneMessage_ClonesPayload()
    {
        // Arrange
        var msg = new FlowMessage
        {
            Payload = "test payload",
            Topic = "test topic"
        };

        // Act
        var clone = Util.CloneMessage(msg);

        // Assert
        Assert.NotNull(clone);
        Assert.NotSame(msg, clone);
        Assert.Equal("test payload", clone!.Payload?.ToString());
        Assert.Equal("test topic", clone.Topic);
    }

    [Fact]
    public void CloneMessage_PreservesReqRes()
    {
        // Arrange
        var reqObj = new object();
        var resObj = new object();
        var msg = new FlowMessage
        {
            Payload = "test",
            Req = reqObj,
            Res = resObj
        };

        // Act
        var clone = Util.CloneMessage(msg);

        // Assert
        Assert.NotNull(clone);
        Assert.Same(reqObj, clone!.Req);
        Assert.Same(resObj, clone.Res);
        Assert.Same(reqObj, msg.Req); // Original should also retain refs
        Assert.Same(resObj, msg.Res);
    }

    [Fact]
    public void CloneMessage_WithNull_ReturnsNull()
    {
        // Act
        var result = Util.CloneMessage(null);

        // Assert
        Assert.Null(result);
    }
}
