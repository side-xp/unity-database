using NUnit.Framework;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the folder scope of the tables (<see cref="FolderScope"/>): the folders, their format, and the paths they contain.
    /// </summary>
    public class FolderScopeTests
    {

        #region Whole project

        /// <summary>
        /// Without folders, the scope is the whole project, and contains every asset.
        /// </summary>
        [Test]
        public void Constructor_NoFolder_WholeProject()
        {
            foreach (FolderScope scope in new[] { new FolderScope(null), new FolderScope(new string[0]), FolderScope.WholeProject })
            {
                Assert.IsTrue(scope.IsWholeProject);
                Assert.IsTrue(scope.Contains("Assets/Items/Sword.asset"));
                Assert.IsTrue(scope.Contains("Packages/com.side-xp.database/Tests/Sword.asset"));
                Assert.IsFalse(scope.Contains(string.Empty));
                Assert.IsFalse(scope.Contains(null));
            }
        }

        /// <summary>
        /// A scope whose folders are all blank is the whole project.
        /// </summary>
        [Test]
        public void Constructor_BlankFolders_WholeProject()
        {
            Assert.IsTrue(new FolderScope(new[] { null, string.Empty, "  " }).IsWholeProject);
        }

        #endregion


        #region Folders

        /// <summary>
        /// The scope contains the assets of its folders and of their subfolders.
        /// </summary>
        [Test]
        public void Contains_FolderAndSubfolders_True()
        {
            FolderScope scope = new FolderScope(new[] { "Assets/Items" });

            Assert.IsFalse(scope.IsWholeProject);
            Assert.IsTrue(scope.Contains("Assets/Items/Sword.asset"));
            Assert.IsTrue(scope.Contains("Assets/Items/Weapons/Axes/Axe.asset"));
            Assert.IsFalse(scope.Contains("Assets/Sword.asset"));
            Assert.IsFalse(scope.Contains("Assets/Enemies/Goblin.asset"));
        }

        /// <summary>
        /// A folder whose name starts like the folder of the scope is another folder.
        /// </summary>
        [Test]
        public void Contains_SameNamePrefix_False()
        {
            FolderScope scope = new FolderScope(new[] { "Assets/Items" });

            Assert.IsFalse(scope.Contains("Assets/ItemsOld/Sword.asset"));
            Assert.IsFalse(scope.Contains("Assets/Items.asset"));
        }

        /// <summary>
        /// The scope contains the assets of any of its folders.
        /// </summary>
        [Test]
        public void Contains_SeveralFolders_AnyFolder()
        {
            FolderScope scope = new FolderScope(new[] { "Assets/Items/Weapons", "Assets/Items/Shields" });

            Assert.IsTrue(scope.Contains("Assets/Items/Weapons/Sword.asset"));
            Assert.IsTrue(scope.Contains("Assets/Items/Shields/Buckler.asset"));
            Assert.IsFalse(scope.Contains("Assets/Items/Armors/Helmet.asset"));
        }

        /// <summary>
        /// The paths are compared ignoring case, like the asset database does.
        /// </summary>
        [Test]
        public void Contains_OtherCase_True()
        {
            FolderScope scope = new FolderScope(new[] { "Assets/Items" });

            Assert.IsTrue(scope.Contains("assets/ITEMS/Sword.asset"));
        }

        #endregion


        #region Format

        /// <summary>
        /// The folders are normalized: forward slashes, no spaces around them, no slash at the end.
        /// </summary>
        [Test]
        public void Constructor_FolderFormats_Normalized()
        {
            FolderScope scope = new FolderScope(new[] { @"Assets\Items\Weapons\", " Assets/Items/Shields/ " });

            CollectionAssert.AreEqual(new[] { "Assets/Items/Weapons", "Assets/Items/Shields" }, scope.Folders);
            Assert.IsTrue(scope.Contains(@"Assets\Items\Weapons\Sword.asset"));
        }

        /// <summary>
        /// The duplicates of a folder (ignoring case and format) and the blank folders are skipped.
        /// </summary>
        [Test]
        public void Constructor_DuplicatesAndBlanks_Skipped()
        {
            FolderScope scope = new FolderScope(new[] { "Assets/Items", "assets/items/", null, @"Assets\Items", string.Empty });

            CollectionAssert.AreEqual(new[] { "Assets/Items" }, scope.Folders);
        }

        /// <summary>
        /// Normalizing a folder keeps its case, and gives an empty text for a blank path.
        /// </summary>
        [Test]
        public void NormalizeFolder_Formats_AssetDatabaseFormat()
        {
            Assert.AreEqual("Assets/Items", FolderScope.NormalizeFolder(@"Assets\Items\"));
            Assert.AreEqual("Assets/Items", FolderScope.NormalizeFolder("  Assets/Items//"));
            Assert.AreEqual(string.Empty, FolderScope.NormalizeFolder("   "));
            Assert.AreEqual(string.Empty, FolderScope.NormalizeFolder(null));
        }

        #endregion

    }

}
