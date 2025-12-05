// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Zerra.Collections;
using Zerra.SourceGeneration;
using Zerra.SourceGeneration.Reflection;
using Zerra.SourceGeneration.Types;

[RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
[RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
public static class GenericTypeGenerator
{
    private static readonly Type methodDetailType = typeof(MethodDetail<>);

    private static readonly ConcurrentFactoryDictionary<TypeKey, MethodDetail> genericMethodDetailsByMethod = new();
    /// <summary>
    /// Gets a <see cref="MethodDetail"/> for a generic method with the specified type arguments.
    /// </summary>
    /// <param name="method">The method to create a generic method detail for.</param>
    /// <param name="types">The type arguments for the generic method.</param>
    /// <returns>A <see cref="MethodDetail"/> for the generic method with the specified type arguments.</returns>
    /// <exception cref="ArgumentException">Thrown when the method has no Reflected Type or Declaring Type.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a caller cannot be created for the generic method.</exception>
    public static MethodDetail GetGenericMethodDetail(this MethodInfo method, params Type[] types)
    {
        if (method.ReflectedType == null && method.DeclaringType == null)
            throw new ArgumentException($"Method has no Reflected Type or Declaring Type");
        var key = new TypeKey(method.ToString(), types);
        var genericMethod = genericMethodDetailsByMethod.GetOrAdd(key, method, types, static (method, types) =>
        {
            var generic = method.MakeGenericMethod(types);
            var parameters = method.GetParameters();
            var parameterTypes = parameters.Select(x => new ParameterDetail(x.ParameterType, x.Name!)).ToArray();

            var attributes = method.GetCustomAttributes(true).Cast<Attribute>().ToArray();

            Delegate? caller = AccessorGenerator.GenerateCaller(method, method.ReturnType);
            if (caller == null)
                throw new InvalidOperationException($"Could not create caller for generic method {method.DeclaringType?.FullName}.{method.Name}");

            Func<object, object?[]?, object?>? callerBoxed = AccessorGenerator.GenerateCaller(method);
            if (callerBoxed == null)
                throw new InvalidOperationException($"Could not create boxed caller for generic method {method.DeclaringType?.FullName}.{method.Name}");

            MethodDetail methodDetail;
            if (method.ReturnType.ContainsGenericParameters || method.ReturnType.IsPointer || method.ReturnType.IsByRef || method.ReturnType.IsByRefLike)
            {
                methodDetail = new MethodDetail(method.ReflectedType ?? method.DeclaringType!, method.Name, method.ReturnType, parameterTypes, caller, callerBoxed, attributes, method.IsStatic, false);
            }
            else
            {
                var methodDetailGenericType = methodDetailType.MakeGenericType(method.ReturnType.Name == "Void" ? typeof(object) : method.ReturnType);
                var constructor = methodDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                methodDetail = (MethodDetail)constructor.Invoke([method.Name, method.ReturnType, parameterTypes, caller, callerBoxed, attributes, method.IsStatic, false]);
            }
            return methodDetail;
        });
        return genericMethod;
    }

    private static readonly ConcurrentFactoryDictionary<TypeKey, MethodDetail> genericMethodDetails = new();
    /// <summary>
    /// Gets a <see cref="MethodDetail"/> for a generic method with the specified type arguments.
    /// </summary>
    /// <param name="methodDetail">The method detail to create a generic method detail for.</param>
    /// <param name="types">The type arguments for the generic method.</param>
    /// <returns>A <see cref="MethodDetail"/> for the generic method with the specified type arguments.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the MethodInfo cannot be found from the MethodDetail.</exception>
    public static MethodDetail GetGenericMethodDetail(this MethodDetail methodDetail, params Type[] types)
    {
        var key = new TypeKey(methodDetail.ToString(), types);
        var genericMethod = genericMethodDetails.GetOrAdd(key, methodDetail, types, static (methodDetail, types) =>
        {
            var methodInfo = methodDetail.ParentType.GetMethod(methodDetail.Name, methodDetail.Parameters.Select(x => x.Type).ToArray());
            if (methodInfo == null)
                throw new InvalidOperationException($"MethodInfo was not found from this MethodDetail");
            return GetGenericMethodDetail(methodInfo, types);
        });
        return genericMethod;
    }

    private static readonly ConcurrentFactoryDictionary<TypeKey, TypeDetail> genericTypeDetails = new();
    /// <summary>
    /// Gets a <see cref="TypeDetail"/> for a generic type with the specified type arguments.
    /// </summary>
    /// <param name="typeDetail">The type detail to create a generic type detail for.</param>
    /// <param name="types">The type arguments for the generic type.</param>
    /// <returns>A <see cref="TypeDetail"/> for the generic type with the specified type arguments.</returns>
    public static TypeDetail GetGenericTypeDetail(this TypeDetail typeDetail, params Type[] types)
    {
        var key = new TypeKey(typeDetail.Type, types);
        var genericType = genericTypeDetails.GetOrAdd(key, typeDetail, types, static (typeDetail, types) =>
        {
            var generic = typeDetail.Type.MakeGenericType(types);
            return generic.GetTypeDetail();
        });
        return genericType;
    }

    private static readonly ConcurrentFactoryDictionary<TypeKey, Type> genericTypesByType = new();
    /// <summary>
    /// Gets a generic <see cref="Type"/> with the specified type arguments.
    /// </summary>
    /// <param name="type">The type to create a generic type for.</param>
    /// <param name="types">The type arguments for the generic type.</param>
    /// <returns>A generic <see cref="Type"/> with the specified type arguments.</returns>
    public static Type GetGenericType(this Type type, params Type[] types)
    {
        var key = new TypeKey(type, types);
        var genericType = genericTypesByType.GetOrAdd(key, type, types, static (type, types) => type.MakeGenericType(types));
        return genericType;
    }
}
