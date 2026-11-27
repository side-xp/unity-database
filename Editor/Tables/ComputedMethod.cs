using System;
using System.Reflection;
using System.Runtime.ExceptionServices;

using Object = UnityEngine.Object;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// A method of a computed column (see <see cref="ComputedColumnAttribute"/>), with a valid signature.
    /// </summary>
    internal class ComputedMethod
    {

        #region Fields

        /// <summary>
        /// The static method that computes the values.
        /// </summary>
        private MethodInfo _method = null;

        /// <summary>
        /// The type of the assets the column applies to (see <see cref="ComputedColumnAttribute.TargetType"/>).
        /// </summary>
        private Type _targetType = null;

        /// <summary>
        /// Checks if the method takes the context of the table as its second parameter.
        /// </summary>
        private bool _hasContext = false;

        /// <summary>
        /// The arguments passed to the method, reused by each call to avoid an allocation per row.
        /// </summary>
        private object[] _arguments = null;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="ComputedMethod"/>
        /// <param name="method"><inheritdoc cref="_method" path="/summary"/></param>
        /// <param name="targetType"><inheritdoc cref="_targetType" path="/summary"/></param>
        /// <param name="hasContext"><inheritdoc cref="_hasContext" path="/summary"/></param>
        private ComputedMethod(MethodInfo method, Type targetType, bool hasContext)
        {
            _method = method;
            _targetType = targetType;
            _hasContext = hasContext;
            _arguments = new object[hasContext ? 2 : 1];
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_method"/>
        public MethodInfo Method => _method;

        /// <inheritdoc cref="_targetType"/>
        public Type TargetType => _targetType;

        /// <summary>
        /// The type of the values returned by the method.
        /// </summary>
        public Type ValueType => _method.ReturnType;

        /// <inheritdoc cref="_hasContext"/>
        public bool HasContext => _hasContext;

        /// <summary>
        /// Checks if a method has the signature of a computed column, and wraps it.
        /// </summary>
        /// <param name="method">The method to check.</param>
        /// <param name="targetType">The type of the assets the column applies to.</param>
        /// <param name="computedMethod">Outputs the wrapped method, or null if its signature is invalid.</param>
        /// <param name="reason">Outputs the reason why the signature is invalid, or null if it's valid.</param>
        /// <returns>Returns true if the signature is valid.</returns>
        public static bool TryCreate(MethodInfo method, Type targetType, out ComputedMethod computedMethod, out string reason)
        {
            computedMethod = null;
            ParameterInfo[] parameters = method.GetParameters();

            if (targetType == null)
                reason = "it has no target type";
            else if (!method.IsStatic)
                reason = "it must be static";
            else if (method.ContainsGenericParameters)
                reason = "it can't be generic, nor declared in an open generic type";
            else if (method.ReturnType == typeof(void))
                reason = "it must return a value";
            else if (method.ReturnType.IsByRef)
                reason = "it can't return a reference";
            else if (parameters.Length < 1 || parameters.Length > 2)
                reason = $"it must take the asset, and optionally the {nameof(ITableContext)}";
            else if (parameters[0].ParameterType.IsByRef || !parameters[0].ParameterType.IsAssignableFrom(targetType))
                reason = $"its first parameter must be of the target type ({targetType}) or of a base type, and not by reference";
            else if (parameters.Length == 2 && parameters[1].ParameterType != typeof(ITableContext))
                reason = $"its second parameter must be an {nameof(ITableContext)}";
            else
                reason = null;

            if (reason != null)
                return false;

            computedMethod = new ComputedMethod(method, targetType, parameters.Length == 2);
            return true;
        }

        /// <summary>
        /// Computes the value of an asset.
        /// </summary>
        /// <param name="asset">The asset of which to compute the value, of the target type.</param>
        /// <param name="context">The context of the table, passed to the method if it takes it.</param>
        /// <returns>Returns the computed value.</returns>
        /// <exception cref="Exception">Thrown by the method itself, unwrapped from the <see cref="TargetInvocationException"/> of the
        /// reflection call.</exception>
        public object Invoke(Object asset, ITableContext context)
        {
            _arguments[0] = asset;
            if (_hasContext)
                _arguments[1] = context;

            try
            {
                return _method.Invoke(null, _arguments);
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                // Keeps the stack trace of the method, which is what the user needs to fix it
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
            finally
            {
                // Don't keep the asset and the context alive between computations
                Array.Clear(_arguments, 0, _arguments.Length);
            }
        }

        #endregion

    }

}
