using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace LaItemAll.main.Models
{
    /// <summary>mod������</summary>
    internal class ModConfig
    {
        /// <summary>����Ʒ�˵��İ����󶨣�Ĭ��I</summary>
        public KeybindList ShowMenuKey { get; set; } = new(SButton.I);

        /// <summary>�Ƿ���ʾ����ʱ���ܵ��´�����������Ʒ��</summary>
        public bool AllowProblematicItems { get; set; } = false;
    }
}
