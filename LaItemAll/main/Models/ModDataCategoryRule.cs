using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using LaItemAll.main.ItemData;
using StardewValley;
using Object = StardewValley.Object;

namespace LaItemAll.main.Models
{
    /// <summary>种类的规则，对应categories.json文件中的数据</summary>
    internal class ModDataCategoryRule
    {
        /// <summary>物品C#实例类型的全名</summary>
        public ISet<string> Class { get; set; }

        /// <summary>物品类型 (i.e. <see cref="Object.Type"/>).</summary>
        public ISet<string> ObjType { get; set; }

        /// <summary>物品种类 (i.e. <see cref="Item.Category"/>).</summary>
        public ISet<int> ObjCategory { get; set; }

        /// <summary>物品唯一ID (i.e. <see cref="Item.ParentSheetIndex"/>).</summary>
        public ISet<string> ItemId { get; set; }

        /// <summary>检查物品是否与规则项匹配</summary>
        /// <param name="entry">要检查的物品</param>
        public bool IsMatch(SearchableItem entry)
        {
            Item item = entry.Item;
            Object obj = item as Object;

            // match criteria
            if (this.Class.Any() && this.GetClassFullNames(item).Any(className => this.Class.Contains(className)))
                return true;
            if (this.ObjCategory.Any() && this.ObjCategory.Contains(item.Category))
                return true;
            if (this.ObjType.Any() && obj != null && this.ObjType.Contains(obj.Type))
                return true;
            if (this.ItemId.Any() && this.ItemId.Contains($"{entry.Type}:{item.ParentSheetIndex}"))
                return true;

            return false;
        }

        /// <summary>反序列化后规范化数据模型</summary>
        /// <param name="context">反序列化上下文</param>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            this.Class = new HashSet<string>(this.Class ?? (IEnumerable<string>)new string[0], StringComparer.OrdinalIgnoreCase);
            this.ObjType = new HashSet<string>(this.ObjType ?? (IEnumerable<string>)new string[0], StringComparer.OrdinalIgnoreCase);
            this.ItemId = new HashSet<string>(this.ItemId ?? (IEnumerable<string>)new string[0], StringComparer.OrdinalIgnoreCase);
            this.ObjCategory ??= new HashSet<int>();
        }

        /// <summary>获取物品类曾经结构中的所有全名</summary>
        /// <param name="item">物品实例</param>
        private IEnumerable<string> GetClassFullNames(Item item)
        {
            for (Type type = item.GetType(); type != null; type = type.BaseType)
                yield return type.FullName;
        }
    }
}
