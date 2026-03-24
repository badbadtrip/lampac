namespace Shared.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class StaticacheAttribute : Attribute
    {
        public StaticacheAttribute(int cacheMinutes)
        {
            this.cacheMinutes = cacheMinutes;
        }

        public int cacheMinutes { get; }
    }
}
