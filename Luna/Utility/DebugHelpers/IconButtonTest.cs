using Dalamud.Interface;

namespace Luna.DebugHelpers;

public static class IconButtonTest
{
    private static int _buttonTestLastClicked;

    public static void Draw()
    {
        if (!Im.Tree.Header("ImSharp Icon.LabeledButton Demo/Test"u8))
            return;

        Im.Text("All of these buttons are dummies that do not execute any actual action."u8);
        Im.Text($"Last clicked button: {_buttonTestLastClicked}");

        using (Im.Id.Push(100))
        {
            Im.Text("Row 100"u8);

            if (ImEx.Icon.LabeledButton(LunaStyle.SaveIcon, "Save"u8, true))
                _buttonTestLastClicked = 101;
            Im.Line.SameInner();
            if (ImEx.Icon.LabeledButton(default(AwesomeIcon), "Save As"u8))
                _buttonTestLastClicked = 102;
            Im.Line.SameInner();
            if (ImEx.Icon.LabeledButton(LunaStyle.TreeExpandIcon, "##saveMoreOptions"u8, "More options for saving"u8))
                _buttonTestLastClicked = 103;
        }

        using (Im.Id.Push(200))
        {
            Im.Text("Row 200"u8);

            if (ImEx.Icon.LabeledButton(FontAwesomeIcon.FastBackward.Icon(), "To Beginning"u8, new Vector2(120.0f, 0.0f),
                    iconPosition: ImEx.Icon.IconPosition.Start))
                _buttonTestLastClicked = 201;
            Im.Line.SameInner();
            if (ImEx.Icon.LabeledButton(FontAwesomeIcon.StepBackward.Icon(), "Previous"u8, new Vector2(120.0f, 0.0f),
                    iconPosition: ImEx.Icon.IconPosition.BeforeLabel))
                _buttonTestLastClicked = 202;
            Im.Line.SameInner();
            if (ImEx.Icon.LabeledButton(FontAwesomeIcon.StepForward.Icon(), "Next"u8, new Vector2(120.0f, 0.0f),
                    iconPosition: ImEx.Icon.IconPosition.AfterLabel))
                _buttonTestLastClicked = 203;
            Im.Line.SameInner();
            if (ImEx.Icon.LabeledButton(FontAwesomeIcon.FastForward.Icon(), "To End"u8, new Vector2(120.0f, 0.0f),
                    iconPosition: ImEx.Icon.IconPosition.End))
                _buttonTestLastClicked = 204;
        }

        using (Im.Id.Push(300))
        {
            Im.Text("Row 300"u8);

            if (ImEx.Icon.LabeledButton(FontAwesomeIcon.FastBackward.Icon(), "##backward"u8, "Move to the previous track."u8,
                    corners: Corners.Left))
                _buttonTestLastClicked = 301;
            Im.Line.NoSpacing();
            if (ImEx.Icon.LabeledButton(FontAwesomeIcon.Pause.Icon(), "##pause"u8, corners: Corners.None))
                _buttonTestLastClicked = 302;
            Im.Line.NoSpacing();
            if (ImEx.Icon.LabeledButton(FontAwesomeIcon.FastForward.Icon(), "##forward"u8, "Move to the next track."u8, true,
                    corners: Corners.Right))
                _buttonTestLastClicked = 303;
            Im.Line.SameInner();
            if (ImEx.Icon.LabeledButton(FontAwesomeIcon.Eject.Icon(), "Eject"u8, true, corners: Corners.All))
                _buttonTestLastClicked = 304;
        }

        using (Im.Id.Push(400))
        {
            Im.Text("Row 400"u8);

            if (ImEx.Icon.LabeledButton(LunaStyle.TrueIcon, "True"u8,
                    buttonColor: _buttonTestLastClicked == 401 ? ImGuiColor.ButtonActive.Get() : ColorParameter.Default, corners: Corners.Left))
                _buttonTestLastClicked = 401;
            Im.Line.NoSpacing();
            var conf = new ImEx.ButtonConfiguration
            {
                ButtonColor = Rgba32.Black,
                TextColor   = Rgba32.Red,
                BorderColor = Rgba32.Yellow,
            };
            if (ImEx.Icon.LabeledButton(LunaStyle.FalseIcon, "##false"u8, in conf, Corners.Right))
                _buttonTestLastClicked = 402;
        }

        using (Im.Id.Push(500))
        {
            Im.Text("Row 500"u8);

            if (ImEx.Icon.Button(LunaStyle.SaveIcon, "Save"u8, true))
                _buttonTestLastClicked = 501;
            Im.Line.SameInner();
            if (ImEx.Icon.Button(LunaStyle.TreeExpandIcon, "More options for saving"u8))
                _buttonTestLastClicked = 502;
        }
    }
}
