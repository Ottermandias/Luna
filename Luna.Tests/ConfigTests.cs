using Luna.Generators;

namespace Luna.Tests;

public partial class ConfigTests
{
    private float _oldVelocity;

    private partial class PrivateConfig
    {
        public int SaveCalled  = 0;
        public int Save2Called = 0;

        [ConfigProperty]
        private int _intProp;

        [ConfigProperty(SkipSave = true)]
        private string _textProp = string.Empty;

        [ConfigProperty(PropertyName = "Velocity", SaveMethodName = "Save2", EventName = "VelocityChanged")]
        private float _floatProp;

        public int ReadInt
            => _intProp;

        public float ReadFloat
            => _floatProp;

        public void Save()
        {
            ++SaveCalled;
            Velocity = Velocity;
        }

        public void Save2()
        {
            ++Save2Called;
        }

        partial void OnTextPropChanging(string newValue, string oldValue)
        {
            IntProp += 1;
        }
    }

    [Fact]
    public void TestPrivateConfig()
    {
        var counter = 0;
        _oldVelocity = 0f;
        var config = new PrivateConfig();
        config.VelocityChanged += (newValue, oldValue) =>
        {
            Assert.Equal(config.Velocity, newValue);
            Assert.Equal(_oldVelocity,    oldValue);
            ++counter;
        };

        Assert.Equal(0, config.ReadInt);
        Assert.Equal(0, config.ReadFloat);
        Assert.Equal(0, config.SaveCalled);
        Assert.Equal(0, config.Save2Called);
        Assert.Equal(0, config.IntProp);
        Assert.Equal(0, config.Velocity);

        config.IntProp = 5;
        Assert.Equal(5, config.IntProp);
        Assert.Equal(5, config.ReadInt);
        Assert.Equal(1, config.SaveCalled);
        config.IntProp = 5;
        Assert.Equal(5, config.IntProp);
        Assert.Equal(5, config.ReadInt);
        Assert.Equal(1, config.SaveCalled);
        config.IntProp = 10;
        Assert.Equal(10, config.IntProp);
        Assert.Equal(10, config.ReadInt);
        Assert.Equal(2,  config.SaveCalled);


        Assert.Equal(0, config.Save2Called);
        config.Velocity = 3.14f;
        _oldVelocity    = 3.14f;
        Assert.Equal(3.14f, config.Velocity);
        Assert.Equal(3.14f, config.ReadFloat);
        Assert.Equal(1,     config.Save2Called);
        config.Velocity = 3.14f;
        Assert.Equal(3.14f, config.Velocity);
        Assert.Equal(3.14f, config.ReadFloat);
        Assert.Equal(1,     config.Save2Called);
        config.Velocity = -2.7f;
        _oldVelocity    = -2.7f;
        Assert.Equal(-2.7f, config.Velocity);
        Assert.Equal(-2.7f, config.ReadFloat);
        Assert.Equal(2,     config.Save2Called);
        Assert.Equal(2,     counter);

        Assert.Equal(string.Empty, config.TextProp);
        config.TextProp = "Hello";
        Assert.Equal("Hello", config.TextProp);
        Assert.Equal(11, config.IntProp);
        Assert.Equal(3, config.SaveCalled);
    }

    [Fact]
    public Task GenerateConfigTests()
    {
        const string source = """
                              using Luna.Generators;

                              public partial class Config
                              {
                                  [ConfigProperty(EventName = "IntChanged"]
                                  private int _intProp;
                              }
                              """;

        return ModuleInitializer.VerifyCode<ConfigPropertyGenerator>(source);
    }
}
