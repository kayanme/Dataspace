using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Dataspace.Common;

namespace Dataspace.Common.Utility
{
    internal sealed class ExpressionTreeComparer:IEqualityComparer<Expression>
    {

        


        public bool Equals(Expression x, Expression y)
        {
            if (x.NodeType != y.NodeType)
                return false;

            if (x is UnaryExpression && y is UnaryExpression)
                return Equals((x as UnaryExpression).Operand, (y as UnaryExpression).Operand);

            if (x is BinaryExpression && y is BinaryExpression)
                return (Equals((x as BinaryExpression).Left, (y as BinaryExpression).Left)
                        && Equals((x as BinaryExpression).Right, (y as BinaryExpression).Right)
                        || Equals((x as BinaryExpression).Right, (y as BinaryExpression).Left)
                        && Equals((x as BinaryExpression).Left, (y as BinaryExpression).Right));
            
            if (x is ConditionalExpression && y is ConditionalExpression)
                return Equals((x as ConditionalExpression).Test, (y as ConditionalExpression).Test)
                       && Equals((x as ConditionalExpression).IfTrue, (y as ConditionalExpression).IfTrue)
                       && Equals((x as ConditionalExpression).IfFalse, (y as ConditionalExpression).IfFalse);

            if (x is ConstantExpression && y is ConstantExpression)
                return (x as ConstantExpression).Value == (y as ConstantExpression).Value;

            if (x is ParameterExpression && y is ParameterExpression)
                return (x as ParameterExpression).Type == (y as ParameterExpression).Type;

            if (x is MemberExpression && y is MemberExpression)
                return (x as MemberExpression).Member == (y as MemberExpression).Member;

            if (x is LambdaExpression && y is LambdaExpression)
                return Equals((x as LambdaExpression).Body,(y as LambdaExpression).Body);

            if (x is NewExpression && y is NewExpression)
                return (x as NewExpression).Constructor == (y as NewExpression).Constructor
                    && (x as NewExpression).Arguments.Zip((y as NewExpression).Arguments,Equals).All(k=>k);

            if (x is InvocationExpression && y is InvocationExpression)
                return Equals((x as InvocationExpression).Expression, (y as InvocationExpression).Expression)
                    && (x as InvocationExpression).Arguments.Zip((y as InvocationExpression).Arguments, Equals).All(k => k);
              
            return false;
        }

        public int GetHashCode(Expression obj)
        {

            if (obj is UnaryExpression)
                return GetHashCode((obj as UnaryExpression).Operand) + obj.NodeType.GetHashCode();

            if (obj is BinaryExpression)
                return GetHashCode((obj as BinaryExpression).Left) + GetHashCode((obj as BinaryExpression).Right)+ obj.NodeType.GetHashCode();;

            if (obj is ConditionalExpression)
                return  GetHashCode((obj as ConditionalExpression).Test)
                      + GetHashCode((obj as ConditionalExpression).IfTrue)
                      + GetHashCode((obj as ConditionalExpression).IfFalse)
                      + obj.NodeType.GetHashCode();

            if (obj is ConstantExpression)
                return (obj as ConstantExpression).Value.ByDefault(k=>k.GetHashCode(),0) + obj.NodeType.GetHashCode();

            if (obj is ParameterExpression)
                return (obj as ParameterExpression).Type.GetHashCode() + obj.NodeType.GetHashCode();

            if (obj is MemberExpression)
                return (obj as MemberExpression).Type.GetHashCode() + obj.NodeType.GetHashCode();

            if (obj is LambdaExpression)
                return GetHashCode((obj as LambdaExpression).Body) + obj.NodeType.GetHashCode();


            if (obj is NewExpression)
                return  (obj as NewExpression).Constructor.GetHashCode() + (obj as NewExpression).Arguments.Sum(k=>GetHashCode(k)) + obj.NodeType.GetHashCode();

            if (obj is InvocationExpression)
                return GetHashCode((obj as InvocationExpression).Expression) + (obj as InvocationExpression).Arguments.Sum(k => GetHashCode(k)) + obj.NodeType.GetHashCode();


            throw new NotImplementedException();
        }
    }
}
