using System;
using StardewValley;

namespace LaItemAll.main.ItemData
{
    /// <summary>����Ԫ���ݵ���Ʒ��</summary>
    /// <remarks>SMAPIԴ�븴�ƣ�Ӧ����ͬ��</remarks>
    internal class SearchableItem
    {
        /// <summary>��Ʒ����</summary>
        public ItemType Type { get; }
        /// <summary>��Ʒʵ��</summary>
        public Item Item { get; }
        /// <summary>����һ����Ʒʵ��</summary>
        public Func<Item> CreateItem { get; }
        /// <summary>��Ʒ����ΨһID</summary>
        public int ID { get; }
        /// <summary>��Ʒ��ʵ����</summary>
        public string Name => this.Item.Name;
        /// <summary>��ǰ������Ʒ��ʾ����</summary>
        public string DisplayName => this.Item.DisplayName;

        /// <summary>���췽��</summary>
        /// <param name="type">��Ʒ����</param>
        /// <param name="id">ΨһID (�����븸��������ͬ).</param>
        /// <param name="createItem">����һ����Ʒ</param>
        public SearchableItem(ItemType type, int id, Func<SearchableItem, Item> createItem)
        {
            this.Type = type;
            this.ID = id;
            this.CreateItem = () => createItem(this);
            this.Item = createItem(this);
        }

        /// <summary>���췽��</summary>
        /// <param name="item">������ƷԪ����</param>
        public SearchableItem(SearchableItem item)
        {
            this.Type = item.Type;
            this.ID = item.ID;
            this.CreateItem = item.CreateItem;
            this.Item = item.Item;
        }

        /// <summary>�����ִ�Сд����</summary>
        /// <param name="substring">��Ʒ����</param>
        public bool NameContains(string substring)
        {
            return
                this.Name.IndexOf(substring, StringComparison.OrdinalIgnoreCase) != -1
                || this.DisplayName.IndexOf(substring, StringComparison.OrdinalIgnoreCase) != -1;
        }

        /// <summary>�����ִ�Сд��ȫ����</summary>
        /// <param name="name">��Ʒ����</param>
        public bool NameEquivalentTo(string name)
        {
            return
                this.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                || this.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
