using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Zerra.Reflection.Runtime;

namespace Zerra.Reflection.Generation
{
    public abstract class TypeDetailTGenerationBase<T> : TypeDetail<T>
    {
        public TypeDetailTGenerationBase() : base(typeof(T)) { }

        protected abstract Func<MethodDetail<T>[]> CreateMethodDetails { get; }
        private MethodDetail<T>[]? methodDetails = null;
        public override sealed IReadOnlyList<MethodDetail<T>> MethodDetails
        {
            get
            {
                if (methodDetails is null)
                {
                    lock (locker)
                    {
                        methodDetails ??= CreateMethodDetails();
                    }
                }
                return methodDetails;
            }
        }
        public override sealed IReadOnlyList<MethodDetail> MethodDetailsBoxed
        {
            get
            {
                if (methodDetails is null)
                {
                    lock (locker)
                    {
                        methodDetails ??= CreateMethodDetails();
                    }
                }
                return methodDetails;
            }
        }

        protected abstract Func<ConstructorDetail<T>[]> CreateConstructorDetails { get; }
        private ConstructorDetail<T>[]? constructorDetails = null;
        public override sealed IReadOnlyList<ConstructorDetail<T>> ConstructorDetails
        {
            get
            {
                if (constructorDetails is null)
                {
                    lock (locker)
                    {
                        constructorDetails ??= CreateConstructorDetails();
                    }
                }
                return constructorDetails;
            }
        }
        public override sealed IReadOnlyList<ConstructorDetail> ConstructorDetailsBoxed
        {
            get
            {
                if (constructorDetails is null)
                {
                    lock (locker)
                    {
                        constructorDetails ??= CreateConstructorDetails();
                    }
                }
                return constructorDetails;
            }
        }

        protected abstract Func<MemberDetail[]> CreateMemberDetails { get; }
        private MemberDetail[]? memberDetails = null;
        public override sealed IReadOnlyList<MemberDetail> MemberDetails
        {
            get
            {
                if (memberDetails is null)
                {
                    lock (locker)
                    {
                        memberDetails ??= CreateMemberDetails();
                    }
                }
                return memberDetails;
            }
        }

        protected abstract Func<Attribute[]> CreateAttributes { get; }
        private Attribute[]? attributes = null;
        public override sealed IReadOnlyList<Attribute> Attributes
        {
            get
            {
                if (attributes is null)
                {
                    lock (locker)
                    {
                        attributes ??= CreateAttributes();
                    }
                }
                return attributes;
            }
        }

        protected void LoadConstructorInfo()
        {
            if (Type.IsGenericTypeDefinition)
                throw new InvalidOperationException($"ConstructorInfo cannot be used for unresolved generic types");

            var constructors = Type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var constructorParameters = constructors.ToDictionary(x => x, x => x.GetParameters());
            foreach (var constructorDetail in ConstructorDetails)
            {
                var constructor = constructors.FirstOrDefault(x => SignatureCompare(constructorParameters[x], constructorDetail));
                if (constructor == null)
                    throw new InvalidOperationException($"ConstructorInfo not found for generated constructor new({String.Join(", ", constructorDetail.ParametersDetails.Select(x => x.Type.GetNiceName()))})");

                var constructorBase = (ConstructorDetailGenerationBase<T>)constructorDetail;
                constructorBase.SetConstructorInfo(constructor);
            }
        }

        protected void LoadMethodInfo()
        {
            if (Type.IsGenericTypeDefinition)
                throw new InvalidOperationException($"MethodInfo cannot be used for unresolved generic types");

            var methods = Type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            var methodParameters = methods.ToDictionary(x => x, x => x.GetParameters());
            foreach (var methodDetail in MethodDetails)
            {
                var methodBase = (MethodDetailGenerationBase<T>)methodDetail;
                var method = methods.FirstOrDefault(x => SignatureCompare(x.Name, methodParameters[x], methodDetail));
                if (method == null)
                    throw new InvalidOperationException($"MethodInfo not found for generated method {methodDetail.Name}({String.Join(", ", methodDetail.ParameterDetails.Select(x => x.Type.GetNiceName()))})");

                methodBase.SetMethodInfo(method);
            }
        }

        protected void LoadMemberInfo()
        {
            if (Type.IsGenericTypeDefinition)
                throw new InvalidOperationException($"MemberInfo cannot be used for unresolved generic types");

            IEnumerable<PropertyInfo> properties = Type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            IEnumerable<FieldInfo> fields = Type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var memberDetail in MemberDetails)
            {
                var property = properties.FirstOrDefault(x => x.Name == memberDetail.Name);
                if (property != null)
                {
                    memberDetail.SetMemberInfo(property);
                    continue;
                }
                var field = fields.FirstOrDefault(x => x.Name == memberDetail.Name);
                if (field != null)
                {
                    memberDetail.SetMemberInfo(field);
                    continue;
                }
                throw new InvalidOperationException($"MemberInfo not found for generated member {Type.GetNiceName()}.{memberDetail.Name}");
            }
        }
    }
}
