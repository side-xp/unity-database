using System;
using System.Linq;

using NUnit.Framework;

using UnityEngine;
using UnityEditor;

using SideXP.Database.EditorOnly;

using Object = UnityEngine.Object;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the <see cref="ProjectAssetSource"/>, against real assets.
    /// </summary>
    /// <remarks>
    /// A temporary folder is created under <c>Assets/</c> in <see cref="SetUp"/> and removed in <see cref="TearDown"/>. The results are
    /// limited to that folder, so other assets of the project don't affect the tests.
    /// </remarks>
    public class ProjectAssetSourceTests
    {

        #region Fields

        /// <summary>
        /// The name of the temporary folder.
        /// </summary>
        private const string TempFolderName = "ProjectAssetSourceTests_Temp";

        /// <summary>
        /// The path of the temporary folder.
        /// </summary>
        private const string TempFolder = "Assets/" + TempFolderName;

        /// <summary>
        /// The name of the subfolder of the temporary folder, created by the tests of the folder scope.
        /// </summary>
        private const string SubFolderName = "Sub";

        /// <summary>
        /// The path of the subfolder of the temporary folder.
        /// </summary>
        private const string SubFolder = TempFolder + "/" + SubFolderName;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Creates the temporary folder and the assets of the tests:
        /// <list type="bullet">
        /// <item><c>Boots</c> (<see cref="BootsSO"/>)</item>
        /// <item><c>Columns</c> (<see cref="ColumnsSO"/>, not an item)</item>
        /// <item><c>Consumable</c> (<see cref="ConsumableSO"/>)</item>
        /// <item><c>Item</c> (<see cref="ItemSO"/>), with the sub-assets <c>Sub Weapon</c> (<see cref="WeaponSO"/>) and
        /// <c>Hidden Helmet</c> (<see cref="HelmetSO"/>, hidden from the Project window)</item>
        /// <item><c>Weapon</c> (<see cref="WeaponSO"/>)</item>
        /// </list>
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.DeleteAsset(TempFolder);

            AssetDatabase.CreateFolder("Assets", TempFolderName);

            CreateAsset<BootsSO>("Boots");
            CreateAsset<ColumnsSO>("Columns");
            CreateAsset<ConsumableSO>("Consumable");
            CreateAsset<WeaponSO>("Weapon");
            ItemSO item = CreateAsset<ItemSO>("Item");

            WeaponSO subWeapon = ScriptableObject.CreateInstance<WeaponSO>();
            subWeapon.name = "Sub Weapon";
            AssetDatabase.AddObjectToAsset(subWeapon, item);

            HelmetSO hiddenHelmet = ScriptableObject.CreateInstance<HelmetSO>();
            hiddenHelmet.name = "Hidden Helmet";
            hiddenHelmet.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(hiddenHelmet, item);

            // Import the file again, so the search index knows the types of its sub-assets
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(item), ImportAssetOptions.ForceUpdate);
        }

        /// <summary>
        /// Deletes the temporary folder and its assets.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.DeleteAsset(TempFolder);
        }

        #endregion


        #region Tests

        /// <summary>
        /// The table of a base type includes the assets of its derived types and their sub-assets, ordered by path, with the sub-assets
        /// after their main asset.
        /// </summary>
        [Test]
        public void LoadAssets_BaseType_IncludesDerivedTypesAndSubAssets()
        {
            CollectionAssert.AreEqual(new[] { "Boots", "Consumable", "Item", "Sub Weapon", "Weapon" }, LoadNames(typeof(ItemSO)));
        }

        /// <summary>
        /// The table of a derived type skips the assets of its base type and sibling types, including the main asset of a file that
        /// contains one of its sub-assets.
        /// </summary>
        [Test]
        public void LoadAssets_DerivedType_SkipsOtherTypes()
        {
            CollectionAssert.AreEqual(new[] { "Boots", "Sub Weapon", "Weapon" }, LoadNames(typeof(EquipmentSO)));
        }

        /// <summary>
        /// The table of an abstract type finds the assets of its concrete derived types, and skips the hidden sub-assets.
        /// </summary>
        [Test]
        public void LoadAssets_AbstractType_FindsConcreteTypes()
        {
            CollectionAssert.AreEqual(new[] { "Boots" }, LoadNames(typeof(ArmorSO)));
        }

        /// <summary>
        /// Sub-assets hidden from the Project window are skipped.
        /// </summary>
        [Test]
        public void LoadAssets_HiddenSubAsset_Skipped()
        {
            CollectionAssert.IsEmpty(LoadNames(typeof(HelmetSO)));
        }

        /// <summary>
        /// With a folder scope, only the assets of its folders and of their subfolders are loaded.
        /// </summary>
        [Test]
        public void LoadAssets_FolderScope_OnlyAssetsOfFolders()
        {
            CreateSubfolderAsset();

            CollectionAssert.AreEqual(new[] { "Inner Weapon" }, LoadNamesInScope(typeof(ItemSO), SubFolder));
            CollectionAssert.AreEqual(new[] { "Boots", "Consumable", "Item", "Sub Weapon", "Inner Weapon", "Weapon" },
                LoadNamesInScope(typeof(ItemSO), TempFolder));
        }

        /// <summary>
        /// A scope whose folders don't exist loads no asset.
        /// </summary>
        [Test]
        public void LoadAssets_MissingFolder_NoAsset()
        {
            CollectionAssert.IsEmpty(LoadNamesInScope(typeof(ItemSO), $"{TempFolder}/Missing"));
        }

        /// <summary>
        /// A change of the scope applies to the next load, and a null scope is the whole project.
        /// </summary>
        [Test]
        public void LoadAssets_ScopeChanged_AppliesToNextLoad()
        {
            CreateSubfolderAsset();
            ProjectAssetSource source = new ProjectAssetSource(typeof(WeaponSO))
            {
                Scope = new FolderScope(new[] { SubFolder })
            };
            CollectionAssert.AreEqual(new[] { "Inner Weapon" }, source.LoadAssets().Select(i => i.name).ToArray());

            source.Scope = null;
            Assert.IsTrue(source.Scope.IsWholeProject);
            CollectionAssert.AreEqual(new[] { "Sub Weapon", "Inner Weapon", "Weapon" }, source.LoadAssets()
                .Where(i => AssetDatabase.GetAssetPath(i).StartsWith($"{TempFolder}/"))
                .Select(i => i.name)
                .ToArray());
        }

        /// <summary>
        /// A source can only be created for a <see cref="ScriptableObject"/> type.
        /// </summary>
        [Test]
        public void Constructor_InvalidType_Throws()
        {
            Assert.Throws<ArgumentException>(() => new ProjectAssetSource(typeof(NotScriptableObjectTable)));
            Assert.Throws<ArgumentException>(() => new ProjectAssetSource(null));
        }

        #endregion


        #region Private API

        /// <summary>
        /// Creates an asset in the temporary folder.
        /// </summary>
        /// <typeparam name="T">The type of the asset.</typeparam>
        /// <param name="name">The name of the asset file, without extension.</param>
        /// <returns>Returns the created asset.</returns>
        private static T CreateAsset<T>(string name)
            where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, $"{TempFolder}/{name}.asset");
            return asset;
        }

        /// <summary>
        /// Loads the assets of a type with a <see cref="ProjectAssetSource"/>, limited to the temporary folder.
        /// </summary>
        /// <param name="type">The type of the assets to load.</param>
        /// <returns>Returns the names of the loaded assets, in order.</returns>
        private static string[] LoadNames(Type type)
        {
            return new ProjectAssetSource(type).LoadAssets()
                .Where(i => AssetDatabase.GetAssetPath(i).StartsWith($"{TempFolder}/"))
                .Select(i => i.name)
                .ToArray();
        }

        /// <summary>
        /// Loads the assets of a type with a <see cref="ProjectAssetSource"/> limited to a folder scope.
        /// </summary>
        /// <param name="type">The type of the assets to load.</param>
        /// <param name="folders">The folders of the scope.</param>
        /// <returns>Returns the names of the loaded assets, in order.</returns>
        private static string[] LoadNamesInScope(Type type, params string[] folders)
        {
            ProjectAssetSource source = new ProjectAssetSource(type) { Scope = new FolderScope(folders) };
            return source.LoadAssets().Select(i => i.name).ToArray();
        }

        /// <summary>
        /// Creates the <see cref="SubFolder"/> folder, with the asset <c>Inner Weapon</c> (<see cref="WeaponSO"/>).
        /// </summary>
        private static void CreateSubfolderAsset()
        {
            AssetDatabase.CreateFolder(TempFolder, SubFolderName);
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<WeaponSO>(), $"{SubFolder}/Inner Weapon.asset");
        }

        #endregion

    }

}
