// (c) 2025 Ottermandias (ChatAlerts)
using Dalamud.Bindings.ImGui;

namespace SimpleTriggers.Gui
{
    public static partial class ImGuiCustom
    {
        public static void HoverTooltip(string text)
        {
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(text);
        }
    }
}