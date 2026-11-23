using UnityEngine;

// The fields of the test types are only read by reflection
#pragma warning disable CS0414

namespace SideXP.Database.Tests
{

    /// <summary>
    /// The fields used to test the built-in cell handlers of Unity's attributes. Not a table.
    /// </summary>
    public class AttributeHandlerTestFields
    {

        [Range(0, 1)]
        public float rangeFloat = 0;

        [Range(0.5f, 10.5f)]
        public int rangeInt = 0;

        [Range(0, 100)]
        public byte rangeByte = 0;

        [Min(2)]
        public float minFloat = 0;

        [Min(1.5f)]
        public int minInt = 0;

        [Range(0, 10)]
        [Min(5)]
        public float rangeAndMin = 0;

        [Range(0, 1)]
        public string rangeString = string.Empty;

        [ColorUsage(false, true)]
        public Color hdrColor = Color.white;

        public float plainFloat = 0;

    }

}
