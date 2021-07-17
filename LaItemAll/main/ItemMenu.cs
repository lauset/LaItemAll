using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LaItemAll.Common;
using LaItemAll.Common.UI;
using LaItemAll.main.Constants;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using SConstants = StardewModdingAPI.Constants;
using SObject = StardewValley.Object;

namespace LaItemAll.main
{
    /// <summary>生成菜单，玩家可以添加任何物品至库存和丢弃库存中的物品</summary>
    internal class ItemMenu : ItemGrabMenu
    {
        /*********
        ** Constants & Fields
        *********/
        /// <summary>UI可以一次显示的最大物品数，3行</summary>
        private readonly int ItemsPerView = Chest.capacity;
        /// <summary>每行可显示的最大物品数</summary>
        private readonly int ItemsPerRow = Chest.capacity / 3;
        /// <summary>没有品级的物品ID</summary>
        private readonly ISet<int> ItemsWithoutQuality = new HashSet<int>
        {
            447, // aged roe
            812 // roe
        };
        /// <summary>可用的类别名称</summary>
        private readonly string[] Categories;
        /// <summary>是否正在Android上显示菜单</summary>
        private bool IsAndroid => SConstants.TargetPlatform == GamePlatform.Android;
        /// <summary>排序框里前面腾出的空间，方便放排序图标</summary>
        private readonly string SortLabelIndent;

        /****
        ** State
        ****/
        /// <summary>处理对SMAPI控制台和日志的写入</summary>
        private readonly IMonitor Monitor;
        /// <summary>基础绘制方法</summary>
        /// <remarks>This circumvents an issue where <see cref="ItemGrabMenu.draw(SpriteBatch)"/> can't be called directly due to a conflicting overload.</remarks>
        private readonly Action<SpriteBatch> BaseDraw;
        /// <summary>默认品质等级图标</summary>
        private readonly Texture2D StarOutlineTexture;
        /// <summary>排序按钮</summary>
        private readonly Texture2D SortTexture;
        /// <summary>当前物品品质</summary>
        private ItemQuality Quality = ItemQuality.Normal;
        /// <summary>对物品排序的字段，默认名称排序</summary>
        private ItemSort SortBy = ItemSort.DisplayName;
        /// <summary>所有可以生成的物品</summary>
        private readonly SpawnableItem[] AllItems;
        /// <summary>与当前搜索框匹配的物品，不滚动</summary>
        private readonly List<SpawnableItem> FilteredItems = new List<SpawnableItem>();
        /// <summary>当前显示在UI的物品列表</summary>
        private readonly IList<Item> ItemsInView;
        /// <summary>UI视图中显示的顶行的索引，用于滚动浏览结果</summary>
        private int TopRowIndex;
        /// <summary>物品最大数量 <see cref="TopRowIndex"/></summary>
        private int MaxTopRowIndex;
        /// <summary>是否可以上滑菜单</summary>
        private bool CanScrollUp => this.TopRowIndex > 0;
        /// <summary>是否可以下滑菜单</summary>
        private bool CanScrollDown => this.TopRowIndex < this.MaxTopRowIndex;

        /****
        ** UI components
        ****/
        /// <summary>排序图标</summary>
        private ClickableTextureComponent SortIcon;
        /// <summary>排序按钮</summary>
        private ClickableComponent SortButton;
        /// <summary>品级按钮</summary>
        private ClickableComponent QualityButton;
        /// <summary>种类下拉框</summary>
        private Dropdown<string> CategoryDropdown;
        /// <summary>上滚箭头</summary>
        private ClickableTextureComponent UpArrow;
        /// <summary>下滚箭头</summary>
        private ClickableTextureComponent DownArrow;

        /*********
        ** Accessors
        *********/
        /// <summary>控制器捕获的子组件</summary>
        /// <remarks>必须公开，并且与支持的类型匹配 <see cref="IClickableMenu.populateClickableComponentList"/>.</remarks>
        public readonly List<ClickableComponent> ChildComponents = new List<ClickableComponent>();

        /*********
        ** Public methods
        *********/
        /// <summary>构造</summary>
        /// <param name="spawnableItems">可生产的物品</param>
        /// <param name="content">用于加载资源的上下文助手</param>
        /// <param name="monitor">日志处理</param>
        public ItemMenu(SpawnableItem[] spawnableItems, IContentHelper content, IMonitor monitor)
            : base(
                inventory: new List<Item>(),
                reverseGrab: false,
                showReceivingMenu: true,
                highlightFunction: item => true,
                behaviorOnItemGrab: (item, player) => { },
                behaviorOnItemSelectFunction: (item, player) => { },
                message: null,
                canBeExitedWithKey: true,
                showOrganizeButton: false,
                source: SConstants.TargetPlatform == GamePlatform.Android ? ItemGrabMenu.source_chest : ItemGrabMenu.source_none // Android 需要避免格式错误的UI
            )
        {
            // 初始化设置
            this.Monitor = monitor;
            this.BaseDraw = this.GetBaseDraw();
            this.ItemsInView = this.ItemsToGrabMenu.actualInventory;
            this.AllItems = spawnableItems;
            this.Categories = this.GetDisplayCategories(spawnableItems).ToArray();

            // 初始化静态资源
            this.StarOutlineTexture = content.Load<Texture2D>("assets/empty-quality.png");
            this.SortTexture = content.Load<Texture2D>("assets/sort.png");
            this.SortLabelIndent = this.GetSpaceIndent(Game1.smallFont, this.SortTexture.Width) + " ";

            // 初始化基础UI
            // Android上手动在背景和菜单之间绘制箭头，并修复所有平台上的UI缩放问题
            this.drawBG = false; 
            this.behaviorOnItemGrab = this.OnItemGrab;

            // 初始化自定义UI
            this.InitializeComponents();
            this.ResetItemView(rebuild: true);
        }

        /// <summary>点击鼠标左键</summary>
        /// <param name="x">鼠标X位置</param>
        /// <param name="y">鼠标Y位置</param>
        /// <param name="playSound">是否播放互动声音</param>
        /// <returns>事件已处理，不进一步传播</returns>
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            // 允许仍掉任何物品
            if (this.trashCan.containsPoint(x, y) && this.heldItem != null)
            {
                Utility.trashItem(this.heldItem);
                this.heldItem = null;
            }
            // 排序按钮
            else if (this.SortButton.bounds.Contains(x, y))
            {
                this.SortBy = this.SortBy.GetNext();
                this.SortButton.label = this.SortButton.name = this.GetSortLabel(this.SortBy);
                this.ResetItemView(rebuild: true);
            }
            // 品质按钮
            else if (this.QualityButton.bounds.Contains(x, y))
            {
                this.Quality = this.Quality.GetNext();
                this.ResetItemView();
            }
            // 滚动按钮
            else if (this.UpArrow.bounds.Contains(x, y))
                this.receiveScrollWheelAction(1);
            else if (this.DownArrow.bounds.Contains(x, y))
                this.receiveScrollWheelAction(-1);
            // 种类下拉框
            else if (this.CategoryDropdown.TryClick(x, y, out bool itemClicked, out bool dropdownToggled))
            {
                if (dropdownToggled)
                    this.SetDropdown(this.CategoryDropdown.IsExpanded);
                if (itemClicked)
                    this.SetCategory(this.CategoryDropdown.Selected);
            }
            // 其它回调
            else
            {
                // 默认处理行为
                base.receiveLeftClick(x, y, playSound);
            }

        }

        /// <summary>点击鼠标右键</summary>
        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            // 关闭下拉框
            if (this.CategoryDropdown.IsExpanded)
                this.SetDropdown(false);
            // 默认处理行为
            else
                base.receiveRightClick(x, y, playSound);
        }

        /// <summary>键盘按下</summary>
        public override void receiveKeyPress(Keys key)
        {
            bool inDropdown = this.CategoryDropdown.IsExpanded;
            bool isEscape = key == Keys.Escape;
            // 判断是否是退出功能的按钮，Esc，菜单键，取消键
            bool isExitButton =
                isEscape
                || Game1.options.doesInputListContain(Game1.options.menuButton, key)
                || Game1.options.doesInputListContain(Game1.options.cancelButton, key);

            // 如果下拉框展开并且按的退出键那么就关闭下拉框
            if (inDropdown && isExitButton)
                this.SetDropdown(false);
            // 按下Delete并且手里的东西不为空，扔垃圾
            else if (key == Keys.Delete && this.heldItem != null)
            {
                Utility.trashItem(this.heldItem);
                this.heldItem = null;
            }
            // 左右按键切换到下一个类别
            else if (key == Keys.Left || key == Keys.Right)
            {
                int direction = key == Keys.Left ? -1 : 1;
                this.NextCategory(direction);
            }
            // 上下按键滚动视图
            else if (key == Keys.Up || key == Keys.Down)
            {
                int direction = key == Keys.Up ? -1 : 1;

                if (inDropdown)
                    this.CategoryDropdown.ReceiveScrollWheelAction(direction);
                else
                    this.ScrollView(direction);
            }
            // 默认处理行为
            else
            {
                base.receiveKeyPress(key);
            }
        }

        /// <summary>处理控制器按下的键</summary>
        public override void receiveGamePadButton(Buttons button)
        {
            // B、Y、Start键为退出键
            bool isExitKey = button == Buttons.B || button == Buttons.Y || button == Buttons.Start;
            bool inDropdown = this.CategoryDropdown.IsExpanded;
            // 左右键切换导航栏类别
            if (button == Buttons.LeftTrigger && !inDropdown)
                this.NextCategory(-1);
            else if (button == Buttons.RightTrigger && !inDropdown)
                this.NextCategory(1);
            else
                base.receiveGamePadButton(button);
        }

        /// <summary>滚动鼠标</summary>
        /// <param name="direction">滚动方向</param>
        public override void receiveScrollWheelAction(int direction)
        {
            base.receiveScrollWheelAction(direction);
            // 如果种类筛选框展开那么滚动种类
            if (this.CategoryDropdown.IsExpanded)
                this.CategoryDropdown.ReceiveScrollWheelAction(direction);
            // 否则滚动列表视图
            else
                this.ScrollView(-direction);
        }

        /// <summary>鼠标悬浮到菜单</summary>
        public override void performHoverAction(int x, int y)
        {
            // 基本逻辑
            base.performHoverAction(x, y);
        }

        /// <summary>绘制菜单</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        public override void draw(SpriteBatch spriteBatch)
        {
            // 绘制背景覆盖
            spriteBatch.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);
            // 绘制滚动箭头
            if (this.CanScrollUp)
                this.UpArrow.draw(spriteBatch);
            if (this.CanScrollDown)
                this.DownArrow.draw(spriteBatch);
            this.BaseDraw(spriteBatch);
            // 绘制搜索框
            // ...
            // 绘制品质，排序按钮
            CommonHelper.DrawTab(this.QualityButton.bounds.X, this.QualityButton.bounds.Y, this.QualityButton.bounds.Width - CommonHelper.ButtonBorderWidth, this.QualityButton.bounds.Height - CommonHelper.ButtonBorderWidth, out Vector2 qualityIconPos, drawShadow: this.IsAndroid);
            CommonHelper.DrawTab(this.SortButton.bounds.X, this.SortButton.bounds.Y, Game1.smallFont, this.SortButton.name, drawShadow: this.IsAndroid);
            this.SortIcon.draw(spriteBatch);
            // 绘制品质图标
            {
                this.GetQualityIcon(out Texture2D texture, out Rectangle sourceRect, out Color color);
                spriteBatch.Draw(texture, new Vector2(qualityIconPos.X, qualityIconPos.Y - 1 * Game1.pixelZoom), sourceRect, color, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 1f);
            }
            // 绘制种类下拉框
            {
                Vector2 position = new Vector2(
                    x: this.CategoryDropdown.bounds.X + this.CategoryDropdown.bounds.Width - 3 * Game1.pixelZoom,
                    y: this.CategoryDropdown.bounds.Y + 2 * Game1.pixelZoom
                );
                Rectangle sourceRect = new Rectangle(437, 450, 10, 11);
                spriteBatch.Draw(Game1.mouseCursors, position, sourceRect, Color.White, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 1f);
                if (this.CategoryDropdown.IsExpanded)
                    spriteBatch.Draw(Game1.mouseCursors, new Vector2(position.X + 2 * Game1.pixelZoom, position.Y + 3 * Game1.pixelZoom), new Rectangle(sourceRect.X + 2, sourceRect.Y + 3, 5, 6), Color.White, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.FlipVertically, 1f);  // right triangle
                this.CategoryDropdown.Draw(spriteBatch);
            }
            // 在新UI上重新绘制光标
            this.drawMouse(spriteBatch);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>获取绘制品质按钮图标</summary>
        /// <param name="texture">包含图标的纹理</param>
        /// <param name="sourceRect">纹理图标的像素区域</param>
        /// <param name="color">图标颜色和透明度</param>
        private void GetQualityIcon(out Texture2D texture, out Rectangle sourceRect, out Color color)
        {
            texture = Game1.mouseCursors;
            color = Color.White;
            switch (this.Quality)
            {
                case ItemQuality.Normal:
                    texture = this.StarOutlineTexture;
                    sourceRect = new Rectangle(0, 0, texture.Width, texture.Height);
                    color = color * 0.65f;
                    break;
                case ItemQuality.Silver:
                    sourceRect = new Rectangle(338, 400, 8, 8);
                    break;
                case ItemQuality.Gold:
                    sourceRect = new Rectangle(346, 400, 8, 8);
                    break;
                default:
                    sourceRect = new Rectangle(346, 392, 8, 8);
                    break;
            }
        }

        /// <summary>获取类型显示到UI上</summary>
        /// <param name="items">可被生成的物品数组</param>
        private IEnumerable<string> GetDisplayCategories(SpawnableItem[] items)
        {
            string all = I18n.Filter_All();
            string misc = I18n.Filter_Miscellaneous();

            HashSet<string> categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (SpawnableItem item in items)
            {
                if (this.EqualsCaseInsensitive(item.Category, all) || this.EqualsCaseInsensitive(item.Category, misc))
                    continue;
                categories.Add(item.Category);
            }

            yield return all;
            foreach (string category in categories.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                yield return category;
            yield return misc;
        }

        /// <summary>初始化自定义UI组件</summary>
        private void InitializeComponents()
        {
            // 获取基础坐标位置
            int x = this.xPositionOnScreen;
            int y = this.yPositionOnScreen;
            int right = x + this.width;
            int top = this.IsAndroid
                ? y - (CommonSprites.Tab.Top.Height * Game1.pixelZoom) // at top of screen, moved up slightly to reduce overlap over items
                : y - Game1.tileSize * 2 + 10; // above menu

            // 基础UI
            this.QualityButton = new ClickableComponent(new Rectangle(x - 2 * Game1.pixelZoom, top, 9 * Game1.pixelZoom + CommonHelper.ButtonBorderWidth, 9 * Game1.pixelZoom + CommonHelper.ButtonBorderWidth - 2), ""); // manually tweak height to align with sort button
            this.SortButton = new ClickableComponent(new Rectangle(this.QualityButton.bounds.Right + 20, top, this.GetMaxSortLabelWidth(Game1.smallFont) + CommonHelper.ButtonBorderWidth, Game1.tileSize), this.GetSortLabel(this.SortBy));
            this.SortIcon = new ClickableTextureComponent(new Rectangle(this.SortButton.bounds.X + CommonHelper.ButtonBorderWidth, top + CommonHelper.ButtonBorderWidth, this.SortTexture.Width, Game1.tileSize), this.SortTexture, new Rectangle(0, 0, this.SortTexture.Width, this.SortTexture.Height), 1f);
            this.CategoryDropdown = new Dropdown<string>(this.SortButton.bounds.Right + 20, this.SortButton.bounds.Y, Game1.smallFont, this.CategoryDropdown?.Selected ?? I18n.Filter_All(), this.Categories, p => p);
            this.UpArrow = new ClickableTextureComponent(new Rectangle(right - 32, y - 64, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom), Game1.mouseCursors, new Rectangle(421, 459, 11, 12), Game1.pixelZoom);
            this.DownArrow = new ClickableTextureComponent(new Rectangle(this.UpArrow.bounds.X, this.UpArrow.bounds.Y + this.height / 2 - 64, this.UpArrow.bounds.Width, this.UpArrow.bounds.Height), Game1.mouseCursors, new Rectangle(421, 472, 11, 12), Game1.pixelZoom);

            // 搜索框
            // ...

            // 移动Android布局
            if (this.IsAndroid)
            {
                this.UpArrow.bounds.X = this.upperRightCloseButton.bounds.Center.X - this.SortButton.bounds.Width / 2;
                this.UpArrow.bounds.Y = this.upperRightCloseButton.bounds.Bottom;
                this.DownArrow.bounds.X = this.UpArrow.bounds.X;
            }

            // 控制器按键顺序
            this.InitializeControllerFlow();
        }

        /// <summary>设置字段支持控制器捕获</summary>
        private void InitializeControllerFlow()
        {
            //   - 排除搜索框，因为不可输入
            //   - 下拉框 ID 自动管理，取决于是否被展开

            // Android无控制器，可能会崩溃
            if (this.IsAndroid)
                return;

            List<ClickableComponent> slots = this.ItemsToGrabMenu.inventory;

            // 修改组件列表
            this.ChildComponents.Clear();
            this.ChildComponents.AddRange(new[] { this.QualityButton, this.SortButton, this.UpArrow, this.DownArrow, this.CategoryDropdown });

            // 设置 IDs
            {
                int curId = 1_000_000;
                foreach (ClickableComponent component in this.ChildComponents)
                    component.myID = curId++;
            }

            Console.WriteLine(slots);

            // 自定义控制器右键流动ID
            this.QualityButton.rightNeighborID = this.SortButton.myID;
            this.SortButton.rightNeighborID = this.CategoryDropdown.myID;
            this.CategoryDropdown.rightNeighborID = this.UpArrow.myID;
            this.UpArrow.downNeighborID = this.DownArrow.myID;

            // 左
            this.UpArrow.upNeighborID = this.CategoryDropdown.myID;
            this.UpArrow.leftNeighborID = slots[10].myID;
            this.CategoryDropdown.leftNeighborID = this.SortButton.myID;
            this.SortButton.leftNeighborID = this.QualityButton.myID;

            // 下
            this.QualityButton.downNeighborID = slots[0].myID;
            this.SortButton.downNeighborID = slots[1].myID;
            this.CategoryDropdown.DefaultDownNeighborId = slots[5].myID;
            this.DownArrow.leftNeighborID = slots.Last().myID;
            this.DownArrow.downNeighborID = this.trashCan.myID;

            // 上
            slots[0].upNeighborID = this.QualityButton.myID;
            foreach (int i in new[] { 1, 2 })
                slots[i].upNeighborID = this.SortButton.myID;
            foreach (int i in new[] { 3, 4, 5, 6, 8, 9, 10, 11 })
                slots[i].upNeighborID = this.QualityButton.myID;
            foreach (int i in new[] { 11, 23 })
                slots[i].rightNeighborID = this.UpArrow.myID;
            slots.Last().rightNeighborID = this.DownArrow.myID;
            this.trashCan.upNeighborID = this.DownArrow.myID;

            // 下拉条目
            this.CategoryDropdown.ReinitializeControllerFlow();
            this.ChildComponents.AddRange(this.CategoryDropdown.GetChildComponents());

            // 修改组件列表
            this.populateClickableComponentList();
        }

        /// <summary>处理从菜单上选择一个物品</summary>
        /// <param name="item">从菜单中抓取的物品</param>
        /// <param name="player">谁抓的</param>
        private void OnItemGrab(Item item, Farmer player)
        {
            this.ResetItemView();
        }

        /// <summary>展开或折叠种类下拉框</summary>
        /// <param name="expanded">是否展开</param>
        protected void SetDropdown(bool expanded)
        {
            this.CategoryDropdown.IsExpanded = expanded;
            this.inventory.highlightMethod = item => !expanded;
            this.ItemsToGrabMenu.highlightMethod = item => !expanded;
            if (!expanded && !Game1.lastCursorMotionWasMouse)
            {
                this.setCurrentlySnappedComponentTo(this.CategoryDropdown.myID);
                this.snapCursorToCurrentSnappedComponent();
            }
        }

        /// <summary>切换到下个类别</summary>
        /// <param name="direction">切换的方向</param>
        protected void NextCategory(int direction)
        {
            direction = direction < 0 ? -1 : 1;
            int last = this.Categories.Length - 1;

            int index = Array.IndexOf(this.Categories, this.CategoryDropdown.Selected) + direction;
            if (index < 0)
                index = last;
            if (index > last)
                index = 0;

            this.SetCategory(this.Categories[index]);
        }

        /// <summary>设置当前的类别</summary>
        /// <param name="category">新的类别值</param>
        protected void SetCategory(string category)
        {
            if (!this.CategoryDropdown.TrySelect(category))
            {
                this.Monitor.Log($"Failed selecting category filter category '{category}'.", LogLevel.Warn);
                if (category != I18n.Filter_All())
                    this.SetCategory(I18n.Filter_All());
                return;
            }
            this.ResetItemView(rebuild: true);
        }

        /// <summary>滚动物品视图</summary>
        /// <param name="direction">滚动方向</param>
        /// <param name="resetItemView">是否更新物品视图</param>
        public void ScrollView(int direction, bool resetItemView = true)
        {
            // 应用滚动
            if (direction < 0)
                this.TopRowIndex--;
            else if (direction > 0)
                this.TopRowIndex++;
            // 规格化滚动
            this.TopRowIndex = (int)MathHelper.Clamp(this.TopRowIndex, 0, this.MaxTopRowIndex);
            // 修改视图
            if (resetItemView)
                this.ResetItemView();
        }

        /// <summary>重置物品视图显示</summary>
        /// <param name="rebuild">是否重置搜索结果</param>
        private void ResetItemView(bool rebuild = false)
        {
            if (rebuild)
            {
                this.FilteredItems.Clear();
                this.FilteredItems.AddRange(this.SearchItems());
                this.TopRowIndex = 0;
            }
            // 如果需要则修复滚动
            int totalRows = (int)Math.Ceiling(this.FilteredItems.Count / (this.ItemsPerRow * 1m));
            this.MaxTopRowIndex = Math.Max(0, totalRows - 3);
            this.ScrollView(0, resetItemView: false);
            // 修改视图中物品
            this.ItemsInView.Clear();
            foreach (var match in this.FilteredItems.Skip(this.TopRowIndex * this.ItemsPerRow).Take(this.ItemsPerView))
            {
                Item item = match.CreateItem();
                item.Stack = item.maximumStackSize();
                if (item is SObject obj && !this.ItemsWithoutQuality.Contains(obj.ParentSheetIndex))
                    obj.Quality = (int)this.Quality;

                this.ItemsInView.Add(item);
            }
        }

        /// <summary>获取与搜索条件匹配的所有项目，忽略分页</summary>
        private IEnumerable<SpawnableItem> SearchItems()
        {
            // 基本查询
            IEnumerable<SpawnableItem> items = this.AllItems;
            items = this.SortBy switch
            {
                ItemSort.Type => items.OrderBy(p => p.Item.Category),
                ItemSort.ID => items.OrderBy(p => p.Item.ParentSheetIndex),
                _ => items.OrderBy(p => p.Item.DisplayName)
            };

            // 下拉框
            if (!this.EqualsCaseInsensitive(this.CategoryDropdown.Selected, I18n.Filter_All()))
                items = items.Where(item => this.EqualsCaseInsensitive(item.Category, this.CategoryDropdown.Selected));

            // 搜索
            // ...

            return items;
        }

        /// <summary>获取指定宽度的字符串缩进</summary>
        /// <param name="font">用来测量缩进大小的字体</param>
        /// <param name="width">以像素为单位最小缩进宽度</param>
        private string GetSpaceIndent(SpriteFont font, int width)
        {
            if (width <= 0)
                return "";

            string indent = " ";
            while (font.MeasureString(indent).X < width)
                indent += " ";

            return indent;
        }

        /// <summary>获取翻译后的排序标签</summary>
        /// <param name="sort">排序类型</param>
        private string GetSortLabel(ItemSort sort)
        {
            return this.SortLabelIndent + sort switch
            {
                ItemSort.DisplayName => I18n.Sort_ByName(),
                ItemSort.Type => I18n.Sort_ByType(),
                ItemSort.ID => I18n.Sort_ById(),
                _ => throw new NotSupportedException($"Invalid sort type {sort}.")
            };
        }

        /// <summary>获取排序标签最大宽度</summary>
        /// <param name="font">参照字体</param>
        private int GetMaxSortLabelWidth(SpriteFont font)
        {
            return
                (
                    from ItemSort key in Enum.GetValues(typeof(ItemSort))
                    let text = this.GetSortLabel(key)
                    select (int)font.MeasureString(text).X
                )
                .Max();
        }

        /// <summary>Get an action wrapper which invokes <see cref="ItemGrabMenu.draw(SpriteBatch)"/>.</summary>
        /// <remarks>See remarks on <see cref="BaseDraw"/>.</remarks>
        private Action<SpriteBatch> GetBaseDraw()
        {
            MethodInfo method = typeof(ItemGrabMenu).GetMethod("draw", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(SpriteBatch) }, null) ?? throw new InvalidOperationException($"Can't find {nameof(ItemGrabMenu)}.{nameof(ItemGrabMenu.draw)} method.");
            IntPtr pointer = method.MethodHandle.GetFunctionPointer();
            return (Action<SpriteBatch>)Activator.CreateInstance(typeof(Action<SpriteBatch>), this, pointer);
        }

        /// <summary>判断两个字符串是否相等，忽略大小写差异</summary>
        private bool EqualsCaseInsensitive(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
