using System.Reflection;
using Zerra.Collections;
using Zerra.Reflection.Dynamic;

namespace Zerra.Reflection
{
    public static class GenericTypeCache
    {
        private static readonly Type methodDetailType = typeof(MethodDetail<>);
        private static readonly ConcurrentFactoryDictionary<TypeKey, MethodDetail> genericMethodDetailsByMethod = new();
        public static MethodDetail GetGenericMethodDetail(MethodInfo method, params Type[] types)
        {
            if (method.ReflectedType is null)
                throw new ArgumentNullException("method.ReflectedType");
            var key = new TypeKey(method.ToString(), types);
            var genericMethod = genericMethodDetailsByMethod.GetOrAdd(key, method, types, static (method, types) =>
            {
                var generic = method.MakeGenericMethod(types);
                var type = generic.ReflectedType ?? generic.DeclaringType!;
                var parameters = generic.GetParameters();
                var parameterTypes = parameters.Select(x => new ParameterDetail(x.ParameterType, x.Name!)).ToArray();

                var attributes = generic.GetCustomAttributes(true).Cast<Attribute>().ToArray();

                Delegate? caller = AccessorGenerator.GenerateCaller(generic, generic.ReturnType);

                Func<object?, object?[]?, object?>? callerBoxed = AccessorGenerator.GenerateCaller(generic);

                var genericArguments = generic.GetGenericArguments();

                MethodDetail methodDetail;
                if (generic.ReturnType.ContainsGenericParameters || generic.ReturnType.IsPointer || generic.ReturnType.IsByRef || generic.ReturnType.IsByRefLike)
                {
                    methodDetail = new MethodDetail(type, generic.Name, generic.ReturnType, genericArguments, parameterTypes, caller, callerBoxed, attributes, generic.IsStatic, false);
                }
                else
                {
                    var methodDetailGenericType = methodDetailType.MakeGenericType(generic.ReturnType.Name == "Void" ? typeof(object) : generic.ReturnType);
                    var constructor = methodDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                    methodDetail = (MethodDetail)constructor.Invoke([type, generic.Name, genericArguments, parameterTypes, caller, callerBoxed, attributes, generic.IsStatic, false]);
                }
                return methodDetail;
            });
            return genericMethod;
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, Type> genericTypesByType = new();
        public static Type GetGenericType(Type type, params Type[] types)
        {
            var key = new TypeKey(type, types);
            var genericType = genericTypesByType.GetOrAdd(key, type, types, static (type, types) => type.MakeGenericType(types));
            return genericType;
        }
    }
}
