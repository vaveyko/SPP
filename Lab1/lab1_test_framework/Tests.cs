using System;
using System.Linq.Expressions;

namespace lab1_test_framework
{
    public class Tests
    {
        public static void IsEqual(object current, object expected)
        {
            if (!current.Equals(expected))
            {
                string msg = $"Ожидалось равенство но {current} != {expected}";
                throw new BaseAssert(msg);
            }
        }

        public static void IsNotEqual(object current, object expected)
        {
            if (current.Equals(expected))
            {
                string msg = $"Ожидалось неравенство но {current} == {expected}";
                throw new BaseAssert(msg);
            }
        }

        public static void IsTrue(bool flag)
        {
            if (!flag)
            {
                string msg = $"Ожидалось True НО answer = {flag}";
                throw new BaseAssert(msg);
            }
        }

        public static void IsFalse(bool flag)
        {
            if (flag)
            {
                string msg = $"Ожидалось False НО answer = {flag}";
                throw new BaseAssert(msg);
            }
        }

        public static void IsNull(object obj)
        {
            if (obj != null)
            {
                string msg = $"Ожидалось null значение, получено {obj.ToString()}";
                throw new BaseAssert(msg);
            }
        }

        public static void IsNotNull(object obj)
        {
            if (obj == null)
            {
                string msg = "Получено null а ожидался объект";
                throw new BaseAssert(msg);
            }
        }

        public static void IsGreater(int current, int min)
        {
            if (current <= min)
            {
                string msg = $"Ожидалось что {current} больше {min}";
                throw new BaseAssert(msg);
            }
        }

        public static void IsLess(int current, int max)
        {
            if (current >= max)
            {
                string msg = $"Ожидалось что {current} меньше {max}";
                throw new BaseAssert(msg);
            }
        }

        public static void StringContains(string text, string part)
        {
            if (!text.Contains(part))
            {
                string msg = $"Строка '{text}' не содержит '{part}'";
                throw new BaseAssert(msg);
            }    
        }

        public static void CollectionCount(int count, int expected)
        {
            if (count != expected) 
            {
                string msg = $"В коллекции {count} элементов, а ожидалось {expected}";
                throw new BaseAssert(msg);
            }
        }


        public static void Check(Expression<Func<bool>> expression)
        {
            Func<bool> compiled = expression.Compile();
            bool result = compiled();

            // Если тест прошел успешно - ничего не делаем
            if (result) return;

            string errorMessage = "\nПровалена проверка выражения: ";

            if (expression.Body is BinaryExpression binaryExpr)
            {
                string leftName = GetExpressionText(binaryExpr.Left);
                string rightName = GetExpressionText(binaryExpr.Right);

                // значения левой и правой части в момент падения
                object leftValue = GetExpressionValue(binaryExpr.Left);
                object rightValue = GetExpressionValue(binaryExpr.Right);

                string operatorStr = GetOperatorString(binaryExpr.NodeType);

                errorMessage += $"\nОжидалось: {leftName} {operatorStr} {rightName}";
                errorMessage += $"\nФактически: {leftValue} {operatorStr} {rightValue}";
            }
            else
            {
                errorMessage += expression.Body.ToString();
            }

            throw new BaseAssert(errorMessage);
        }

        // имя переменной из выражения
        private static string GetExpressionText(Expression expr)
        {
            if (expr is MemberExpression memberExpr)
            {
                return memberExpr.Member.Name;
            }
            if (expr is ConstantExpression constExpr)
            {
                return constExpr.Value?.ToString() ?? "null";
            }
            return expr.ToString();
        }

        // вычисляет значения ветки выражения
        private static object GetExpressionValue(Expression expr)
        {
            var objectMember = Expression.Convert(expr, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }

        // пишем знак оператора
        private static string GetOperatorString(ExpressionType nodeType)
        {
            return nodeType switch
            {
                ExpressionType.Equal => "==",
                ExpressionType.NotEqual => "!=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                _ => nodeType.ToString()
            };
        }
    }
}
