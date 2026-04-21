// (c) 2025 Ottermandias (ChatAlerts)
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace SimpleTriggers.Gui
{
    public static partial class ImGuiCustom
    {
        public static void HoverTooltip(string text)
        {
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(text);
        }

        public static void IconTooltip(string text, FontAwesomeIcon icon = FontAwesomeIcon.ExclamationCircle)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text(icon.ToIconString());
            ImGui.PopFont();
            HoverTooltip(text);
        }
    }
}