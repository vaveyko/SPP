using System;

namespace lab1_test_framework
{
    public class BaseAssert : Exception
    { 
        public BaseAssert(string m) : base(m) { }
    }
}
