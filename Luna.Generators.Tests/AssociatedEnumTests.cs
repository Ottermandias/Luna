namespace Luna.Generators.Tests;

public class AssociatedEnumTests
{
    [Fact]
    public Task GenerateAssociatedEnum()
    {
        const string source = """
                              using Luna.Generators;

                              [AssociatedEnum(typeof(Test2))]
                              public enum Test
                              {
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
}
