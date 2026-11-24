using System;

using NUnit.Framework;

using UnityEngine;
using UnityEditor;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the <see cref="AssetOperations"/>, against real assets.
    /// </summary>
    /// <remarks>
    /// A temporary folder is created under <c>Assets/</c> in <see cref="SetUp"/> and removed in <see cref="TearDown"/>.
    /// </remarks>
    public class AssetOperationsTests
    {

        #region Fields

        /// <summary>
        /// The name of the temporary folder.
        /// </summary>
        private const string TempFolderName = "AssetOperationsTests_Temp";

        /// <summary>
        /// The path of the temporary folder.
        /// </summary>
        private const string TempFolder = "Assets/" + TempFolderName;

        /// <summary>
        /// A main asset, named <c>Sword</c>.
        /// </summary>
        private WeaponSO _sword = null;

        /// <summary>
        /// A main asset, named <c>Axe</c>.
        /// </summary>
        private WeaponSO _axe = null;

        /// <summary>
        /// A main asset, named <c>Bundle</c>, which contains <see cref="_blade"/>.
        /// </summary>
        private ItemSO _bundle = null;

        /// <summary>
        /// A sub-asset of <see cref="_bundle"/>, named <c>Blade</c>.
        /// </summary>
        private WeaponSO _blade = null;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Creates the temporary folder and the assets of the tests.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.DeleteAsset(TempFolder);

            AssetDatabase.CreateFolder("Assets", TempFolderName);

            _sword = CreateAsset<WeaponSO>("Sword");
            _axe = CreateAsset<WeaponSO>("Axe");
            _bundle = CreateAsset<ItemSO>("Bundle");

            _blade = ScriptableObject.CreateInstance<WeaponSO>();
            _blade.name = "Blade";
            AssetDatabase.AddObjectToAsset(_blade, _bundle);
            AssetDatabase.SaveAssets();
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


        #region Rename

        /// <summary>
        /// Renaming a main asset renames its file.
        /// </summary>
        [Test]
        public void TryRename_MainAsset_RenamesFile()
        {
            Assert.IsTrue(AssetOperations.TryRename(_sword, "Long Sword", out string error));
            Assert.IsNull(error);
            Assert.AreEqual("Long Sword", _sword.name);
            Assert.AreEqual($"{TempFolder}/Long Sword.asset", AssetDatabase.GetAssetPath(_sword));
        }

        /// <summary>
        /// Renaming a sub-asset renames the object only: its file and its main asset keep their name.
        /// </summary>
        [Test]
        public void TryRename_SubAsset_RenamesObjectOnly()
        {
            Assert.IsTrue(AssetOperations.TryRename(_blade, "Long Blade", out string error));
            Assert.IsNull(error);
            Assert.AreEqual("Long Blade", _blade.name);
            Assert.AreEqual("Bundle", _bundle.name);
            Assert.AreEqual($"{TempFolder}/Bundle.asset", AssetDatabase.GetAssetPath(_blade));
        }

        /// <summary>
        /// Renaming a sub-asset is a single Undo step.
        /// </summary>
        [Test]
        public void TryRename_SubAsset_CanBeUndone()
        {
            AssetOperations.TryRename(_blade, "Long Blade", out _);
            Undo.PerformUndo();
            Assert.AreEqual("Blade", _blade.name);
        }

        /// <summary>
        /// The new name is trimmed.
        /// </summary>
        [Test]
        public void TryRename_NameWithSpaces_Trimmed()
        {
            Assert.IsTrue(AssetOperations.TryRename(_sword, "  Long Sword ", out _));
            Assert.AreEqual("Long Sword", _sword.name);

            Assert.IsTrue(AssetOperations.TryRename(_blade, " Long Blade  ", out _));
            Assert.AreEqual("Long Blade", _blade.name);
        }

        /// <summary>
        /// The current name, even with spaces around it, renames nothing and is not an error.
        /// </summary>
        [Test]
        public void TryRename_SameName_NothingWithoutError()
        {
            Assert.IsFalse(AssetOperations.TryRename(_sword, "Sword ", out string error));
            Assert.IsNull(error);
            Assert.AreEqual($"{TempFolder}/Sword.asset", AssetDatabase.GetAssetPath(_sword));
        }

        /// <summary>
        /// A blank name is refused, for main assets and sub-assets.
        /// </summary>
        [Test]
        public void TryRename_BlankName_Refused()
        {
            Assert.IsFalse(AssetOperations.TryRename(_sword, "  ", out string error));
            Assert.IsNotNull(error);
            Assert.AreEqual("Sword", _sword.name);

            Assert.IsFalse(AssetOperations.TryRename(_blade, null, out error));
            Assert.IsNotNull(error);
            Assert.AreEqual("Blade", _blade.name);
        }

        /// <summary>
        /// The name of another file of the same folder is refused, with the error of Unity.
        /// </summary>
        [Test]
        public void TryRename_NameOfAnotherFile_Refused()
        {
            Assert.IsFalse(AssetOperations.TryRename(_sword, _axe.name, out string error));
            Assert.IsFalse(string.IsNullOrEmpty(error));
            Assert.AreEqual("Sword", _sword.name);
            Assert.AreEqual($"{TempFolder}/Sword.asset", AssetDatabase.GetAssetPath(_sword));
        }

        /// <summary>
        /// An object that is not saved in an asset file can't be renamed.
        /// </summary>
        [Test]
        public void TryRename_NotAnAsset_Refused()
        {
            WeaponSO instance = ScriptableObject.CreateInstance<WeaponSO>();
            instance.name = "Instance";
            try
            {
                Assert.IsFalse(AssetOperations.TryRename(instance, "Renamed", out string error));
                Assert.IsNotNull(error);
                Assert.AreEqual("Instance", instance.name);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        /// <summary>
        /// A null asset is a programming error.
        /// </summary>
        [Test]
        public void TryRename_NullAsset_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => AssetOperations.TryRename(null, "Name", out _));
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

        #endregion

    }

}
