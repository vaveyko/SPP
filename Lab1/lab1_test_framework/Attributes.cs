namespace lab1_test_framework
{

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ParameterAttribute : System.Attribute
    {
        public Object[] parameters { get; }

        public ParameterAttribute(Object[] parameters)
        {
            this.parameters = parameters;
        }
    }

    public class StartAttribute : System.Attribute { }
    public class EndAttribute : System.Attribute { }

    public class TestMethodAttribute : System.Attribute
    {
        public int DayCaloriesNorm { get; set; } = 2000;
        public string AdditionalInfo { get; set; } = string.Empty;
    }

    public class SharedContextAttribute : System.Attribute 
    {
        public int ContextId { get; set; }
        public int Priority { get; set; }
        public SharedContextAttribute(int contextId, int priority) { ContextId = contextId; Priority = priority; }
    }

    public class SharedContextParamAttribute : System.Attribute
    {
        public int DayCaloriesNorm { get; set; } = 2000;
        public string AdditionalInfo { get; set; } = string.Empty;

    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TestClassAttribute() : System.Attribute { }

    public class SkipAttribute() : System.Attribute { }

    public class TimeoutAttribute : System.Attribute {
        public int Milliseconds { get; }

        public TimeoutAttribute(int timeoutSeconds) { Milliseconds = timeoutSeconds; }
    }
}
