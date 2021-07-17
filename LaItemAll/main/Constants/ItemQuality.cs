using System;
using SObject = StardewValley.Object;

namespace LaItemAll.main.Constants
{
    /// <summary>物品品质(普通<银<金<铱)</summary>
    internal enum ItemQuality
    {
        Normal = SObject.lowQuality,
        Silver = SObject.medQuality,
        Gold = SObject.highQuality,
        Iridium = SObject.bestQuality
    }

    internal static class ItemQualityExtensions
    {
        /// <summary>获取下一级品质方法</summary>
        /// <param name="current">当前品质</param>
        public static ItemQuality GetNext(this ItemQuality current)
        {
            return current switch
            {
                ItemQuality.Normal => ItemQuality.Silver,
                ItemQuality.Silver => ItemQuality.Gold,
                ItemQuality.Gold => ItemQuality.Iridium,
                ItemQuality.Iridium => ItemQuality.Normal,
                _ => throw new NotSupportedException($"Unknown quality '{current}'.")
            };
        }
    }
}
