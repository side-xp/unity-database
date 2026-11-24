using System;
using System.IO;

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


        #region Duplicate

        /// <summary>
        /// Duplicating a main asset copies its file next to the original, with a unique name.
        /// </summary>
        [Test]
        public void TryDuplicate_MainAsset_CopiesFileNextToOriginal()
        {
            Assert.IsTrue(AssetOperations.TryDuplicate(_sword, out UnityEngine.Object copy, out string error));
            Assert.IsNull(error);
            Assert.IsInstanceOf<WeaponSO>(copy);
            Assert.AreNotSame(_sword, copy);

            string copyPath = AssetDatabase.GetAssetPath(copy);
            Assert.AreNotEqual(AssetDatabase.GetAssetPath(_sword), copyPath);
            Assert.AreEqual(TempFolder, Path.GetDirectoryName(copyPath).Replace('\\', '/'));
            Assert.IsTrue(AssetDatabase.IsMainAsset(copy));
            Assert.AreEqual($"{TempFolder}/Sword.asset", AssetDatabase.GetAssetPath(_sword));
        }

        /// <summary>
        /// Duplicating a main asset also copies its sub-assets, like a duplicate in the Project window.
        /// </summary>
        [Test]
        public void TryDuplicate_MainAssetWithSubAssets_CopiesSubAssets()
        {
            Assert.IsTrue(AssetOperations.TryDuplicate(_bundle, out UnityEngine.Object copy, out _));

            UnityEngine.Object[] copiedSubAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(copy));
            Assert.AreEqual(1, copiedSubAssets.Length);
            Assert.AreEqual("Blade", copiedSubAssets[0].name);
        }

        /// <summary>
        /// Duplicating a sub-asset copies it into the same file, with a name that no other object of the file has.
        /// </summary>
        [Test]
        public void TryDuplicate_SubAsset_CopiesIntoSameFile()
        {
            Assert.IsTrue(AssetOperations.TryDuplicate(_blade, out UnityEngine.Object copy, out string error));
            Assert.IsNull(error);
            Assert.IsInstanceOf<WeaponSO>(copy);
            Assert.AreNotSame(_blade, copy);
            Assert.IsTrue(AssetDatabase.IsSubAsset(copy));
            Assert.AreEqual(AssetDatabase.GetAssetPath(_bundle), AssetDatabase.GetAssetPath(copy));
            Assert.AreNotEqual(_blade.name, copy.name);
            Assert.AreEqual(2, AssetOperations.CountSubAssets(_bundle));
        }

        /// <summary>
        /// Duplicating a sub-asset is a single Undo step, which destroys the copy.
        /// </summary>
        [Test]
        public void TryDuplicate_SubAsset_CanBeUndone()
        {
            AssetOperations.TryDuplicate(_blade, out UnityEngine.Object copy, out _);
            Undo.PerformUndo();
            Assert.IsTrue(copy == null);
            Assert.IsTrue(_blade != null);
        }

        /// <summary>
        /// An object that is not saved in an asset file can't be duplicated.
        /// </summary>
        [Test]
        public void TryDuplicate_NotAnAsset_Refused()
        {
            WeaponSO instance = ScriptableObject.CreateInstance<WeaponSO>();
            try
            {
                Assert.IsFalse(AssetOperations.TryDuplicate(instance, out UnityEngine.Object copy, out string error));
                Assert.IsNull(copy);
                Assert.IsNotNull(error);
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
        public void TryDuplicate_NullAsset_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => AssetOperations.TryDuplicate(null, out _, out _));
        }

        #endregion


        #region Delete

        /// <summary>
        /// Deleting a sub-asset removes it from its file, and keeps the main asset.
        /// </summary>
        /// <remarks>
        /// Deleting a main asset is not tested: it only calls <see cref="AssetDatabase.MoveAssetToTrash(string)"/>, and would fill the
        /// trash of the system with test files on every run.
        /// </remarks>
        [Test]
        public void TryDelete_SubAsset_RemovesFromFile()
        {
            Assert.IsTrue(AssetOperations.TryDelete(_blade, out string error));
            Assert.IsNull(error);
            Assert.IsTrue(_blade == null);
            Assert.IsTrue(_bundle != null);
            Assert.AreEqual(0, AssetOperations.CountSubAssets(_bundle));
        }

        /// <summary>
        /// Deleting a sub-asset is a single Undo step, which puts it back in its file.
        /// </summary>
        [Test]
        public void TryDelete_SubAsset_CanBeUndone()
        {
            AssetOperations.TryDelete(_blade, out _);
            Undo.PerformUndo();

            UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(_bundle));
            Assert.AreEqual(1, subAssets.Length);
            Assert.AreEqual("Blade", subAssets[0].name);
        }

        /// <summary>
        /// An object that is not saved in an asset file can't be deleted.
        /// </summary>
        [Test]
        public void TryDelete_NotAnAsset_Refused()
        {
            WeaponSO instance = ScriptableObject.CreateInstance<WeaponSO>();
            try
            {
                Assert.IsFalse(AssetOperations.TryDelete(instance, out string error));
                Assert.IsNotNull(error);
                Assert.IsTrue(instance != null);
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
        public void TryDelete_NullAsset_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => AssetOperations.TryDelete(null, out _));
        }

        /// <summary>
        /// The visible sub-assets of the file of an asset are counted, from the main asset or a sub-asset.
        /// </summary>
        [Test]
        public void CountSubAssets_CountsVisibleSubAssetsOfFile()
        {
            Assert.AreEqual(1, AssetOperations.CountSubAssets(_bundle));
            Assert.AreEqual(1, AssetOperations.CountSubAssets(_blade));
            Assert.AreEqual(0, AssetOperations.CountSubAssets(_sword));
            Assert.AreEqual(0, AssetOperations.CountSubAssets(null));
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
