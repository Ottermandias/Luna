using Luna.Generators;

namespace Luna.Tests;

[StrongType<uint>(Flags: (StrongTypeFlag)0xFFFFFFFFFFFFFFFFul)]
public readonly partial struct TestId
{
    public override string ToString()
        => $"TestID: {Value}";
}

public class StrongTypeTests
{
    [Fact]
    public void UsageTest()
    {
        Assert.Equal($"TestID: 999",    new TestId(999).ToString());
        Assert.Equal(999.ToString(),    new TestId(999).ToString("D3", null));
        Assert.Equal(999.ToString("D4"),    new TestId(999).ToString("D4", null));
        Assert.Equal(new TestId(5 + 6), new TestId(5) + new TestId(6));
        Assert.Equal(new TestId(5 + 6), new TestId(5) + 6u);
        Assert.Equal(new TestId(5 + 6), 6u + new TestId(5));
        Assert.Equal(new TestId(5 - 3), new TestId(5) - 3u);
        Assert.True(new TestId(5) > new TestId(2));
        Assert.True(new TestId(5) >= new TestId(2));
        Assert.True(new TestId(5) <= new TestId(3 + 2));
        Assert.True(new TestId(5) < new TestId(6));
        Assert.True(new TestId(5) == new TestId(3 + 2));
        Assert.True(new TestId(5) != new TestId(3));
        Assert.True(new TestId(5) != 3u);
        Assert.True(new TestId(5) == 5u);
    }

    [Fact]
    public Task GenerateStrongType()
    {
        const string source = """
                              using Luna.Generators;

                              namespace Luna.Test;

                              [StrongType<uint>()]
                              public partial struct Test;
                              """;

        return ModuleInitializer.VerifyCode<StrongTypeGenerator>(source);
    }
}
