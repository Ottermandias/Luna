using Luna.Generators;

namespace Luna.Tests;

public class NamedEnumTests
{
    [Fact]
    public Task GenerateNamedEnum()
    {
        const string source = """
                              using Luna.Generators;

                              [NamedEnum]
                              public enum Test
                              {
                                  A = 0,
                                  B = 1,
                              }
                              """;

        return ModuleInitializer.VerifyCode<NamedEnumGenerator>(source);
    }

    [Fact]
    public Task GenerateCustomNamedEnum()
    {
        const string source = """
                              using Luna.Generators;

                              [NamedEnum(Method: "ToCustomName")]
                              public enum Test
                              {
                                  A = 0,
                                  B = 1,
                              }
                              """;

        return ModuleInitializer.VerifyCode<NamedEnumGenerator>(source);
    }

    [Fact]
    public Task GenerateComplexNamedEnum()
    {
        const string source = """
                              using Luna.Generators;
                              
                              namespace Complex.Test;
                              
                              [NamedEnum(Method: "ToComplexName", Utf16: false, Utf8: true, Unknown: "ERROR", Class: "TempClass", Namespace: "Test.Name.Space")]
                              public enum Test
                              {
                                  [Name(Omit: true)]
                                  A = 0,
                                  
                                  [Name("Not B")]
                                  B = 1,
                                  
                                  C = 2,
                                  
                                  [Name]
                                  D = 3,
                                  
                                  [Name("Not E", true)]
                                  E = 4,
                                  
                                  [Name(Omit: false, Name: "Not F")]
                                  F = 5,
                              }
                              """;

        return ModuleInitializer.VerifyCode<NamedEnumGenerator>(source);
    }
}
