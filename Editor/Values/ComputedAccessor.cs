using System;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// The accessor of a computed column. Its values are not read from each asset alone: the compute stage of the pipeline computes
    /// them with the context of the table (see <see cref="TablePipeline"/>).
    /// </summary>
    internal class ComputedAccessor : IValueAccessor
    {

        #region Fields

        /// <summary>
        /// The method that computes the values.
        /// </summary>
        private ComputedMethod _method = null;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="ComputedAccessor"/>
        /// <param name="method"><inheritdoc cref="_method" path="/summary"/></param>
        /// <exception cref="ArgumentNullException">Thrown if the method is null.</exception>
        public ComputedAccessor(ComputedMethod method)
        {
            _method = method ?? throw new ArgumentNullException(nameof(method));
        }

        #endregion


        #region Public API

        /// <inheritdoc/>
        public Type ValueType => _method.ValueType;

        /// <summary>
        /// The text that names the method in the messages: its declaring type and its name.
        /// </summary>
        public string MethodLabel => $"{_method.Method.DeclaringType}.{_method.Method.Name}()";

        /// <summary>
        /// Not supported: the values of a computed column need the context of the table. Use <see cref="Compute"/> instead.
        /// </summary>
        /// <param name="context">Ignored.</param>
        /// <returns>Never returns.</returns>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public object Read(ValueReadContext context)
        {
            throw new NotSupportedException($"The values of the computed column {MethodLabel} are computed by the pipeline, with the " +
                "context of the table: they can't be read from an asset alone.");
        }

        /// <summary>
        /// Computes the value of an asset.
        /// </summary>
        /// <param name="asset">The asset of which to compute the value.</param>
        /// <param name="context">The context of the table.</param>
        /// <returns>Returns the computed value.</returns>
        /// <exception cref="Exception">Thrown by the method of the column.</exception>
        public object Compute(Object asset, ITableContext context)
        {
            return _method.Invoke(asset, context);
        }

        #endregion

    }

}
