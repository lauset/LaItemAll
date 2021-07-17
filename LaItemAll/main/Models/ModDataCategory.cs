using LaItemAll.main.ItemData;

namespace LaItemAll.main.Models
{
    /// <summary>��Ʒ������˹��򣬶�Ӧ��categories.json�ļ��е�����</summary>
    internal class ModDataCategory
    {
        /// <summary>�����ʾ���ı�</summary>
        public string Label { get; set; }

        /// <summary>������ƥ��Ĺ���</summary>
        public ModDataCategoryRule When { get; set; }

        /// <summary>���ԵĹ���</summary>
        public ModDataCategoryRule Except { get; set; }

        /// <summary>�����Ʒ�����Ƿ�ƥ��</summary>
        /// <param name="item">Ҫ������Ʒ��</param>
        public bool IsMatch(SearchableItem item)
        {
            return
                this.When != null
                && this.When.IsMatch(item)
                && this.Except?.IsMatch(item) != true;
        }
    }
}
