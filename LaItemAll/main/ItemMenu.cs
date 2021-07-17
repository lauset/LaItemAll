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
    /// <summary>���ɲ˵�����ҿ�������κ���Ʒ�����Ͷ�������е���Ʒ</summary>
    internal class ItemMenu : ItemGrabMenu
    {
        /*********
        ** Constants & Fields
        *********/
        /// <summary>UI����һ����ʾ�������Ʒ����3��</summary>
        private readonly int ItemsPerView = Chest.capacity;
        /// <summary>ÿ�п���ʾ�������Ʒ��</summary>
        private readonly int ItemsPerRow = Chest.capacity / 3;
        /// <summary>û��Ʒ������ƷID</summary>
        private readonly ISet<int> ItemsWithoutQuality = new HashSet<int>
        {
            447, // aged roe
            812 // roe
        };
        /// <summary>���õ��������</summary>
        private readonly string[] Categories;
        /// <summary>�Ƿ�����Android����ʾ�˵�</summary>
        private bool IsAndroid => SConstants.TargetPlatform == GamePlatform.Android;
        /// <summary>�������ǰ���ڳ��Ŀռ䣬���������ͼ��</summary>
        private readonly string SortLabelIndent;

        /****
        ** State
        ****/
        /// <summary>�����SMAPI����̨����־��д��</summary>
        private readonly IMonitor Monitor;
        /// <summary>�������Ʒ���</summary>
        /// <remarks>This circumvents an issue where <see cref="ItemGrabMenu.draw(SpriteBatch)"/> can't be called directly due to a conflicting overload.</remarks>
        private readonly Action<SpriteBatch> BaseDraw;
        /// <summary>Ĭ��Ʒ�ʵȼ�ͼ��</summary>
        private readonly Texture2D StarOutlineTexture;
        /// <summary>����ť</summary>
        private readonly Texture2D SortTexture;
        /// <summary>��ǰ��ƷƷ��</summary>
        private ItemQuality Quality = ItemQuality.Normal;
        /// <summary>����Ʒ������ֶΣ�Ĭ����������</summary>
        private ItemSort SortBy = ItemSort.DisplayName;
        /// <summary>���п������ɵ���Ʒ</summary>
        private readonly SpawnableItem[] AllItems;
        /// <summary>�뵱ǰ������ƥ�����Ʒ��������</summary>
        private readonly List<SpawnableItem> FilteredItems = new List<SpawnableItem>();
        /// <summary>��ǰ��ʾ��UI����Ʒ�б�</summary>
        private readonly IList<Item> ItemsInView;
        /// <summary>UI��ͼ����ʾ�Ķ��е����������ڹ���������</summary>
        private int TopRowIndex;
        /// <summary>��Ʒ������� <see cref="TopRowIndex"/></summary>
        private int MaxTopRowIndex;
        /// <summary>�Ƿ�����ϻ��˵�</summary>
        private bool CanScrollUp => this.TopRowIndex > 0;
        /// <summary>�Ƿ�����»��˵�</summary>
        private bool CanScrollDown => this.TopRowIndex < this.MaxTopRowIndex;

        /****
        ** UI components
        ****/
        /// <summary>����ͼ��</summary>
        private ClickableTextureComponent SortIcon;
        /// <summary>����ť</summary>
        private ClickableComponent SortButton;
        /// <summary>Ʒ����ť</summary>
        private ClickableComponent QualityButton;
        /// <summary>����������</summary>
        private Dropdown<string> CategoryDropdown;
        /// <summary>�Ϲ���ͷ</summary>
        private ClickableTextureComponent UpArrow;
        /// <summary>�¹���ͷ</summary>
        private ClickableTextureComponent DownArrow;

        /*********
        ** Accessors
        *********/
        /// <summary>����������������</summary>
        /// <remarks>���빫����������֧�ֵ�����ƥ�� <see cref="IClickableMenu.populateClickableComponentList"/>.</remarks>
        public readonly List<ClickableComponent> ChildComponents = new List<ClickableComponent>();

        /*********
        ** Public methods
        *********/
        /// <summary>����</summary>
        /// <param name="spawnableItems">����������Ʒ</param>
        /// <param name="content">���ڼ�����Դ������������</param>
        /// <param name="monitor">��־����</param>
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
                source: SConstants.TargetPlatform == GamePlatform.Android ? ItemGrabMenu.source_chest : ItemGrabMenu.source_none // Android ��Ҫ�����ʽ�����UI
            )
        {
            // ��ʼ������
            this.Monitor = monitor;
            this.BaseDraw = this.GetBaseDraw();
            this.ItemsInView = this.ItemsToGrabMenu.actualInventory;
            this.AllItems = spawnableItems;
            this.Categories = this.GetDisplayCategories(spawnableItems).ToArray();

            // ��ʼ����̬��Դ
            this.StarOutlineTexture = content.Load<Texture2D>("assets/empty-quality.png");
            this.SortTexture = content.Load<Texture2D>("assets/sort.png");
            this.SortLabelIndent = this.GetSpaceIndent(Game1.smallFont, this.SortTexture.Width) + " ";

            // ��ʼ������UI
            // Android���ֶ��ڱ����Ͳ˵�֮����Ƽ�ͷ�����޸�����ƽ̨�ϵ�UI��������
            this.drawBG = false; 
            this.behaviorOnItemGrab = this.OnItemGrab;

            // ��ʼ���Զ���UI
            this.InitializeComponents();
            this.ResetItemView(rebuild: true);
        }

        /// <summary>���������</summary>
        /// <param name="x">���Xλ��</param>
        /// <param name="y">���Yλ��</param>
        /// <param name="playSound">�Ƿ񲥷Ż�������</param>
        /// <returns>�¼��Ѵ�������һ������</returns>
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            // �����Ե��κ���Ʒ
            if (this.trashCan.containsPoint(x, y) && this.heldItem != null)
            {
                Utility.trashItem(this.heldItem);
                this.heldItem = null;
            }
            // ����ť
            else if (this.SortButton.bounds.Contains(x, y))
            {
                this.SortBy = this.SortBy.GetNext();
                this.SortButton.label = this.SortButton.name = this.GetSortLabel(this.SortBy);
                this.ResetItemView(rebuild: true);
            }
            // Ʒ�ʰ�ť
            else if (this.QualityButton.bounds.Contains(x, y))
            {
                this.Quality = this.Quality.GetNext();
                this.ResetItemView();
            }
            // ������ť
            else if (this.UpArrow.bounds.Contains(x, y))
                this.receiveScrollWheelAction(1);
            else if (this.DownArrow.bounds.Contains(x, y))
                this.receiveScrollWheelAction(-1);
            // ����������
            else if (this.CategoryDropdown.TryClick(x, y, out bool itemClicked, out bool dropdownToggled))
            {
                if (dropdownToggled)
                    this.SetDropdown(this.CategoryDropdown.IsExpanded);
                if (itemClicked)
                    this.SetCategory(this.CategoryDropdown.Selected);
            }
            // �����ص�
            else
            {
                // Ĭ�ϴ�����Ϊ
                base.receiveLeftClick(x, y, playSound);
            }

        }

        /// <summary>�������Ҽ�</summary>
        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            // �ر�������
            if (this.CategoryDropdown.IsExpanded)
                this.SetDropdown(false);
            // Ĭ�ϴ�����Ϊ
            else
                base.receiveRightClick(x, y, playSound);
        }

        /// <summary>���̰���</summary>
        public override void receiveKeyPress(Keys key)
        {
            bool inDropdown = this.CategoryDropdown.IsExpanded;
            bool isEscape = key == Keys.Escape;
            // �ж��Ƿ����˳����ܵİ�ť��Esc���˵�����ȡ����
            bool isExitButton =
                isEscape
                || Game1.options.doesInputListContain(Game1.options.menuButton, key)
                || Game1.options.doesInputListContain(Game1.options.cancelButton, key);

            // ���������չ�����Ұ����˳�����ô�͹ر�������
            if (inDropdown && isExitButton)
                this.SetDropdown(false);
            // ����Delete��������Ķ�����Ϊ�գ�������
            else if (key == Keys.Delete && this.heldItem != null)
            {
                Utility.trashItem(this.heldItem);
                this.heldItem = null;
            }
            // ���Ұ����л�����һ�����
            else if (key == Keys.Left || key == Keys.Right)
            {
                int direction = key == Keys.Left ? -1 : 1;
                this.NextCategory(direction);
            }
            // ���°���������ͼ
            else if (key == Keys.Up || key == Keys.Down)
            {
                int direction = key == Keys.Up ? -1 : 1;

                if (inDropdown)
                    this.CategoryDropdown.ReceiveScrollWheelAction(direction);
                else
                    this.ScrollView(direction);
            }
            // Ĭ�ϴ�����Ϊ
            else
            {
                base.receiveKeyPress(key);
            }
        }

        /// <summary>������������µļ�</summary>
        public override void receiveGamePadButton(Buttons button)
        {
            // B��Y��Start��Ϊ�˳���
            bool isExitKey = button == Buttons.B || button == Buttons.Y || button == Buttons.Start;
            bool inDropdown = this.CategoryDropdown.IsExpanded;
            // ���Ҽ��л����������
            if (button == Buttons.LeftTrigger && !inDropdown)
                this.NextCategory(-1);
            else if (button == Buttons.RightTrigger && !inDropdown)
                this.NextCategory(1);
            else
                base.receiveGamePadButton(button);
        }

        /// <summary>�������</summary>
        /// <param name="direction">��������</param>
        public override void receiveScrollWheelAction(int direction)
        {
            base.receiveScrollWheelAction(direction);
            // �������ɸѡ��չ����ô��������
            if (this.CategoryDropdown.IsExpanded)
                this.CategoryDropdown.ReceiveScrollWheelAction(direction);
            // ��������б���ͼ
            else
                this.ScrollView(-direction);
        }

        /// <summary>����������˵�</summary>
        public override void performHoverAction(int x, int y)
        {
            // �����߼�
            base.performHoverAction(x, y);
        }

        /// <summary>���Ʋ˵�</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        public override void draw(SpriteBatch spriteBatch)
        {
            // ���Ʊ�������
            spriteBatch.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);
            // ���ƹ�����ͷ
            if (this.CanScrollUp)
                this.UpArrow.draw(spriteBatch);
            if (this.CanScrollDown)
                this.DownArrow.draw(spriteBatch);
            this.BaseDraw(spriteBatch);
            // ����������
            // ...
            // ����Ʒ�ʣ�����ť
            CommonHelper.DrawTab(this.QualityButton.bounds.X, this.QualityButton.bounds.Y, this.QualityButton.bounds.Width - CommonHelper.ButtonBorderWidth, this.QualityButton.bounds.Height - CommonHelper.ButtonBorderWidth, out Vector2 qualityIconPos, drawShadow: this.IsAndroid);
            CommonHelper.DrawTab(this.SortButton.bounds.X, this.SortButton.bounds.Y, Game1.smallFont, this.SortButton.name, drawShadow: this.IsAndroid);
            this.SortIcon.draw(spriteBatch);
            // ����Ʒ��ͼ��
            {
                this.GetQualityIcon(out Texture2D texture, out Rectangle sourceRect, out Color color);
                spriteBatch.Draw(texture, new Vector2(qualityIconPos.X, qualityIconPos.Y - 1 * Game1.pixelZoom), sourceRect, color, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 1f);
            }
            // ��������������
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
            // ����UI�����»��ƹ��
            this.drawMouse(spriteBatch);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>��ȡ����Ʒ�ʰ�ťͼ��</summary>
        /// <param name="texture">����ͼ�������</param>
        /// <param name="sourceRect">����ͼ�����������</param>
        /// <param name="color">ͼ����ɫ��͸����</param>
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

        /// <summary>��ȡ������ʾ��UI��</summary>
        /// <param name="items">�ɱ����ɵ���Ʒ����</param>
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

        /// <summary>��ʼ���Զ���UI���</summary>
        private void InitializeComponents()
        {
            // ��ȡ��������λ��
            int x = this.xPositionOnScreen;
            int y = this.yPositionOnScreen;
            int right = x + this.width;
            int top = this.IsAndroid
                ? y - (CommonSprites.Tab.Top.Height * Game1.pixelZoom) // at top of screen, moved up slightly to reduce overlap over items
                : y - Game1.tileSize * 2 + 10; // above menu

            // ����UI
            this.QualityButton = new ClickableComponent(new Rectangle(x - 2 * Game1.pixelZoom, top, 9 * Game1.pixelZoom + CommonHelper.ButtonBorderWidth, 9 * Game1.pixelZoom + CommonHelper.ButtonBorderWidth - 2), ""); // manually tweak height to align with sort button
            this.SortButton = new ClickableComponent(new Rectangle(this.QualityButton.bounds.Right + 20, top, this.GetMaxSortLabelWidth(Game1.smallFont) + CommonHelper.ButtonBorderWidth, Game1.tileSize), this.GetSortLabel(this.SortBy));
            this.SortIcon = new ClickableTextureComponent(new Rectangle(this.SortButton.bounds.X + CommonHelper.ButtonBorderWidth, top + CommonHelper.ButtonBorderWidth, this.SortTexture.Width, Game1.tileSize), this.SortTexture, new Rectangle(0, 0, this.SortTexture.Width, this.SortTexture.Height), 1f);
            this.CategoryDropdown = new Dropdown<string>(this.SortButton.bounds.Right + 20, this.SortButton.bounds.Y, Game1.smallFont, this.CategoryDropdown?.Selected ?? I18n.Filter_All(), this.Categories, p => p);
            this.UpArrow = new ClickableTextureComponent(new Rectangle(right - 32, y - 64, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom), Game1.mouseCursors, new Rectangle(421, 459, 11, 12), Game1.pixelZoom);
            this.DownArrow = new ClickableTextureComponent(new Rectangle(this.UpArrow.bounds.X, this.UpArrow.bounds.Y + this.height / 2 - 64, this.UpArrow.bounds.Width, this.UpArrow.bounds.Height), Game1.mouseCursors, new Rectangle(421, 472, 11, 12), Game1.pixelZoom);

            // ������
            // ...

            // �ƶ�Android����
            if (this.IsAndroid)
            {
                this.UpArrow.bounds.X = this.upperRightCloseButton.bounds.Center.X - this.SortButton.bounds.Width / 2;
                this.UpArrow.bounds.Y = this.upperRightCloseButton.bounds.Bottom;
                this.DownArrow.bounds.X = this.UpArrow.bounds.X;
            }

            // ����������˳��
            this.InitializeControllerFlow();
        }

        /// <summary>�����ֶ�֧�ֿ���������</summary>
        private void InitializeControllerFlow()
        {
            //   - �ų���������Ϊ��������
            //   - ������ ID �Զ�����ȡ�����Ƿ�չ��

            // Android�޿����������ܻ����
            if (this.IsAndroid)
                return;

            List<ClickableComponent> slots = this.ItemsToGrabMenu.inventory;

            // �޸�����б�
            this.ChildComponents.Clear();
            this.ChildComponents.AddRange(new[] { this.QualityButton, this.SortButton, this.UpArrow, this.DownArrow, this.CategoryDropdown });

            // ���� IDs
            {
                int curId = 1_000_000;
                foreach (ClickableComponent component in this.ChildComponents)
                    component.myID = curId++;
            }

            Console.WriteLine(slots);

            // �Զ���������Ҽ�����ID
            this.QualityButton.rightNeighborID = this.SortButton.myID;
            this.SortButton.rightNeighborID = this.CategoryDropdown.myID;
            this.CategoryDropdown.rightNeighborID = this.UpArrow.myID;
            this.UpArrow.downNeighborID = this.DownArrow.myID;

            // ��
            this.UpArrow.upNeighborID = this.CategoryDropdown.myID;
            this.UpArrow.leftNeighborID = slots[10].myID;
            this.CategoryDropdown.leftNeighborID = this.SortButton.myID;
            this.SortButton.leftNeighborID = this.QualityButton.myID;

            // ��
            this.QualityButton.downNeighborID = slots[0].myID;
            this.SortButton.downNeighborID = slots[1].myID;
            this.CategoryDropdown.DefaultDownNeighborId = slots[5].myID;
            this.DownArrow.leftNeighborID = slots.Last().myID;
            this.DownArrow.downNeighborID = this.trashCan.myID;

            // ��
            slots[0].upNeighborID = this.QualityButton.myID;
            foreach (int i in new[] { 1, 2 })
                slots[i].upNeighborID = this.SortButton.myID;
            foreach (int i in new[] { 3, 4, 5, 6, 8, 9, 10, 11 })
                slots[i].upNeighborID = this.QualityButton.myID;
            foreach (int i in new[] { 11, 23 })
                slots[i].rightNeighborID = this.UpArrow.myID;
            slots.Last().rightNeighborID = this.DownArrow.myID;
            this.trashCan.upNeighborID = this.DownArrow.myID;

            // ������Ŀ
            this.CategoryDropdown.ReinitializeControllerFlow();
            this.ChildComponents.AddRange(this.CategoryDropdown.GetChildComponents());

            // �޸�����б�
            this.populateClickableComponentList();
        }

        /// <summary>����Ӳ˵���ѡ��һ����Ʒ</summary>
        /// <param name="item">�Ӳ˵���ץȡ����Ʒ</param>
        /// <param name="player">˭ץ��</param>
        private void OnItemGrab(Item item, Farmer player)
        {
            this.ResetItemView();
        }

        /// <summary>չ�����۵�����������</summary>
        /// <param name="expanded">�Ƿ�չ��</param>
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

        /// <summary>�л����¸����</summary>
        /// <param name="direction">�л��ķ���</param>
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

        /// <summary>���õ�ǰ�����</summary>
        /// <param name="category">�µ����ֵ</param>
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

        /// <summary>������Ʒ��ͼ</summary>
        /// <param name="direction">��������</param>
        /// <param name="resetItemView">�Ƿ������Ʒ��ͼ</param>
        public void ScrollView(int direction, bool resetItemView = true)
        {
            // Ӧ�ù���
            if (direction < 0)
                this.TopRowIndex--;
            else if (direction > 0)
                this.TopRowIndex++;
            // ��񻯹���
            this.TopRowIndex = (int)MathHelper.Clamp(this.TopRowIndex, 0, this.MaxTopRowIndex);
            // �޸���ͼ
            if (resetItemView)
                this.ResetItemView();
        }

        /// <summary>������Ʒ��ͼ��ʾ</summary>
        /// <param name="rebuild">�Ƿ������������</param>
        private void ResetItemView(bool rebuild = false)
        {
            if (rebuild)
            {
                this.FilteredItems.Clear();
                this.FilteredItems.AddRange(this.SearchItems());
                this.TopRowIndex = 0;
            }
            // �����Ҫ���޸�����
            int totalRows = (int)Math.Ceiling(this.FilteredItems.Count / (this.ItemsPerRow * 1m));
            this.MaxTopRowIndex = Math.Max(0, totalRows - 3);
            this.ScrollView(0, resetItemView: false);
            // �޸���ͼ����Ʒ
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

        /// <summary>��ȡ����������ƥ���������Ŀ�����Է�ҳ</summary>
        private IEnumerable<SpawnableItem> SearchItems()
        {
            // ������ѯ
            IEnumerable<SpawnableItem> items = this.AllItems;
            items = this.SortBy switch
            {
                ItemSort.Type => items.OrderBy(p => p.Item.Category),
                ItemSort.ID => items.OrderBy(p => p.Item.ParentSheetIndex),
                _ => items.OrderBy(p => p.Item.DisplayName)
            };

            // ������
            if (!this.EqualsCaseInsensitive(this.CategoryDropdown.Selected, I18n.Filter_All()))
                items = items.Where(item => this.EqualsCaseInsensitive(item.Category, this.CategoryDropdown.Selected));

            // ����
            // ...

            return items;
        }

        /// <summary>��ȡָ����ȵ��ַ�������</summary>
        /// <param name="font">��������������С������</param>
        /// <param name="width">������Ϊ��λ��С�������</param>
        private string GetSpaceIndent(SpriteFont font, int width)
        {
            if (width <= 0)
                return "";

            string indent = " ";
            while (font.MeasureString(indent).X < width)
                indent += " ";

            return indent;
        }

        /// <summary>��ȡ�����������ǩ</summary>
        /// <param name="sort">��������</param>
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

        /// <summary>��ȡ�����ǩ�����</summary>
        /// <param name="font">��������</param>
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

        /// <summary>�ж������ַ����Ƿ���ȣ����Դ�Сд����</summary>
        private bool EqualsCaseInsensitive(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
