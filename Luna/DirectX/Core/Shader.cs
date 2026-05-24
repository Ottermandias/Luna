using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Luna.DirectX;

/// <summary> A Direct3D shader. </summary>
/// <param name="blob"> The shader blob. </param>
/// <param name="description"> A description of this shader, for debugging and logging purposes. </param>
public abstract class Shader<T>(byte[] blob, string description) : IDisposable where T : unmanaged, ID3D11DeviceChild.Interface
{
    /// <summary> The shader blob. </summary>
    protected byte[] Blob = blob;

    /// <summary> A description of this shader, for debugging and logging purposes. </summary>
    protected string? Description = description;

    private ComPtr<T> _shader;

    ~Shader()
        => Dispose(false);

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary> Releases the resources used by this object. </summary>
    /// <param name="disposing"> True if called explicitly, false if garbage collected. </param>
    protected virtual void Dispose(bool disposing)
        => _shader.Dispose();

    /// <inheritdoc/>
    public override string? ToString()
        => Description ?? base.ToString();

    /// <summary> Gets the Direct3D shader object, creating it if necessary. </summary>
    /// <returns> The shader object. </returns>
    public unsafe T* GetOrCreateShader()
    {
        if (!_shader.Valid)
            _shader.Attach(CreateShader());

        return _shader;
    }

    /// <summary> Creates the Direct3D shader object. </summary>
    /// <returns> The shader object. </returns>
    /// <remarks> This can be overridden to use Direct3D 11 class linkage features. </remarks>
    protected abstract unsafe T* CreateShader();

    /// <summary> Invalidates the Direct3D shader object. </summary>
    /// <remarks> If using the default implementation of <see cref="CreateShader"/>, this should be called only after changing <see cref="Blob" />. </remarks>
    protected void InvalidateShader()
        => _shader.Dispose();
}

/// <summary> A Direct3D vertex shader. </summary>
/// <param name="blob"> The vertex shader blob. </param>
/// <param name="description"> A description of this shader, for debugging and logging purposes. </param>
public class VertexShader(byte[] blob, string description) : Shader<ID3D11VertexShader>(blob, description)
{
    /// <inheritdoc/>
    protected override unsafe ID3D11VertexShader* CreateShader()
    {
        ID3D11VertexShader* shader;
        fixed (byte* pBlob = Blob)
        {
            Marshal.ThrowExceptionForHR(
                CustomRenderManager.Instance.Device->CreateVertexShader(pBlob, unchecked((uint)Blob.Length), null, &shader));
        }

        return shader;
    }
}

/// <summary> A Direct3D pixel shader. </summary>
/// <param name="blob"> The pixel shader blob. </param>
/// <param name="description"> A description of this shader, for debugging and logging purposes. </param>
public class PixelShader(byte[] blob, string description) : Shader<ID3D11PixelShader>(blob, description)
{
    /// <inheritdoc/>
    protected override unsafe ID3D11PixelShader* CreateShader()
    {
        ID3D11PixelShader* shader;
        fixed (byte* pBlob = Blob)
        {
            Marshal.ThrowExceptionForHR(
                CustomRenderManager.Instance.Device->CreatePixelShader(pBlob, unchecked((uint)Blob.Length), null, &shader));
        }

        return shader;
    }
}

/// <summary> A Direct3D geometry shader. </summary>
/// <param name="blob"> The geometry shader blob. </param>
/// <param name="description"> A description of this shader, for debugging and logging purposes. </param>
public class GeometryShader(byte[] blob, string description) : Shader<ID3D11GeometryShader>(blob, description)
{
    /// <inheritdoc/>
    protected override unsafe ID3D11GeometryShader* CreateShader()
    {
        ID3D11GeometryShader* shader;
        fixed (byte* pBlob = Blob)
        {
            Marshal.ThrowExceptionForHR(
                CustomRenderManager.Instance.Device->CreateGeometryShader(pBlob, unchecked((uint)Blob.Length), null, &shader));
        }

        return shader;
    }
}

/// <summary> A Direct3D hull shader. </summary>
/// <param name="blob"> The hull shader blob. </param>
/// <param name="description"> A description of this shader, for debugging and logging purposes. </param>
public class HullShader(byte[] blob, string description) : Shader<ID3D11HullShader>(blob, description)
{
    /// <inheritdoc/>
    protected override unsafe ID3D11HullShader* CreateShader()
    {
        ID3D11HullShader* shader;
        fixed (byte* pBlob = Blob)
        {
            Marshal.ThrowExceptionForHR(
                CustomRenderManager.Instance.Device->CreateHullShader(pBlob, unchecked((uint)Blob.Length), null, &shader));
        }

        return shader;
    }
}

/// <summary> A Direct3D domain shader. </summary>
/// <param name="blob"> The domain shader blob. </param>
/// <param name="description"> A description of this shader, for debugging and logging purposes. </param>
public class DomainShader(byte[] blob, string description) : Shader<ID3D11DomainShader>(blob, description)
{
    /// <inheritdoc/>
    protected override unsafe ID3D11DomainShader* CreateShader()
    {
        ID3D11DomainShader* shader;
        fixed (byte* pBlob = Blob)
        {
            Marshal.ThrowExceptionForHR(
                CustomRenderManager.Instance.Device->CreateDomainShader(pBlob, unchecked((uint)Blob.Length), null, &shader));
        }

        return shader;
    }
}

/// <summary> A Direct3D compute shader. </summary>
/// <param name="blob"> The compute shader blob. </param>
/// <param name="description"> A description of this shader, for debugging and logging purposes. </param>
public class ComputeShader(byte[] blob, string description) : Shader<ID3D11ComputeShader>(blob, description)
{
    /// <inheritdoc/>
    protected override unsafe ID3D11ComputeShader* CreateShader()
    {
        ID3D11ComputeShader* shader;
        fixed (byte* pBlob = Blob)
        {
            Marshal.ThrowExceptionForHR(
                CustomRenderManager.Instance.Device->CreateComputeShader(pBlob, unchecked((uint)Blob.Length), null, &shader));
        }

        return shader;
    }
}
