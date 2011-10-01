﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using System.Reflection;

namespace LinqKit
{
    /// <summary>
    /// Custom expresssion visitor for ExpandableQuery. This expands calls to Expression.Compile() and
    /// collapses captured lambda references in subqueries which LINQ to SQL can't otherwise handle.
    /// </summary>
    partial class ExpressionExpander : ExpressionVisitor
    {
        // Replacement parameters - for when invoking a lambda expression.
        Dictionary<ParameterExpression, Expression> _replaceVars = null;

		internal ExpressionExpander () { }

		private ExpressionExpander (Dictionary<ParameterExpression, Expression> replaceVars)
		{
			_replaceVars = replaceVars;
		}

		protected override Expression VisitParameter (ParameterExpression p)
		{
			if ((_replaceVars != null) && (_replaceVars.ContainsKey (p)))
				return _replaceVars[p];
			else
				return base.VisitParameter (p);
		}

		/// <summary>
		/// Flatten calls to Invoke so that Entity Framework can understand it. Calls to Invoke are generated
		/// by PredicateBuilder.
		/// </summary>
		protected override Expression VisitInvocation (InvocationExpression iv)
		{
			Expression target = iv.Expression;
			if (target is MemberExpression) target = TransformExpr ((MemberExpression)target);
			if (target is ConstantExpression) target = ((ConstantExpression)target).Value as Expression;

			LambdaExpression lambda = (LambdaExpression)target;

			Dictionary<ParameterExpression, Expression> replaceVars;
			if (_replaceVars == null)
				replaceVars = new Dictionary<ParameterExpression, Expression> ();
			else
				replaceVars = new Dictionary<ParameterExpression, Expression> (_replaceVars);

			try
			{
				for (int i = 0; i < lambda.Parameters.Count; i++)
					replaceVars.Add (lambda.Parameters[i], iv.Arguments[i]);
			}
			catch (ArgumentException ex)
			{
				throw new InvalidOperationException ("Invoke cannot be called recursively - try using a temporary variable.", ex);
			}

			return new ExpressionExpander (replaceVars).Visit (lambda.Body);
		}


        private Expression MyVisitRecursive(Expression target)
        {
            if (target is MemberExpression) return MyVisitRecursive(TransformExpr((MemberExpression)target));

            // Added by Ryan D. Hatch, GeniusCode on 2010.01.19 - Allows support for Methods that return Expressions
            if (target is MethodCallExpression) return MyVisitRecursive(TransformExpr((MethodCallExpression)target));

            // Added by Ryan D. Hatch, GeniusCode on 2011.07.08 - Allows support for Funcs that return Expressions
            if (target is InvocationExpression) return MyVisitRecursive(((InvocationExpression)target).Expression);

            if (target is ConstantExpression) return MyVisitRecursive(((ConstantExpression)target).Value as Expression);

            if (target is LambdaExpression) return target;
            

            throw new NotSupportedException();
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.Name == "Invoke" && m.Method.DeclaringType == typeof(Extensions))
            {
                Expression target = m.Arguments[0];

                //LambdaExpression lambda = (LambdaExpression)target;
                LambdaExpression lambda = (LambdaExpression)MyVisitRecursive(target);

				Dictionary<ParameterExpression, Expression> replaceVars;
				if (_replaceVars == null)
					replaceVars = new Dictionary<ParameterExpression, Expression> ();
				else
					replaceVars = new Dictionary<ParameterExpression, Expression> (_replaceVars);

				try
				{
					for (int i = 0; i < lambda.Parameters.Count; i++)
						replaceVars.Add (lambda.Parameters[i], m.Arguments[i + 1]);
				}
				catch (ArgumentException ex)
				{
					throw new InvalidOperationException ("Invoke cannot be called recursively - try using a temporary variable.", ex);
				}

				return new ExpressionExpander (replaceVars).Visit (lambda.Body);
			}

			// Expand calls to an expression's Compile() method:
			if (m.Method.Name == "Compile" && m.Object is MemberExpression)
			{
				var me = (MemberExpression)m.Object;
				Expression newExpr = TransformExpr (me);
				if (newExpr != me) return newExpr;
			}

            // Strip out any nested calls to AsExpandable():
            if (m.Method.Name == "AsExpandable" && m.Method.DeclaringType == typeof(Extensions))
                return m.Arguments[0];

			return base.VisitMethodCall (m);
		}

		protected override Expression VisitMemberAccess (MemberExpression m)
		{
			// Strip out any references to expressions captured by outer variables - LINQ to SQL can't handle these:
			if (m.Member.DeclaringType.Name.StartsWith ("<>"))
				return TransformExpr (m);

			return base.VisitMemberAccess (m);
		}

        public Expression Visit(Expression exp)
        {
            return base.Visit(exp);
        }

        //TODO: Maybe override other member calls



    }
}
