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

    public static object InvokeInstance(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
        {
            throw new MissingMethodException(target.GetType().FullName, methodName);
        }

        return method.Invoke(target, args);
    }

    public static object GetProperty(object target, string propertyName)
    {
        var type = target is Type targetType ? targetType : target.GetType();
        var property = type.GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        if (property == null)
        {
            throw new MissingMemberException(type.FullName, propertyName);
        }

        return property.GetValue(target is Type ? null : target);
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

    public static void SetField(object target, string fieldName, object value)
    {
        var type = target.GetType();
        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            throw new MissingFieldException(type.FullName, fieldName);
        }

        field.SetValue(target, value);
    }
}
