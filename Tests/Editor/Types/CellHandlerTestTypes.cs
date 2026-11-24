using System;

using SideXP.Database.EditorOnly;

namespace SideXP.Database.Tests
{

    /// <summary>
    /// Attribute that selects <see cref="TestCellHandler"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class TestCellAttribute : Attribute { }

    /// <summary>
    /// Handler of the fields marked with <see cref="TestCellAttribute"/>.
    /// </summary>
    [CustomCellHandler(typeof(TestCellAttribute))]
    public class TestCellHandler : CellHandler { }

    /// <summary>
    /// Base value type of which the handler is used for the derived types too.
    /// </summary>
    public class TestBaseValue { }

    /// <summary>
    /// Value type derived from <see cref="TestBaseValue"/>, without its own handler.
    /// </summary>
    public class TestDerivedValue : TestBaseValue { }

    /// <summary>
    /// Handler of <see cref="TestBaseValue"/> and its derived types.
    /// </summary>
    [CustomCellHandler(typeof(TestBaseValue), UseForDerivedTypes = true)]
    public class TestBaseValueHandler : CellHandler { }

    /// <summary>
    /// Value type of which the handler is not used for the derived types.
    /// </summary>
    public class TestExactValue { }

    /// <summary>
    /// Value type derived from <see cref="TestExactValue"/>, without its own handler.
    /// </summary>
    public class TestExactDerivedValue : TestExactValue { }

    /// <summary>
    /// Handler of <see cref="TestExactValue"/> only.
    /// </summary>
    [CustomCellHandler(typeof(TestExactValue))]
    public class TestExactValueHandler : CellHandler { }

}
