using System;
using SObject = StardewValley.Object;

namespace LaItemAll.main.Constants
{
    /// <summary>��ƷƷ��(��ͨ<��<��<ҿ)</summary>
    internal enum ItemQuality
    {
        Normal = SObject.lowQuality,
        Silver = SObject.medQuality,
        Gold = SObject.highQuality,
        Iridium = SObject.bestQuality
    }

    internal static class ItemQualityExtensions
    {
        /// <summary>��ȡ��һ��Ʒ�ʷ���</summary>
        /// <param name="current">��ǰƷ��</param>
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
