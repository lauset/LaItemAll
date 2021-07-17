using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI;

namespace LaItemAll.main
{
    /// <summary>��ȡ��ͬ���Զ�Ӧ������</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Deliberately named for consistency and to match translation conventions.")]
    internal static class I18n
    {
        /*********
        ** Fields
        *********/
        /// <summary>MOD��������</summary>
        private static ITranslationHelper Translations;

        /*********
        ** Public methods
        *********/
        /// <summary>��ʼ��</summary>
        /// <param name="translations">MOD��������</param>
        public static void Init(ITranslationHelper translations)
        {
            I18n.Translations = translations;
        }

        public static string Sort_ByName()
        {
            return I18n.GetByKey("sort.by-name");
        }

        public static string Sort_ByType()
        {
            return I18n.GetByKey("sort.by-type");
        }

        public static string Sort_ById()
        {
            return I18n.GetByKey("sort.by-id");
        }

        public static string Filter_All()
        {
            return I18n.GetByKey("filter.all");
        }

        public static string Filter_ArtisanAndCooking()
        {
            return I18n.GetByKey("filter.artisan-and-cooking");
        }

        public static string Filter_Crafting_Products()
        {
            return I18n.GetByKey("filter.crafting.products");
        }

        public static string Filter_Crafting_Resources()
        {
            return I18n.GetByKey("filter.crafting.resources");
        }

        public static string Filter_Decor_Furniture()
        {
            return I18n.GetByKey("filter.decor.furniture");
        }

        public static string Filter_Decor_Other()
        {
            return I18n.GetByKey("filter.decor.other");
        }

        public static string Filter_EquipmentBoots()
        {
            return I18n.GetByKey("filter.equipment-boots");
        }

        public static string Filter_EquipmentClothes()
        {
            return I18n.GetByKey("filter.equipment-clothes");
        }

        public static string Filter_EquipmentHats()
        {
            return I18n.GetByKey("filter.equipment-hats");
        }

        public static string Filter_EquipmentRings()
        {
            return I18n.GetByKey("filter.equipment-rings");
        }

        public static string Filter_EquipmentTools()
        {
            return I18n.GetByKey("filter.equipment-tools");
        }

        public static string Filter_EquipmentWeapons()
        {
            return I18n.GetByKey("filter.equipment-weapons");
        }

        public static string Filter_FarmAnimalDrops()
        {
            return I18n.GetByKey("filter.farm-animal-drops");
        }

        public static string Filter_FarmCrops()
        {
            return I18n.GetByKey("filter.farm-crops");
        }

        public static string Filter_FarmSeeds()
        {
            return I18n.GetByKey("filter.farm-seeds");
        }

        public static string Filter_Fish()
        {
            return I18n.GetByKey("filter.fish");
        }

        public static string Filter_MineralsAndArtifacts()
        {
            return I18n.GetByKey("filter.minerals-and-artifacts");
        }

        public static string Filter_Miscellaneous()
        {
            return I18n.GetByKey("filter.miscellaneous");
        }

        /// <summary>ͨ��Key��ȡ�������ı�</summary>
        /// <param name="key">�ַ���KEY</param>
        /// <param name="tokens">�������Ƽ�/ֵ�ԵĶ��������������������ֵ����ʵ��</param>
        public static Translation GetByKey(string key, object tokens = null)
        {
            if (I18n.Translations == null)
                throw new InvalidOperationException($"You must call {nameof(I18n)}.{nameof(I18n.Init)} from the mod's entry method before reading translations.");
            return I18n.Translations.Get(key, tokens);
        }
    }
}

