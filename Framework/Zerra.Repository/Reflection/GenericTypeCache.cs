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
                var type = method.ReflectedType ?? method.DeclaringType!;
                var generic = method.MakeGenericMethod(types);
                var parameters = method.GetParameters();
                var parameterTypes = parameters.Select(x => new ParameterDetail(x.ParameterType, x.Name!)).ToArray();

                var attributes = method.GetCustomAttributes(true).Cast<Attribute>().ToArray();

                Delegate? caller = AccessorGenerator.GenerateCaller(method, method.ReturnType);

                Func<object?, object?[]?, object?>? callerBoxed = AccessorGenerator.GenerateCaller(method);

                MethodDetail methodDetail;
                if (method.ReturnType.ContainsGenericParameters || method.ReturnType.IsPointer || method.ReturnType.IsByRef || method.ReturnType.IsByRefLike)
                {
                    methodDetail = new MethodDetail(type, method.Name, method.ReturnType, method.GetGenericArguments().Length, parameterTypes, caller, callerBoxed, attributes, method.IsStatic, false);
                }
                else
                {
                    var methodDetailGenericType = methodDetailType.MakeGenericType(method.ReturnType.Name == "Void" ? typeof(object) : method.ReturnType);
                    var constructor = methodDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                    methodDetail = (MethodDetail)constructor.Invoke([type, method.Name, method.GetGenericArguments().Length, parameterTypes, caller, callerBoxed, attributes, method.IsStatic, false]);
                }
                return methodDetail;
            });
            return genericMethod;
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, MethodDetail> genericMethodDetails = new();
        public static MethodDetail GetGenericMethodDetail(MethodDetail methodDetail, params Type[] types)
        {
            var key = new TypeKey(methodDetail.MethodInfo.ToString(), types);
            var genericMethod = genericMethodDetails.GetOrAdd(key, methodDetail, types, static (methodDetail, types) => GetGenericMethodDetail(methodDetail.MethodInfo, types));
            return genericMethod;
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, TypeDetail> genericTypeDetails = new();
        public static TypeDetail GetGenericTypeDetail(TypeDetail typeDetail, params Type[] types)
        {
            var key = new TypeKey(typeDetail.Type, types);
            var genericType = genericTypeDetails.GetOrAdd(key, typeDetail, types, static (typeDetail, types) =>
            {
                var generic = typeDetail.Type.MakeGenericType(types);
                return TypeAnalyzer.GetTypeDetail(generic);
            });
            return genericType;
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, Type> genericTypesByType = new();
        public static Type GetGenericType(Type type, params Type[] types)
        {
            var key = new TypeKey(type, types);
            var genericType = genericTypesByType.GetOrAdd(key, type, types, static (type, types) => type.MakeGenericType(types));
            return genericType;
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, TypeDetail> genericTypeDetailsByType = new();
        public static TypeDetail GetGenericTypeDetail(Type type, params Type[] types)
        {
            var key = new TypeKey(type, types);
            var genericTypeDetail = genericTypeDetailsByType.GetOrAdd(key, type, types, static (type, types) => TypeAnalyzer.GetTypeDetail(type.MakeGenericType(types)));
            return genericTypeDetail;
        }
    }
}
