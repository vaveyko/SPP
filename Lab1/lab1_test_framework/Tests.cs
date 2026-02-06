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
    }
}
