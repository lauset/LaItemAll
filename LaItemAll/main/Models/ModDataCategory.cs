using LaItemAll.main.ItemData;

namespace LaItemAll.main.Models
{
    /// <summary>物品种类过滤规则，对应了categories.json文件中的数据</summary>
    internal class ModDataCategory
    {
        /// <summary>类别显示的文本</summary>
        public string Label { get; set; }

        /// <summary>与类型匹配的规则</summary>
        public ModDataCategoryRule When { get; set; }

        /// <summary>忽略的规则</summary>
        public ModDataCategoryRule Except { get; set; }

        /// <summary>检查物品种类是否匹配</summary>
        /// <param name="item">要检查的物品项</param>
        public bool IsMatch(SearchableItem item)
        {
            return
                this.When != null
                && this.When.IsMatch(item)
                && this.Except?.IsMatch(item) != true;
        }
    }
}
