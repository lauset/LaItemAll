using System;

namespace LaItemAll.main.Constants
{
    /// <summary>ָ���������</summary>
    internal enum ItemSort
    {
        /// <summary>������������ (display name)</summary>
        DisplayName,
        /// <summary>������������ (category name)</summary>
        Type,
        /// <summary>����ID����</summary>
        ID
    }

    internal static class ItemSortExtensions
    {
        /// <summary>��ȡ��һ������ѡ��</summary>
        /// <param name="current">��ǰ�����ֵ</param>
        public static ItemSort GetNext(this ItemSort current)
        {
            return current switch
            {
                ItemSort.DisplayName => ItemSort.Type,
                ItemSort.Type => ItemSort.ID,
                ItemSort.ID => ItemSort.DisplayName,
                _ => throw new NotSupportedException($"Unknown sort '{current}'.")
            };
        }
    }
}
