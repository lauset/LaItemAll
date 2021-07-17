using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace LaItemAll.main.Models
{
    /// <summary>mod设置项</summary>
    internal class ModConfig
    {
        /// <summary>打开物品菜单的按键绑定，默认I</summary>
        public KeybindList ShowMenuKey { get; set; } = new(SButton.I);

        /// <summary>是否显示生成时可能导致错误或崩溃的物品项</summary>
        public bool AllowProblematicItems { get; set; } = false;
    }
}
