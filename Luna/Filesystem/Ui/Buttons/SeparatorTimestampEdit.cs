namespace Luna;

/// <summary> Provides a date input to move the separators sort order when using dates. </summary>
/// <param name="fileSystem"> The parent file system. </param>
public sealed class SeparatorTimestampEdit(BaseFileSystem fileSystem) : BaseButton<IFileSystemSeparator>
{
    private long _tempDate = long.MinValue;
    private bool _setFocus = false;

    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label(in IFileSystemSeparator data)
        => "Timestamp Edit"u8;

    /// <inheritdoc/>
    public override bool DrawMenuItem(in IFileSystemSeparator data)
    {
        if (_tempDate is not long.MinValue)
        {
            if (_setFocus)
            {
                Im.Keyboard.SetFocusHere();
                _setFocus = false;
            }

            Im.Item.SetNextWidthScaled(250);
            Im.Input.Scalar("Sort Order Time"u8, ref _tempDate);
            if (Im.Item.Deactivated)
            {
                if (Im.Item.DeactivatedAfterEdit)
                {
                    fileSystem.ChangeSeparator(data, _tempDate);
                    _tempDate = long.MinValue;
                    return true;
                }

                _tempDate = long.MinValue;
            }

            return false;
        }

        var date = DateTimeOffset.FromUnixTimeMilliseconds(data.CreationDate).ToLocalTime();
        if (Im.Button($"{date:g}", ImEx.ScaledVectorX(250)))
        {
            _tempDate = data.CreationDate;
            _setFocus = true;
        }

        Im.Line.SameInner();
        Im.Text("Sort Order Time"u8);
        return false;
    }
}
