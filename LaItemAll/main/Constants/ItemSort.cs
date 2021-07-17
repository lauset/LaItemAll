using System;

namespace LaItemAll.main.Constants
{
    /// <summary>指定如何排序</summary>
    internal enum ItemSort
    {
        /// <summary>按照名称排序 (display name)</summary>
        DisplayName,
        /// <summary>按照种类排序 (category name)</summary>
        Type,
        /// <summary>按照ID排序</summary>
        ID
    }

    internal static class ItemSortExtensions
    {
        /// <summary>获取下一个排序选项</summary>
        /// <param name="current">当前排序的值</param>
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
