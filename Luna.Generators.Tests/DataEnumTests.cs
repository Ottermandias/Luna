namespace Luna.Generators.Tests;

public class DataEnumTests
{
    [Fact]
    public Task GenerateDataEnum()
    {
        const string source = """
                              using Luna.Generators;

                              [DataEnum(typeof(System.Type, "ToType"))]
                              public enum Test
                              {
                                  [Data("ToType", "typeof(int)"]
                                  A = 0,
                                  [Data("ToType", "typeof(string)"]
                                  B = 1,
                                  
                                  C = 2,
                                  
                                  [Data("ToType", "", true]
                                  D = 3
                              }
                              """;

        return ModuleInitializer.VerifyCode<DataEnumGenerator>(source);
    }
}
