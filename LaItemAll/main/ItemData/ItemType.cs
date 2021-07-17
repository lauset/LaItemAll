namespace LaItemAll.main.ItemData
{
    /// <summary>物品类型</summary>
    internal enum ItemType
    {
        /// <summary>工艺品 <see cref="StardewValley.Game1.bigCraftablesInformation"/> </summary>
        BigCraftable,
        /// <summary>鞋子 <see cref="StardewValley.Objects.Boots"/> </summary>
        Boots,
        /// <summary>衣服 <see cref="StardewValley.Objects.Clothing"/> </summary>
        Clothing,
        /// <summary>地板 <see cref="StardewValley.Objects.Wallpaper"/> </summary>
        Flooring,
        /// <summary>家具 <see cref="StardewValley.Objects.Furniture"/> </summary>
        Furniture,
        /// <summary>帽子 <see cref="StardewValley.Objects.Hat"/> </summary>
        Hat,
        /// <summary>物体，除了戒指 <see cref="StardewValley.Game1.objectInformation"/> </summary>
        Object,
        /// <summary>戒指 <see cref="StardewValley.Objects.Ring"/> </summary>
        Ring,
        /// <summary>工具 <see cref="StardewValley.Tool"/> </summary>
        Tool,
        /// <summary>墙纸 <see cref="StardewValley.Objects.Wallpaper"/> </summary>
        Wallpaper,
        /// <summary>武器 <see cref="StardewValley.Tools.MeleeWeapon"/> 或 <see cref="StardewValley.Tools.Slingshot"/> </summary>
        Weapon
    }
}
