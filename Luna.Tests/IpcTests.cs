using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Luna.Generators;
using Moq;

namespace Luna.Tests;

public enum ApiResultCode
{
    Success,
    UnspecifiedError,
}

public readonly record struct ApiVersion(uint Major, uint Minor)
{
    public static explicit operator (uint Major, uint Minor)(ApiVersion version)
        => (version.Major, version.Minor);

    public static explicit operator ApiVersion((uint Major, uint Minor) version)
        => new(version.Major, version.Minor);
}

public interface ISampleIpcService
{
    void DoMoreWork();

    public static ISampleIpcService Bind(object obj)
        => obj as ISampleIpcService ?? throw new NotImplementedException();
}

public partial interface ILunaSampleIpc
{
    [EraseType<(uint, uint)>]
    public ApiVersion Version
    {
        [Ipc("Luna.Tests.GetApiVersion")]
        get;
    }

    public nint this[[EraseType] TestId id]
    {
        [Ipc("Luna.Tests.GetItemById")]
        get;
        [Ipc("Luna.Tests.SetItemById")]
        set;
    }

    [Ipc("Luna.Tests.OnApiStarted")]
    public event Action<uint, uint> OnStarted;

    [Ipc("Luna.Tests.OnApiStopping")]
    public event Action OnStopping;

    [Ipc("Luna.Tests.DoSomeWork")]
    [return: EraseType]
    public ApiResultCode DoSomeWork(bool parameter, [EraseType<(uint, uint)>] ApiVersion version);

    [Ipc("Luna.Tests.GetSomeService")]
    [return: EraseType(MarshalBack = nameof(ISampleIpcService.Bind))]
    public ISampleIpcService GetSomeService();

    [GeneratedIpcSubscriber]
    public static partial ILunaSampleIpc Create(IDalamudPluginInterface pluginInterface);

    [GeneratedIpcSubscriber(LazySubscribers = true)]
    public static partial ILunaSampleIpc CreateLazy(IDalamudPluginInterface pluginInterface);
    
    [GeneratedIpcProvider]
    public static partial IDisposable CreateProvider(IDalamudPluginInterface pluginInterface, ILunaSampleIpc implementation);
}

public class IpcTests
{
    [Fact]
    public void EagerSubscriberTest()
    {
        var pluginInterface         = new Mock<IDalamudPluginInterface>();
        var getApiVersionSubscriber = new Mock<ICallGateSubscriber<(uint, uint)>>();
        var setItemByIdSubscriber   = new Mock<ICallGateSubscriber<uint, nint, object?>>();
        var onApiStartedSubscriber  = new Mock<ICallGateSubscriber<uint, uint, object?>>();
        pluginInterface.Setup(pi => pi.GetIpcSubscriber<(uint, uint)>("Luna.Tests.GetApiVersion"))
            .Returns(() => getApiVersionSubscriber.Object);
        pluginInterface.Setup(pi => pi.GetIpcSubscriber<uint, nint, object?>("Luna.Tests.SetItemById"))
            .Returns(() => setItemByIdSubscriber.Object);
        pluginInterface.Setup(pi => pi.GetIpcSubscriber<uint, uint, object?>("Luna.Tests.OnApiStarted"))
            .Returns(() => onApiStartedSubscriber.Object);
        getApiVersionSubscriber.Setup(sub => sub.InvokeFunc())
            .Returns((42, 9001));
        setItemByIdSubscriber.Setup(sub => sub.InvokeFunc(42, unchecked((nint)0x077E25A2EC001)))
            .Returns((object?)null);
        onApiStartedSubscriber.Setup(sub => sub.Subscribe(It.IsAny<Action<uint, uint>>()))
            .Callback<Action<uint, uint>>(action => action(9, 1));

        var client = ILunaSampleIpc.Create(pluginInterface.Object);
        pluginInterface.Verify(pi => pi.GetIpcSubscriber<(uint, uint)>("Luna.Tests.GetApiVersion"),         Times.Once());
        pluginInterface.Verify(pi => pi.GetIpcSubscriber<uint, nint>("Luna.Tests.GetItemById"),             Times.Once());
        pluginInterface.Verify(pi => pi.GetIpcSubscriber<uint, nint, object?>("Luna.Tests.SetItemById"),    Times.Once());
        pluginInterface.Verify(pi => pi.GetIpcSubscriber<uint, uint, object?>("Luna.Tests.OnApiStarted"),   Times.Once());
        pluginInterface.Verify(pi => pi.GetIpcSubscriber<object?>("Luna.Tests.OnApiStopping"),              Times.Once());
        pluginInterface.Verify(pi => pi.GetIpcSubscriber<bool, (uint, uint), int>("Luna.Tests.DoSomeWork"), Times.Once());
        pluginInterface.Verify(pi => pi.GetIpcSubscriber<object>("Luna.Tests.GetSomeService"),              Times.Once());
        
        Assert.Equal(new ApiVersion(42, 9001), client.Version);
        
        client[new TestId(42)] = unchecked((nint)0x077E25A2EC001);
        setItemByIdSubscriber.Verify(sub => sub.InvokeAction(42, unchecked((nint)0x077E25A2EC001)), Times.Once());
        
        var capturedMajor = uint.MaxValue;
        var capturedMinor = uint.MaxValue;
        client.OnStarted += (major, minor) =>
        {
            capturedMajor = major;
            capturedMinor = minor;
        };
        Assert.Equal(9u, capturedMajor);
        Assert.Equal(1u, capturedMinor);
    }
    
    [Fact]
    public void LazySubscriberTest()
    {
        var pluginInterface         = new Mock<IDalamudPluginInterface>();
        var getApiVersionSubscriber = new Mock<ICallGateSubscriber<(uint, uint)>>();
        pluginInterface.Setup(pi => pi.GetIpcSubscriber<(uint, uint)>("Luna.Tests.GetApiVersion"))
            .Returns(() => getApiVersionSubscriber.Object);
        getApiVersionSubscriber.Setup(sub => sub.InvokeFunc())
            .Returns((42, 9001));

        var client = ILunaSampleIpc.CreateLazy(pluginInterface.Object);
        pluginInterface.Verify(pi => pi.GetIpcSubscriber<(uint, uint)>("Luna.Tests.GetApiVersion"), Times.Never());
        
        Assert.Equal(new ApiVersion(42, 9001), client.Version);
    }
    
    [Fact]
    public void ProviderTest()
    {
        var pluginInterface       = new Mock<IDalamudPluginInterface>();
        var implementation        = new Mock<ILunaSampleIpc>();
        var getApiVersionProvider = new Mock<ICallGateProvider<(uint, uint)>>();
        var setItemByIdProvider   = new Mock<ICallGateProvider<uint, nint, object?>>();
        var onApiStartedProvider  = new Mock<ICallGateProvider<uint, uint, object?>>();
        pluginInterface.Setup(pi => pi.GetIpcProvider<(uint, uint)>("Luna.Tests.GetApiVersion"))
            .Returns(() => getApiVersionProvider.Object);
        pluginInterface.Setup(pi => pi.GetIpcProvider<uint, nint>("Luna.Tests.GetItemById"))
            .Returns(Mock.Of<ICallGateProvider<uint, nint>>);
        pluginInterface.Setup(pi => pi.GetIpcProvider<uint, nint, object?>("Luna.Tests.SetItemById"))
            .Returns(() => setItemByIdProvider.Object);
        pluginInterface.Setup(pi => pi.GetIpcProvider<uint, uint, object?>("Luna.Tests.OnApiStarted"))
            .Returns(() => onApiStartedProvider.Object);
        pluginInterface.Setup(pi => pi.GetIpcProvider<object?>("Luna.Tests.OnApiStopping"))
            .Returns(Mock.Of<ICallGateProvider<object?>>);
        pluginInterface.Setup(pi => pi.GetIpcProvider<bool, (uint, uint), int>("Luna.Tests.DoSomeWork"))
            .Returns(Mock.Of<ICallGateProvider<bool, (uint, uint), int>>);
        pluginInterface.Setup(pi => pi.GetIpcProvider<object?>("Luna.Tests.GetSomeService"))
            .Returns(Mock.Of<ICallGateProvider<object?>>);
        implementation.SetupGet(impl => impl.Version)
            .Returns(new ApiVersion(42, 9001));
        implementation.SetupAdd(impl => impl.OnStarted += It.IsAny<Action<uint, uint>>())
            .Callback<Action<uint, uint>>(handler => handler(9, 1));
        getApiVersionProvider.Setup(prov => prov.RegisterFunc(It.IsAny<Func<(uint, uint)>>()))
            .Callback<Func<(uint, uint)>>(impl => Assert.Equal((42u, 9001u), impl()));
        setItemByIdProvider.Setup(prov => prov.RegisterAction(It.IsAny<Action<uint, nint>>()))
            .Callback<Action<uint, nint>>(impl => impl(42, unchecked((nint)0x077E25A2EC001)));

        using (ILunaSampleIpc.CreateProvider(pluginInterface.Object, implementation.Object))
        {
            getApiVersionProvider.Verify(prov => prov.RegisterFunc(It.IsAny<Func<(uint, uint)>>()), Times.Once());
            setItemByIdProvider.Verify(prov => prov.RegisterAction(It.IsAny<Action<uint, nint>>()), Times.Once());
            implementation.VerifySet(impl => impl[new TestId(42)] =  unchecked((nint)0x077E25A2EC001));
            implementation.VerifyAdd(impl => impl.OnStarted       += It.IsAny<Action<uint, uint>>(), Times.Once());
            onApiStartedProvider.Verify(prov => prov.SendMessage(9, 1));
        }

        getApiVersionProvider.Verify(prov => prov.UnregisterFunc(), Times.Once());
        setItemByIdProvider.Verify(prov => prov.UnregisterAction(), Times.Once());
        implementation.VerifyRemove(impl => impl.OnStarted -= It.IsAny<Action<uint, uint>>(), Times.Once());
    }
}
