using System;
using StardewValley;

namespace LaItemAll.main.ItemData
{
    /// <summary>带有元数据的物品项</summary>
    /// <remarks>SMAPI源码复制，应保持同步</remarks>
    internal class SearchableItem
    {
        /// <summary>物品类型</summary>
        public ItemType Type { get; }
        /// <summary>物品实例</summary>
        public Item Item { get; }
        /// <summary>创建一个物品实例</summary>
        public Func<Item> CreateItem { get; }
        /// <summary>物品类型唯一ID</summary>
        public int ID { get; }
        /// <summary>物品真实名称</summary>
        public string Name => this.Item.Name;
        /// <summary>当前语言物品显示名称</summary>
        public string DisplayName => this.Item.DisplayName;

        /// <summary>构造方法</summary>
        /// <param name="type">物品类型</param>
        /// <param name="id">唯一ID (假设与父级索引不同).</param>
        /// <param name="createItem">创建一个物品</param>
        public SearchableItem(ItemType type, int id, Func<SearchableItem, Item> createItem)
        {
            this.Type = type;
            this.ID = id;
            this.CreateItem = () => createItem(this);
            this.Item = createItem(this);
        }

        /// <summary>构造方法</summary>
        /// <param name="item">复制物品元数据</param>
        public SearchableItem(SearchableItem item)
        {
            this.Type = item.Type;
            this.ID = item.ID;
            this.CreateItem = item.CreateItem;
            this.Item = item.Item;
        }

        /// <summary>不区分大小写包含</summary>
        /// <param name="substring">物品名称</param>
        public bool NameContains(string substring)
        {
            return
                this.Name.IndexOf(substring, StringComparison.OrdinalIgnoreCase) != -1
                || this.DisplayName.IndexOf(substring, StringComparison.OrdinalIgnoreCase) != -1;
        }

        /// <summary>不区分大小写完全等于</summary>
        /// <param name="name">物品名称</param>
        public bool NameEquivalentTo(string name)
        {
            return
                this.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                || this.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
