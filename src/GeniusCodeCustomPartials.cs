using System;
using System.Linq;
using System.Linq.Expressions;
using LinqExpression = System.Linq.Expressions.Expression;

namespace LinqKit
{
    partial class ExpressionExpander
    {
        /// <summary>
        /// Transforms Invoke for Expressions on a Method
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        LinqExpression TransformExpr(MethodCallExpression input)
        {
            if (input == null) return null;

            // Require Method to return an Expression
            if (!typeof(LinqExpression).IsAssignableFrom(input.Method.ReturnType))
                throw new Exception(String.Format("For Method {0} to be Expandable - it must return an Expression!", input.Method.Name));

            // Require Method to have no arguments
            if (input.Arguments.Count > 0)
                throw new ArgumentException(String.Format("Method {0}, which returns an Expression, cannot have any Arguments!", input.Method.Name));

            object instance = input.Object;
          
            var constantExpression = instance as ConstantExpression;
            if (constantExpression != null)
                instance = constantExpression.Value;

            // Call Method
            var result = (LinqExpression)input.Method.Invoke(instance, new object[] { });

            return Visit(result);
        }

        /// <summary>
        /// Transforms Invoke for Expressions on a Field or Property
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        LinqExpression TransformExpr(MemberExpression input)
        {
            // Collapse captured outer variables
            if (input == null) return null;

            if (input.Expression == null) //static
            {
                bool success;
                switch (input.Member.MemberType)
                {
                    case System.Reflection.MemberTypes.Method:
                        return (Expression) ReflectionHelper.TryInvokeMethod(input.Member.Name, null, input.Member.DeclaringType, out success);
                    case System.Reflection.MemberTypes.Property:
                        return (Expression) ReflectionHelper.TryGetPropertyValue(input.Member.Name, null, out success, input.Member.DeclaringType);
                    case System.Reflection.MemberTypes.Field:
                        return (Expression) ReflectionHelper.TryGetFieldValue(input.Member.Name, null, out success, input.Member.DeclaringType);
                    default:
                        throw new NotSupportedException();
                }
            }

            if (input.Expression is ConstantExpression)
            {
                var obj = ((ConstantExpression)input.Expression).Value;
                if (obj == null) return input;

                // Get Constant from Field or Property
                var result = ReflectionHelper.GetMemberValue(obj, input.Member.Name);

                // Linq Expression?
                var linqResult = result as LinqExpression;
                if (linqResult != null) return Visit(linqResult);

                // Func Delegate?
                if (result is Delegate)
                {
                    var method = result as Delegate;

                    // Check to make sure there are no arguments
                    if (method.Method.GetParameters().Any())
                        throw new Exception("Cannot expand an expression that is inside a delegate which has parameters!");

                    // Check to make sure it will return a LinqExpression
                    if (typeof(LinqExpression).IsAssignableFrom(method.Method.ReturnType) == false)
                        throw new Exception("Cannot expand an expression from a delegate, because the delegate doesn't return an Expression type.");

                    // Invoke Delegate to get Expression out
                    var methodResult = method.DynamicInvoke(new object[] { });

                    linqResult = methodResult as LinqExpression;
                    if (linqResult != null) return Visit(linqResult);
                }

                throw new NotSupportedException();
            }

            return input;
        }
    }
}
