using Dalamud.Interface.Colors;

namespace Luna;

/// <summary> The list of available colors provided by Dalamud. </summary>
public enum DalamudColor
{
    DalamudRed,
    DalamudGrey,
    DalamudGrey2,
    DalamudGrey3,
    DalamudWhite,
    DalamudWhite2,
    DalamudOrange,
    TankBlue,
    HealerGreen,
    DpsRed,
    DalamudYellow,
    DalamudViolet,
    ParsedGrey,
    ParsedGreen,
    ParsedBlue,
    ParsedPurple,
    ParsedOrange,
    ParsedPink,
    ParsedGold,
    InfoForeground,
    InfoBackground,
    SuccessForeground,
    SuccessBackground,
    WarningForeground,
    WarningBackground,
    ErrorForeground,
    ErrorBackground,
    AttentionForeground,
    AttentionBackground,
}

public static class DalamudColorExtensions
{
    extension(DalamudColor id)
    {
        /// <summary> Get the currently assigned value for a Dalamud color. </summary>
        public Vector4 Value
        {
            [MethodImpl(ImSharpConfiguration.OptInl)]
            get => id switch
            {
                DalamudColor.DalamudRed          => ImGuiColors.DalamudRed,
                DalamudColor.DalamudGrey         => ImGuiColors.DalamudGrey,
                DalamudColor.DalamudGrey2        => ImGuiColors.DalamudGrey2,
                DalamudColor.DalamudGrey3        => ImGuiColors.DalamudGrey3,
                DalamudColor.DalamudWhite        => ImGuiColors.DalamudWhite,
                DalamudColor.DalamudWhite2       => ImGuiColors.DalamudWhite2,
                DalamudColor.DalamudOrange       => ImGuiColors.DalamudOrange,
                DalamudColor.TankBlue            => ImGuiColors.TankBlue,
                DalamudColor.HealerGreen         => ImGuiColors.HealerGreen,
                DalamudColor.DpsRed              => ImGuiColors.DPSRed,
                DalamudColor.DalamudYellow       => ImGuiColors.DalamudYellow,
                DalamudColor.DalamudViolet       => ImGuiColors.DalamudViolet,
                DalamudColor.ParsedGrey          => ImGuiColors.ParsedGrey,
                DalamudColor.ParsedGreen         => ImGuiColors.ParsedGreen,
                DalamudColor.ParsedBlue          => ImGuiColors.ParsedBlue,
                DalamudColor.ParsedPurple        => ImGuiColors.ParsedPurple,
                DalamudColor.ParsedOrange        => ImGuiColors.ParsedOrange,
                DalamudColor.ParsedPink          => ImGuiColors.ParsedPink,
                DalamudColor.ParsedGold          => ImGuiColors.ParsedGold,
                DalamudColor.InfoForeground      => ImGuiColors.InfoForeground,
                DalamudColor.InfoBackground      => ImGuiColors.InfoBackground,
                DalamudColor.SuccessForeground   => ImGuiColors.SuccessForeground,
                DalamudColor.SuccessBackground   => ImGuiColors.SuccessBackground,
                DalamudColor.WarningForeground   => ImGuiColors.WarningForeground,
                DalamudColor.WarningBackground   => ImGuiColors.WarningBackground,
                DalamudColor.ErrorForeground     => ImGuiColors.ErrorForeground,
                DalamudColor.ErrorBackground     => ImGuiColors.ErrorBackground,
                DalamudColor.AttentionForeground => ImGuiColors.AttentionForeground,
                DalamudColor.AttentionBackground => ImGuiColors.AttentionBackground,
                _                                => throw new ArgumentOutOfRangeException(nameof(id), id, null),
            };
        }
    }
}
