using UnityEngine;

// The fields of the test types are only read by reflection
#pragma warning disable CS0414

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Type of the in-memory assets used to test the pipeline and the accessors. Its public fields let the tests change the values
    /// directly. Not a table, so it doesn't appear in the window.
    /// </summary>
    public class PipelineTestSO : ScriptableObject
    {

        public int number = 0;

        public string label = string.Empty;

        [SerializeField]
        private string _secret = "secret";

    }

}
