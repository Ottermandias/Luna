using Dalamud.Interface.ImGuiNotification;

namespace Luna;

/// <summary> Auxiliary functions to draw some of the support buttons. </summary>
public static class SupportButton
{
    public const uint DiscordColor     = 0xFFDA8972;
    public const uint ReniColorButton  = 0xFFCC648D;
    public const uint ReniColorHovered = 0xFFB070B0;
    public const uint ReniColorActive  = 0xFF9070E0;

    /// <summary> Draw a button to open the official Penumbra/Glamourer discord server. </summary>
    public static void Discord(MessageService message, float width)
    {
        const string address = "https://discord.gg/kVva7DHV4r";
        using var    color   = ImGuiColor.Button.Push(DiscordColor);

        Link(message, "Join Discord for Support"u8, address, width, $"Open {address}");
    }

    /// <summary> Draw the button that opens the ReniGuide. </summary>
    public static void ReniGuide(MessageService message, float width)
    {
        const string address = "https://reniguide.info/";
        using var color = Im.Color.Push(ImGuiColor.Button, ReniColorButton)
            .Push(ImGuiColor.ButtonHovered, ReniColorHovered)
            .Push(ImGuiColor.ButtonActive,  ReniColorActive);

        Link(message, "Beginner's Guides"u8, address, width,
            $"Open {address}\nImage and text based guides for most functionality of Penumbra made by Serenity.\n"
          + "Not directly affiliated and potentially, but not usually out of date.");
    }

    /// <summary> Draw a button that opens an address in the browser. </summary>
    public static void Link(MessageService message, Utf8LabelHandler text, string address, float width, Utf8TextHandler tooltip)
    {
        if (Im.Button(text, new Vector2(width, 0)))
            try
            {
                var process = new ProcessStartInfo(address)
                {
                    UseShellExecute = true,
                };
                Process.Start(process);
            }
            catch
            {
                message.NotificationMessage($"Could not open the link to {address} in external browser", NotificationType.Error);
            }

        Im.Tooltip.OnHover(ref tooltip);
    }

    private const string KofiAddress    = "https://ko-fi.com/ottermandias";
    private const string PatreonAddress = "https://www.patreon.com/Ottermandias";

    private const string Happiness =
        "Any donations made are entirely voluntary and will not yield any preferential treatment or benefits beyond making Otter happy.";

    private static readonly ImEx.SplitButtonData KoFiData = new(new StringU8("Ko-Fi"u8))
    {
        Active     = 0xFF5B5EFFu,
        Background = 0xFFFFC313u,
        Hovered    = ColorParameter.Default,
        Tooltip    = new StringU8($"Open Ottermandias' Ko-Fi at {KofiAddress} in your browser.\n\n{Happiness}"),
    };

    private static readonly ImEx.SplitButtonData PatreonData = new(new StringU8("Patreon"u8))
    {
        Active     = 0xFF492C00u,
        Hovered    = ColorParameter.Default,
        Background = 0xFF5467F7u,
        Tooltip    = new StringU8($"Open Ottermandias' Patreon at {PatreonAddress} in your browser.\n\n{Happiness}"),
    };

    /// <summary> Draw a split button to link to Ottermandias' Ko-Fi and Patreon. </summary>
    public static void KoFiPatreon(MessageService message, Vector2 size)
    {
        var splitButtonPatreon = ImEx.SplitButton(5, KoFiData, PatreonData, size,
            KoFiData.Background.Color!.Value.Mix(PatreonData.Background.Color!.Value));
        var (address, name) = splitButtonPatreon switch
        {
            ImEx.SplitButtonHalf.UpperLeft  => (KofiAddress, "Ko-Fi"),
            ImEx.SplitButtonHalf.LowerRight => (PatreonAddress, "Patreon"),
            _                               => (string.Empty, string.Empty),
        };
        if (address.Length is 0)
            return;

        try
        {
            var process = new ProcessStartInfo(address)
            {
                UseShellExecute = true,
            };
            Process.Start(process);
        }
        catch
        {
            message.NotificationMessage($"Could not open {name} link at {address} in external browser", NotificationType.Error);
        }
    }
}
