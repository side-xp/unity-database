using NUnit.Framework;

using UnityEngine;
using UnityEditor;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Tests for the changes that concern a table with a folder scope (<see cref="TableChangeTracker.Concerns(AssetChanges)"/>),
    /// against real files.
    /// </summary>
    /// <remarks>
    /// A temporary folder is created under <c>Assets/</c> in <see cref="SetUp"/> and removed in <see cref="TearDown"/>. The tracked
    /// table is limited to its subfolder, so the other assets of the project don't affect the tests. The changes are given to the
    /// tracker directly, instead of waiting for the asset postprocessor.
    /// </remarks>
    public class TableChangeTrackerTests
    {

        #region Fields

        private const string TempFolderName = "TableChangeTrackerTests_Temp";
        private const string TempFolder = "Assets/" + TempFolderName;
        private const string SubFolderName = "Scope";
        private const string SubFolder = TempFolder + "/" + SubFolderName;

        /// <summary>
        /// The path of the weapon outside of the scope.
        /// </summary>
        private const string OutsidePath = TempFolder + "/Outside.asset";

        /// <summary>
        /// The path of the weapon in the scope, a row of the table.
        /// </summary>
        private const string InsidePath = SubFolder + "/Inside.asset";

        /// <summary>
        /// The tracker of the items table, limited to <see cref="SubFolder"/>, with its index built.
        /// </summary>
        private TableChangeTracker _tracker = null;

        #endregion


        #region Lifecycle

        /// <summary>
        /// Creates the temporary folders, a weapon in each, and the tracker of the items table limited to the subfolder.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.DeleteAsset(TempFolder);

            AssetDatabase.CreateFolder("Assets", TempFolderName);
            AssetDatabase.CreateFolder(TempFolder, SubFolderName);
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<WeaponSO>(), OutsidePath);
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<WeaponSO>(), InsidePath);

            FolderScope scope = new FolderScope(new[] { SubFolder });
            ProjectAssetSource source = new ProjectAssetSource(typeof(ItemSO)) { Scope = scope };
            TablePipeline pipeline = new TablePipeline(source, new IValueAccessor[0]);
            pipeline.Update();

            _tracker = new TableChangeTracker(typeof(ItemSO), pipeline) { Scope = scope };
            _tracker.UpdateIndex();
        }

        /// <summary>
        /// Deletes the temporary folders and their assets.
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
        /// A file of the table imported outside of the scope doesn't concern the table.
        /// </summary>
        [Test]
        public void Concerns_ImportedOutsideScope_False()
        {
            Assert.IsFalse(_tracker.Concerns(Imported(OutsidePath)));
        }

        /// <summary>
        /// A file of the table imported in the scope concerns the table, whether it already has rows or is new.
        /// </summary>
        [Test]
        public void Concerns_ImportedInScope_True()
        {
            string newPath = $"{SubFolder}/New.asset";
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<WeaponSO>(), newPath);

            Assert.IsTrue(_tracker.Concerns(Imported(InsidePath)));
            Assert.IsTrue(_tracker.Concerns(Imported(newPath)));
        }

        /// <summary>
        /// A file of another type imported in the scope doesn't concern the table.
        /// </summary>
        [Test]
        public void Concerns_OtherTypeInScope_False()
        {
            string otherPath = $"{SubFolder}/Columns.asset";
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ColumnsSO>(), otherPath);

            Assert.IsFalse(_tracker.Concerns(Imported(otherPath)));
        }

        /// <summary>
        /// A file of the table moved into the scope from outside of it concerns the table, although it had no row.
        /// </summary>
        [Test]
        public void Concerns_MovedIntoScope_True()
        {
            string movedPath = $"{SubFolder}/Outside.asset";
            Assert.IsEmpty(AssetDatabase.MoveAsset(OutsidePath, movedPath));

            Assert.IsTrue(_tracker.Concerns(Moved(OutsidePath, movedPath)));
        }

        /// <summary>
        /// A file of the table moved out of the scope concerns the table, since its rows must leave it.
        /// </summary>
        [Test]
        public void Concerns_MovedOutOfScope_True()
        {
            string movedPath = $"{TempFolder}/Inside.asset";
            Assert.IsEmpty(AssetDatabase.MoveAsset(InsidePath, movedPath));

            Assert.IsTrue(_tracker.Concerns(Moved(InsidePath, movedPath)));
        }

        /// <summary>
        /// A file of the table moved outside of the scope, from outside of it, doesn't concern the table.
        /// </summary>
        [Test]
        public void Concerns_MovedOutsideScope_False()
        {
            string movedPath = $"{TempFolder}/Renamed.asset";
            Assert.IsEmpty(AssetDatabase.MoveAsset(OutsidePath, movedPath));

            Assert.IsFalse(_tracker.Concerns(Moved(OutsidePath, movedPath)));
        }

        /// <summary>
        /// Without a folder scope, a new file of the table concerns the table wherever it is.
        /// </summary>
        [Test]
        public void Concerns_WholeProject_ImportedAnywhere()
        {
            _tracker.Scope = null;

            Assert.IsTrue(_tracker.Scope.IsWholeProject);
            Assert.IsTrue(_tracker.Concerns(Imported(OutsidePath)));
        }

        #endregion


        #region Private API

        /// <summary>
        /// Creates the changes of an import.
        /// </summary>
        /// <param name="path">The path of the imported file.</param>
        /// <returns>Returns the created changes.</returns>
        private static AssetChanges Imported(string path)
        {
            return new AssetChanges(new[] { path }, null, null, null);
        }

        /// <summary>
        /// Creates the changes of a move.
        /// </summary>
        /// <param name="fromPath">The previous path of the moved file.</param>
        /// <param name="toPath">The new path of the moved file.</param>
        /// <returns>Returns the created changes.</returns>
        private static AssetChanges Moved(string fromPath, string toPath)
        {
            return new AssetChanges(null, null, new[] { toPath }, new[] { fromPath });
        }

        #endregion

    }

}
