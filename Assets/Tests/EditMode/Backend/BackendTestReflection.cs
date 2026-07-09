using System;
using System.Linq;
using System.Reflection;

public static class BackendTestReflection
{
    public static Type GetType(string fullName)
    {
        var type = AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetType(fullName))
            .FirstOrDefault(value => value != null);

        if (type == null)
        {
            throw new InvalidOperationException($"Type not found: {fullName}");
        }

        return type;
    }

    public static object InvokeStatic(Type type, string methodName, params object[] args)
    {
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null)
        {
            throw new MissingMethodException(type.FullName, methodName);
        }

        return method.Invoke(null, args);
    }

    public static object GetField(object target, string fieldName)
    {
        var type = target is Type targetType ? targetType : target.GetType();
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        var field = type.GetField(fieldName, flags);
        if (field == null)
        {
            throw new MissingFieldException(type.FullName, fieldName);
        }

        return field.GetValue(target is Type ? null : target);
    }
}
