using LaItemAll.main.ItemData;

namespace LaItemAll.main
{
    internal class SpawnableItem : SearchableItem
    {
        /// <summary>物品所属种类筛选标签</summary>
        public string Category { get; }

        /// <summary>构造方法</summary>
        /// <param name="item">物品元数据</param>
        /// <param name="category">物品种类（菜单中的标签）</param>
        public SpawnableItem(SearchableItem item, string category)
            : base(item)
        {
            this.Category = category;
        }
    }
}
