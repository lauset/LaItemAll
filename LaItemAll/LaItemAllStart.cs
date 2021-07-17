using System;
using System.Collections.Generic;
using System.Linq;
using LaItemAll.main;
using LaItemAll.main.ItemData;
using LaItemAll.main.Models;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace LaItemAll
{
    internal class ModEntry : Mod
    {
        /*********
        ** Fields
        *********/
        /// <summary>mod设置</summary>
        private ModConfig Config;

        /// <summary>关于项目的内部mod数据</summary>
        private ModItemData ItemData;

        /// <summary>菜单中的所有筛选类别</summary>
        private ModDataCategory[] Categories;

        /*********
        ** Public methods
        *********/
        public override void Entry(IModHelper helper)
        {
            // 读取Mod配置
            this.Config = helper.ReadConfig<ModConfig>();
            this.Monitor.Log($"Started with menu key {this.Config.ShowMenuKey}.");

            // 读取有问题的物品项
            this.ItemData = helper.Data.ReadJsonFile<ModItemData>("assets/item-data.json");
            if (this.ItemData?.ProblematicItems == null)
                this.Monitor.Log("One of the mod files (assets/item-data.json) is missing or invalid. Some features may not work correctly; consider reinstalling the mod.", LogLevel.Warn);

            // 读取物品种类数据文件
            this.Categories = helper.Data.ReadJsonFile<ModDataCategory[]>("assets/categories.json");
            if (this.Categories == null)
                this.Monitor.LogOnce("One of the mod files (assets/categories.json) is missing or invalid. Some features may not work correctly; consider reinstalling the mod.", LogLevel.Warn);

            // 初始化语言，新增按钮变换事件和游戏状态更新事件
            I18n.Init(helper.Translation);
            helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;
            //helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        }

        /*********
        ** Private methods
        *********/
        /// <summary>在玩家按下或释放键盘、控制器或鼠标上的任何按钮后引发</summary>
        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            // 判断角色是否处于空闲状态
            if (!Context.IsPlayerFree)
                return;
            // 如果配置中按键刚刚被按下，那么触发生成菜单事件
            if (this.Config.ShowMenuKey.JustPressed())
                Game1.activeClickableMenu = this.BuildMenu();
        }

        /// <summary>游戏状态修改后触发 (≈60次/秒).</summary>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {

        }

        /// <summary>构建生成菜单</summary>
        private ItemMenu BuildMenu()
        {
            SpawnableItem[] items = this.GetSpawnableItems().ToArray();
            return new ItemMenu(items, this.Helper.Content, this.Monitor);
        }

        /// <summary>获取可以生成的物品</summary>
        private IEnumerable<SpawnableItem> GetSpawnableItems()
        {
            // 从物品仓库类中获取所有物品
            IEnumerable<SearchableItem> items = new ItemRepository().GetAll();
            // 处理有问题的物品，选择是否过滤掉有问题的物品项
            if (!this.Config.AllowProblematicItems && this.ItemData?.ProblematicItems?.Any() == true)
            {
                var problematicItems = new HashSet<string>(this.ItemData.ProblematicItems, StringComparer.OrdinalIgnoreCase);
                items = items.Where(item => !problematicItems.Contains($"{item.Type}:{item.ID}"));
            }
            // yield 迭代对象，将对象组装成SpawnableItem对象
            foreach (SearchableItem entry in items)
            {
                ModDataCategory category = this.Categories?.FirstOrDefault(rule => rule.IsMatch(entry));
                string categoryLabel = category != null
                    ? I18n.GetByKey(category.Label).Default(category.Label)
                    : I18n.Filter_Miscellaneous();
                yield return new SpawnableItem(entry, categoryLabel);
            }
        }
    }
}
