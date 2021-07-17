using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using LaItemAll.main.ItemData;
using StardewValley;
using Object = StardewValley.Object;

namespace LaItemAll.main.Models
{
    /// <summary>����Ĺ��򣬶�Ӧcategories.json�ļ��е�����</summary>
    internal class ModDataCategoryRule
    {
        /// <summary>��ƷC#ʵ�����͵�ȫ��</summary>
        public ISet<string> Class { get; set; }

        /// <summary>��Ʒ���� (i.e. <see cref="Object.Type"/>).</summary>
        public ISet<string> ObjType { get; set; }

        /// <summary>��Ʒ���� (i.e. <see cref="Item.Category"/>).</summary>
        public ISet<int> ObjCategory { get; set; }

        /// <summary>��ƷΨһID (i.e. <see cref="Item.ParentSheetIndex"/>).</summary>
        public ISet<string> ItemId { get; set; }

        /// <summary>�����Ʒ�Ƿ��������ƥ��</summary>
        /// <param name="entry">Ҫ������Ʒ</param>
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

        /// <summary>�����л���淶������ģ��</summary>
        /// <param name="context">�����л�������</param>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            this.Class = new HashSet<string>(this.Class ?? (IEnumerable<string>)new string[0], StringComparer.OrdinalIgnoreCase);
            this.ObjType = new HashSet<string>(this.ObjType ?? (IEnumerable<string>)new string[0], StringComparer.OrdinalIgnoreCase);
            this.ItemId = new HashSet<string>(this.ItemId ?? (IEnumerable<string>)new string[0], StringComparer.OrdinalIgnoreCase);
            this.ObjCategory ??= new HashSet<int>();
        }

        /// <summary>��ȡ��Ʒ�������ṹ�е�����ȫ��</summary>
        /// <param name="item">��Ʒʵ��</param>
        private IEnumerable<string> GetClassFullNames(Item item)
        {
            for (Type type = item.GetType(); type != null; type = type.BaseType)
                yield return type.FullName;
        }
    }
}
