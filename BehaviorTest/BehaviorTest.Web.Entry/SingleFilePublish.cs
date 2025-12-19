using Furion;
using System.Reflection;

namespace BehaviorTest.Web.Entry;

public class SingleFilePublish : ISingleFilePublish
{
    public Assembly[] IncludeAssemblies()
    {
        return Array.Empty<Assembly>();
    }

    public string[] IncludeAssemblyNames()
    {
        return new[]
        {
            "BehaviorTest.Application",
            "BehaviorTest.Core",
            "BehaviorTest.EntityFramework.Core",
            "BehaviorTest.Web.Core"
        };
    }
}