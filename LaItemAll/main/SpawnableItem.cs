using LaItemAll.main.ItemData;

namespace LaItemAll.main
{
    internal class SpawnableItem : SearchableItem
    {
        /// <summary>��Ʒ��������ɸѡ��ǩ</summary>
        public string Category { get; }

        /// <summary>���췽��</summary>
        /// <param name="item">��ƷԪ����</param>
        /// <param name="category">��Ʒ���ࣨ�˵��еı�ǩ��</param>
        public SpawnableItem(SearchableItem item, string category)
            : base(item)
        {
            this.Category = category;
        }
    }
}
