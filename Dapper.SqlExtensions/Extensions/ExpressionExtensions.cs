using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Dapper.SqlExtensions.Extensions
{
    internal static class ExpressionExtensions
    {
        private static IEnumerable<BinaryExpression> GetBinaryExpressions(this Expression binaryExpressionLeft)
        {
            var lastExp = (BinaryExpression) binaryExpressionLeft;
            while (lastExp.Left is BinaryExpression && !(lastExp.Left is MemberExpression))
            {
                if (lastExp.Right is BinaryExpression)
                    foreach (var binaryExpression in GetBinaryExpressions(lastExp.Right))
                        yield return binaryExpression;

                lastExp = (BinaryExpression) lastExp.Left;
            }

            yield return lastExp;
        }

        internal static IEnumerable<PropertyInfo> GetPropertiesFromExpression<T, TProperty>(
            this Expression<Func<T, TProperty>> propertyLambda)
        {
            var type = typeof(T);

            if (propertyLambda.Body is NewExpression newExpression &&
                newExpression.Arguments.All(i => i is MemberExpression))
            {
                foreach (var memberExpression in newExpression.Arguments.Cast<MemberExpression>())
                {
                    var propertyFromExpression = memberExpression.Member as PropertyInfo;
                    yield return propertyFromExpression;
                }

                yield break;
            }

            if (!(propertyLambda.Body is MemberExpression member))
                throw new ArgumentException($"Expression '{propertyLambda}' refers to a method, not a property.");

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException(
                    $"Expression '{propertyLambda}' refers to a field, not a property.");

            if (type != propInfo.ReflectedType &&
                !type.IsSubclassOf(propInfo.ReflectedType ?? throw new InvalidOperationException()))
                throw new ArgumentException(
                    $"Expression '{propertyLambda}' refers to a property that is not from type {type}.");
            yield return propInfo;
        }

        internal static IEnumerable<KeyValuePair<PropertyInfo, object>> GetPropertyAndValuePairFromBinaryExpression<T,
            TProperty>(
            this Expression<Func<T, TProperty>> propertyLambda)
        {
            if (propertyLambda.Body is BinaryExpression binaryExpression)
            {
                if (!(binaryExpression.Left is MemberExpression member))
                {
                    var binaryExpressions = new List<BinaryExpression>();
                    binaryExpressions.AddRange(binaryExpression.Left.GetBinaryExpressions());
                    binaryExpressions.AddRange(binaryExpression.Right.GetBinaryExpressions());

                    foreach (var expression in binaryExpressions)
                        if (expression.Left is MemberExpression memberExpression)
                        {
                            var property = memberExpression.Member as PropertyInfo;
                            var value = GetValueFromRightExpression(expression.Right);
                            yield return new KeyValuePair<PropertyInfo, object>(property, value);
                        }
                }
                else
                {
                    var property = member.Member as PropertyInfo;
                    var value = GetValueFromRightExpression(binaryExpression.Right);
                    yield return new KeyValuePair<PropertyInfo, object>(property, value);
                }
            }
        }

        private static object GetValueFromRightExpression(this Expression expression)
        {
            if (expression is ConstantExpression constantExpression) return constantExpression.Value;

            if (expression is MemberExpression memberExpression)
            {
                var objectMember = Expression.Convert(memberExpression, typeof(object));
                var getterLambda = Expression.Lambda<Func<object>>(objectMember);
                var getter = getterLambda.Compile();
                return getter();
            }

            return null;
        }
    }
}