using System;

using UnityEngine;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Type of the in-memory assets used to test the write path of the edits, with a field of each kind of serialized property that
    /// needs a specific write. Not a table, so it doesn't appear in the window.
    /// </summary>
    public class EditTestSO : ScriptableObject
    {

        public DayOfWeek day = DayOfWeek.Monday;

        public char letter = 'a';

        public LayerMask layers = 0;

        public Color32 tint = new Color32(0, 0, 0, 255);

        public Quaternion rotation = Quaternion.identity;

        public ScriptableObject reference = null;

    }

}
