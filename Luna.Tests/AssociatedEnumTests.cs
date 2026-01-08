using Luna.Generators;

namespace Luna.Tests;

public class AssociatedEnumTests
{
    [AssociatedEnum<Test2>(ForwardDefaultValue: Test2.A, BackwardMethod: "BackToA", BackwardDefaultValue: Test.C)]
    [AssociatedEnum<Test3>]
    public enum Test
    {
        [Associate<Test2>(Test2.B)]
        A = 0,
        [Associate<Test2>(Test2.A)]
        B = 1,
        [Associate<Test2>(false)]
        C,
        D,
    }

    [Flags]
    public enum Test2
    {
        A = 0x01,
        B = 0x02,
        D,
    }

    [Flags]
    public enum Test3
    {
        A,
        B,
        C,
        D,
    }

    [Fact]
    public void TestUsage()
    {
        Assert.Equal(Test2.A, Test.B.ToTest2());
        Assert.Equal(Test2.B, Test.A.ToTest2());
        Assert.Equal(Test2.D, Test.D.ToTest2());
        Assert.Equal(Test2.A, ((Test) 999).ToTest2());

        Assert.Equal(Test.A, Test2.B.BackToA());
        Assert.Equal(Test.B, Test2.A.BackToA());
        Assert.Equal(Test.D, Test2.D.BackToA());
        Assert.Equal(Test.C, ((Test2) 999).BackToA());

        Assert.Equal(Test3.A, Test.A.ToTest3());
        Assert.Equal(Test3.B, Test.B.ToTest3());
        Assert.Equal(Test3.C, Test.C.ToTest3());
        Assert.Equal(Test3.D, Test.D.ToTest3());
        Assert.Equal(Test3.A, ((Test) 999).ToTest3());
    }

    [Fact]
    public Task GenerateAssociatedEnum()
    {
        const string source = """
                              using Luna.Generators;

                              [AssociatedEnum<Test2>()]
                              public enum Test
                              {
                                  [Associate<Test2>(Test2.B)]
                                  A = 0,
                                  B = 1,
                              }

                              [Flags]
                              public enum Test2
                              {
                                  A = 0x01,
                                  B = 0x02,
                              }
                              """;

        return ModuleInitializer.VerifyCode<AssociatedEnumGenerator>(source);
    }

    [Fact]
    public Task GenerateComplexAssociatedEnum()
    {
        const string source = """
                              using Luna.Generators;

                              [AssociatedEnum<Test2>(ForwardDefaultValue: Test2.A, BackwardMethod: "BackToA", BackwardDefaultValue: Test.C)]
                              public enum Test
                              {
                                  [Associate<Test2>(Test2.B)]
                                  A = 0,
                                  [Associate<Test2>(Test2.A)]
                                  B = 1,
                                  [Associate<Test2>(false)]
                                  C,
                                  D,
                              }

                              [Flags]
                              public enum Test2
                              {
                                  A = 0x01,
                                  B = 0x02,
                                  D,
                              }
                              """;

        return ModuleInitializer.VerifyCode<AssociatedEnumGenerator>(source);
    }

    [Fact]
    public Task GenerateMultipleAssociatedEnum()
    {
        const string source = """
                              using Luna.Generators;

                              [AssociatedEnum<Test2>(ForwardDefaultValue: Test2.A, BackwardMethod: "BackToA", BackwardDefaultValue: Test.C)]
                              [AssociatedEnum<Test3>]
                              public enum Test
                              {
                                  [Associate<Test2>(Test2.B)]
                                  A = 0,
                                  [Associate<Test2>(Test2.A)]
                                  B = 1,
                                  [Associate<Test2>(false)]
                                  C,
                                  D,
                              }

                              [Flags]
                              public enum Test2
                              {
                                  A = 0x01,
                                  B = 0x02,
                                  D,
                              }

                              [Flags]
                              public enum Test3
                              {
                                  A,
                                  B,
                                  C,
                                  D,
                              }
                              """;

        return ModuleInitializer.VerifyCode<AssociatedEnumGenerator>(source);
    }
}
