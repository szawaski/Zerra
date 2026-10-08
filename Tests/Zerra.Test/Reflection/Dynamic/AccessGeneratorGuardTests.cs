// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Reflection;
using System.Reflection.Emit;
using Xunit;
using Zerra.Reflection.Dynamic;

namespace Zerra.Test.Reflection.Dynamic
{
    public class AccessGeneratorGuardTests
    {
        public sealed unsafe class UnreachableMembers
        {
            private int value;
            public int WriteOnly { set => this.value = value; }
            public int ReadOnly => value;
            public int* Pointer { get => null; set { } }
            public ref int Reference => ref value;
            public int* PointerField;
            public int* PointerMethod() => null;
            public ref int ReferenceMethod() => ref value;
            public T GenericMethod<T>() => default!;
        }

        //calls every overload of the named generator, generic ones with object for their type arguments
        private static List<object?> CallAll(string name, MemberInfo member)
        {
            var results = new List<object?>();
            foreach (var method in typeof(AccessorGenerator).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != name || !method.GetParameters()[0].ParameterType.IsAssignableFrom(member.GetType()))
                    continue;
                var call = method.IsGenericMethodDefinition ? method.MakeGenericMethod(method.GetGenericArguments().Select(x => typeof(object)).ToArray()) : method;
                var args = new object[call.GetParameters().Length];
                args[0] = member;
                for (var i = 1; i < args.Length; i++)
                    args[i] = typeof(object);
                results.Add(call.Invoke(null, args));
            }
            Assert.NotEmpty(results);
            return results;
        }

        private static void AssertAllNull(string name, MemberInfo member)
        {
            foreach (var result in CallAll(name, member))
                Assert.Null(result);
        }

        [Fact]
        public void MembersThatCantBeAccessed_GetNoAccessor()
        {
            var type = typeof(UnreachableMembers);

            AssertAllNull(nameof(AccessorGenerator.GenerateGetter), type.GetProperty(nameof(UnreachableMembers.WriteOnly))!);
            AssertAllNull(nameof(AccessorGenerator.GenerateSetter), type.GetProperty(nameof(UnreachableMembers.ReadOnly))!);
            foreach (var name in new[] { nameof(UnreachableMembers.Pointer), nameof(UnreachableMembers.Reference) })
            {
                AssertAllNull(nameof(AccessorGenerator.GenerateGetter), type.GetProperty(name)!);
                AssertAllNull(nameof(AccessorGenerator.GenerateSetter), type.GetProperty(name)!);
            }

            AssertAllNull(nameof(AccessorGenerator.GenerateGetter), type.GetField(nameof(UnreachableMembers.PointerField))!);
            AssertAllNull(nameof(AccessorGenerator.GenerateSetter), type.GetField(nameof(UnreachableMembers.PointerField))!);

            foreach (var name in new[] { nameof(UnreachableMembers.PointerMethod), nameof(UnreachableMembers.ReferenceMethod), nameof(UnreachableMembers.GenericMethod) })
                AssertAllNull(nameof(AccessorGenerator.GenerateCaller), type.GetMethod(name)!);

            //members of a ref struct
            var spanLength = typeof(ReadOnlySpan<byte>).GetProperty(nameof(ReadOnlySpan<byte>.Length))!;
            AssertAllNull(nameof(AccessorGenerator.GenerateGetter), spanLength);
            AssertAllNull(nameof(AccessorGenerator.GenerateSetter), spanLength);
            var spanField = typeof(ReadOnlySpan<byte>).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).First(x => x.FieldType == typeof(int));
            AssertAllNull(nameof(AccessorGenerator.GenerateGetter), spanField);
            AssertAllNull(nameof(AccessorGenerator.GenerateSetter), spanField);
        }

        [Fact]
        public void GlobalMembers_GetNoAccessor()
        {
            //members of a module aren't in a type
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("AccessGeneratorGlobals"), AssemblyBuilderAccess.Run);
            var module = assembly.DefineDynamicModule("AccessGeneratorGlobals");
            _ = module.DefineInitializedData("GlobalData", new byte[4], FieldAttributes.Public);
            var globalMethod = module.DefineGlobalMethod("GlobalMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(int), Type.EmptyTypes);
            var il = globalMethod.GetILGenerator();
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Ret);
            module.CreateGlobalFunctions();

            var field = Assert.Single(module.GetFields());
            Assert.Null(field.ReflectedType);
            AssertAllNull(nameof(AccessorGenerator.GenerateGetter), field);
            AssertAllNull(nameof(AccessorGenerator.GenerateSetter), field);

            var method = module.GetMethod("GlobalMethod")!;
            Assert.Null(method.ReflectedType);
            AssertAllNull(nameof(AccessorGenerator.GenerateCaller), method);
        }
    }
}
