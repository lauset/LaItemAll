using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using StardewValley;
using StardewValley.GameData.FishPond;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace LaItemAll.main.ItemData
{
    /// <summary>提供搜索和构造项方法.</summary>
    /// <remarks>这是从SMAPI源代码复制的，应该与之保持同步</remarks>
    internal class ItemRepository
    {
        /// <summary>自定义，项目中不存在的唯一ID</summary>
        private readonly int CustomIDOffset = 1000;

        /// <summary>获取所有可生成的物品.</summary>
        [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "TryCreate invokes the lambda immediately.")]
        public IEnumerable<SearchableItem> GetAll()
        {
            // 注意，闭包变量捕获
            //
            // SearchableItem 存储Func<Item> 以稍后创建新实例。
            // 循环变量传递到函数将被捕获，因此循环中的每个func都将使用上一次迭代的值
            // 用TryCreate(type, id, entity => item) 或创建局部变量传入
            //

            IEnumerable<SearchableItem> GetAllRaw()
            {
                // 工具get tools
                for (int q = Tool.stone; q <= Tool.iridium; q++)
                {
                    // 品级 stone石头 -> iridium铱
                    int quality = q;
                    // 斧头
                    yield return this.TryCreate(ItemType.Tool, ToolFactory.axe, _ => ToolFactory.getToolFromDescription(ToolFactory.axe, quality));
                    // 锄头
                    yield return this.TryCreate(ItemType.Tool, ToolFactory.hoe, _ => ToolFactory.getToolFromDescription(ToolFactory.hoe, quality));
                    // 镐
                    yield return this.TryCreate(ItemType.Tool, ToolFactory.pickAxe, _ => ToolFactory.getToolFromDescription(ToolFactory.pickAxe, quality));
                    // 水壶
                    yield return this.TryCreate(ItemType.Tool, ToolFactory.wateringCan, _ => ToolFactory.getToolFromDescription(ToolFactory.wateringCan, quality));
                    // 鱼竿
                    if (quality != Tool.iridium)
                        yield return this.TryCreate(ItemType.Tool, ToolFactory.fishingRod, _ => ToolFactory.getToolFromDescription(ToolFactory.fishingRod, quality));
                }
                // 以下没有sort ID，所以赋值自定义ID
                // 奶瓶
                yield return this.TryCreate(ItemType.Tool, this.CustomIDOffset, _ => new MilkPail());
                // 剪刀
                yield return this.TryCreate(ItemType.Tool, this.CustomIDOffset + 1, _ => new Shears());
                // pan
                yield return this.TryCreate(ItemType.Tool, this.CustomIDOffset + 2, _ => new Pan());
                // 魔杖
                yield return this.TryCreate(ItemType.Tool, this.CustomIDOffset + 3, _ => new Wand());
                // 衣服clothing
                {
                    HashSet<int> clothingIds = new HashSet<int>();
                    foreach (int id in Game1.clothingInformation.Keys)
                    {
                        if (id < 0)
                            continue;

                        clothingIds.Add(id);
                        yield return this.TryCreate(ItemType.Clothing, id, p => new Clothing(p.ID));
                    }
                    // 有些衬衫在这个范围内没有数据，但游戏有特殊的逻辑来处理它们
                    for (int id = 1000; id <= 1111; id++)
                    {
                        if (!clothingIds.Contains(id))
                            yield return this.TryCreate(ItemType.Clothing, id, p => new Clothing(p.ID));
                    }
                }
                // 墙纸wallpapers
                for (int id = 0; id < 112; id++)
                    yield return this.TryCreate(ItemType.Wallpaper, id, p => new Wallpaper(p.ID) { Category = SObject.furnitureCategory });
                // 地板flooring
                for (int id = 0; id < 56; id++)
                    yield return this.TryCreate(ItemType.Flooring, id, p => new Wallpaper(p.ID, isFloor: true) { Category = SObject.furnitureCategory });
                // 鞋子Boots
                foreach (int id in this.TryLoad<int, string>("Data\\Boots").Keys)
                    yield return this.TryCreate(ItemType.Boots, id, p => new Boots(p.ID));
                // 帽子hats
                foreach (int id in this.TryLoad<int, string>("Data\\hats").Keys)
                    yield return this.TryCreate(ItemType.Hat, id, p => new Hat(p.ID));
                // 武器weapons
                foreach (int id in this.TryLoad<int, string>("Data\\weapons").Keys)
                {
                    yield return this.TryCreate(ItemType.Weapon, id, p => (p.ID >= 32 && p.ID <= 34)
                        ? (Item)new Slingshot(p.ID)
                        : new MeleeWeapon(p.ID)
                    );
                }
                // 家具furniture
                foreach (int id in this.TryLoad<int, string>("Data\\Furniture").Keys)
                    yield return this.TryCreate(ItemType.Furniture, id, p => Furniture.GetFurnitureInstance(p.ID));
                // 手工艺品craftables
                foreach (int id in Game1.bigCraftablesInformation.Keys)
                    yield return this.TryCreate(ItemType.BigCraftable, id, p => new SObject(Vector2.Zero, p.ID));
                // objects
                foreach (int id in Game1.objectInformation.Keys)
                {
                    string[] fields = Game1.objectInformation[id]?.Split('/');
                    // 秘密笔记SecretNotes
                    if (id == 79)
                    {
                        foreach (int secretNoteId in this.TryLoad<int, string>("Data\\SecretNotes").Keys)
                        {
                            yield return this.TryCreate(ItemType.Object, this.CustomIDOffset + secretNoteId, _ =>
                            {
                                SObject note = new SObject(79, 1);
                                note.name = $"{note.name} #{secretNoteId}";
                                return note;
                            });
                        }
                    }
                    // 戒指ring
                    else if (id != 801 && fields?.Length >= 4 && fields[3] == "Ring") // 801 = 结婚戒指，不属于装备戒指
                        yield return this.TryCreate(ItemType.Ring, id, p => new Ring(p.ID));
                    else
                    {
                        // 生成主物品
                        SObject item = null;
                        yield return this.TryCreate(ItemType.Object, id, p =>
                        {
                            return item = (p.ID == 812 // 鱼籽roe
                                ? new ColoredObject(p.ID, 1, Color.White)
                                : new SObject(p.ID, 1)
                            );
                        });
                        if (item == null)
                            continue;

                        // 风味物品flavored items
                        switch (item.Category)
                        {
                            // 水果fruit products
                            case SObject.FruitsCategory:
                                // 葡萄酒wine
                                yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 2 + item.ParentSheetIndex, _ => new SObject(348, 1)
                                {
                                    Name = $"{item.Name} Wine",
                                    Price = item.Price * 3,
                                    preserve = { SObject.PreserveType.Wine },
                                    preservedParentSheetIndex = { item.ParentSheetIndex }
                                });
                                // 果冻jelly
                                yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 3 + item.ParentSheetIndex, _ => new SObject(344, 1)
                                {
                                    Name = $"{item.Name} Jelly",
                                    Price = 50 + item.Price * 2,
                                    preserve = { SObject.PreserveType.Jelly },
                                    preservedParentSheetIndex = { item.ParentSheetIndex }
                                });
                                break;
                            // 蔬菜vegetable products
                            case SObject.VegetableCategory:
                                // 果汁juice
                                yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 4 + item.ParentSheetIndex, _ => new SObject(350, 1)
                                {
                                    Name = $"{item.Name} Juice",
                                    Price = (int)(item.Price * 2.25d),
                                    preserve = { SObject.PreserveType.Juice },
                                    preservedParentSheetIndex = { item.ParentSheetIndex }
                                });
                                // 腌制pickled
                                yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 5 + item.ParentSheetIndex, _ => new SObject(342, 1)
                                {
                                    Name = $"Pickled {item.Name}",
                                    Price = 50 + item.Price * 2,
                                    preserve = { SObject.PreserveType.Pickle },
                                    preservedParentSheetIndex = { item.ParentSheetIndex }
                                });
                                break;
                            // 花蜜flower honey
                            case SObject.flowersCategory:
                                yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 5 + item.ParentSheetIndex, _ =>
                                {
                                    SObject honey = new SObject(Vector2.Zero, 340, $"{item.Name} Honey", false, true, false, false)
                                    {
                                        Name = $"{item.Name} Honey",
                                        preservedParentSheetIndex = { item.ParentSheetIndex }
                                    };
                                    honey.Price += item.Price * 2;
                                    return honey;
                                });
                                break;
                            // 鱼籽部分 roe and aged roe (来源于 FishPond.GetFishProduce)
                            case SObject.sellAtFishShopCategory when item.ParentSheetIndex == 812:
                                {
                                    this.GetRoeContextTagLookups(out HashSet<string> simpleTags, out List<List<string>> complexTags);

                                    foreach (var pair in Game1.objectInformation)
                                    {
                                        // get input
                                        SObject input = this.TryCreate(ItemType.Object, pair.Key, p => new SObject(p.ID, 1))?.Item as SObject;
                                        var inputTags = input?.GetContextTags();
                                        if (inputTags?.Any() != true)
                                            continue;

                                        // check if roe-producing fish
                                        if (!inputTags.Any(tag => simpleTags.Contains(tag)) && !complexTags.Any(set => set.All(tag => input.HasContextTag(tag))))
                                            continue;
                                        
                                        // yield roe
                                        SObject roe = null;
                                        Color color = this.GetRoeColor(input);
                                        yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 7 + item.ParentSheetIndex, _ =>
                                        {
                                            roe = new ColoredObject(812, 1, color)
                                            {
                                                name = $"{input.Name} Roe",
                                                preserve = { Value = SObject.PreserveType.Roe },
                                                preservedParentSheetIndex = { Value = input.ParentSheetIndex }
                                            };
                                            roe.Price += input.Price / 2;
                                            return roe;
                                        });

                                        // aged roe
                                        if (roe != null && pair.Key != 698) // 老鲟鱼籽是鱼子酱，是一个单独的项目
                                        {
                                            yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 7 + item.ParentSheetIndex, _ => new ColoredObject(447, 1, color)
                                            {
                                                name = $"Aged {input.Name} Roe",
                                                Category = -27,
                                                preserve = { Value = SObject.PreserveType.AgedRoe },
                                                preservedParentSheetIndex = { Value = input.ParentSheetIndex },
                                                Price = roe.Price * 2
                                            });
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
            }

            return GetAllRaw().Where(p => p != null);
        }

        /*********
        ** Private methods
        *********/
        /// <summary>获取优化查找匹配可以生在鱼塘的鱼籽</summary>
        /// <param name="simpleTags">单数标签匹配鱼籽生产的鱼的查找</param>
        /// <param name="complexTags">匹配鱼籽的标记列表</param>
        private void GetRoeContextTagLookups(out HashSet<string> simpleTags, out List<List<string>> complexTags)
        {
            simpleTags = new HashSet<string>();
            complexTags = new List<List<string>>();

            foreach (FishPondData data in Game1.content.Load<List<FishPondData>>("Data\\FishPondData"))
            {
                if (data.ProducedItems.All(p => p.ItemID != 812))
                    continue; // 不产生鱼籽

                if (data.RequiredTags.Count == 1 && !data.RequiredTags[0].StartsWith("!"))
                    simpleTags.Add(data.RequiredTags[0]);
                else
                    complexTags.Add(data.RequiredTags);
            }
        }

        /// <summary>尝试加载数据文件，如果无效则返回空数据</summary>
        /// <param name="assetName">资源名称</param>
        private Dictionary<TKey, TValue> TryLoad<TKey, TValue>(string assetName)
        {
            try
            {
                return Game1.content.Load<Dictionary<TKey, TValue>>(assetName);
            }
            catch (ContentLoadException)
            {
                // 通常是因为玩家错误的替换XNB MOD才会导致
                return new Dictionary<TKey, TValue>();
            }
        }

        /// <summary>数据有效就创建一个Searchable对象</summary>
        private SearchableItem TryCreate(ItemType type, int id, Func<SearchableItem, Item> createItem)
        {
            try
            {
                // 强制加载数据，无效的话会报错被捕获
                var item = new SearchableItem(type, id, createItem);
                item.Item.getDescription(); 
                return item;
            }
            catch
            {
                // 无效物品数据则排除
                return null;
            }
        }

        /// <summary>获取可让水塘变色的女的颜色</summary>
        /// <param name="fish">可变色的鱼</param>
        /// <remarks>派生自 <see cref="StardewValley.Buildings.FishPond.GetFishProduce"/>.</remarks>
        private Color GetRoeColor(SObject fish)
        {
            return fish.ParentSheetIndex == 698 // 鲟鱼sturgeon
                ? new Color(61, 55, 42)
                : (TailoringMenu.GetDyeColor(fish) ?? Color.Orange);
        }
    }
}
