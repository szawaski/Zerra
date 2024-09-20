// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Zerra.Reflection;

namespace Zerra.Test
{
    //[TestClass]
    public class ReflectionTest
    {
        [TestMethod]
        public void TestTypeDetails()
        {
            var typeDetail = TypeAnalyzer<TypesAllModel>.GetTypeDetail();
            InspectTypeDetail(typeDetail, new Stack<Type>());
        }


        private static readonly MethodInfo methodInspectTypeDetail = typeof(ReflectionTest).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First(x => x.Name == nameof(InspectTypeDetail) && x.GetGenericArguments().Length == 1);
        private void InspectTypeDetail(TypeDetail typeDetail, Stack<Type> stack)
        {
            if (typeDetail.CoreType.HasValue || typeDetail.Type.Name == "Void")
                return;
            methodInspectTypeDetail.MakeGenericMethod(typeDetail.Type).Invoke(this, new object[] { typeDetail, stack });
        }
        private void InspectTypeDetail<T>(TypeDetail<T> typeDetail, Stack<Type> stack)
        {
            if (stack.Count > 2)
                return;

            if (typeDetail.CoreType.HasValue || typeDetail.Type.Name == "Void")
                return;

            if (stack.Contains(typeDetail.Type))
                return;

            stack.Push(typeDetail.Type);

            //properties have lazy loading so need to check all the groups

            _ = typeDetail.InnerTypes;
            _ = typeDetail.EnumUnderlyingType;
            var baseTypes = typeDetail.BaseTypes;
            var interfaces = typeDetail.Interfaces;
            _ = typeDetail.HasIEnumerable;
            var members = typeDetail.MemberDetails;
            var methods = typeDetail.MethodDetails;
            var constructors = typeDetail.ConstructorDetails;
            var attributes = typeDetail.Attributes;
            _ = typeDetail.SerializableMemberDetails;
            var isIEnumerableGeneric = typeDetail.HasIEnumerableGeneric;
            if (isIEnumerableGeneric)
                _ = typeDetail.IEnumerableGenericInnerTypeDetail;
            _ = typeDetail.HasTaskResultGetter;
            _ = typeDetail.HasCreator;

            foreach (var member in members)
            {
                _ = member.Attributes;
                _ = member.HasGetterBoxed;
                //_ = member.HasGetterTyped;
                _ = member.HasSetterBoxed;
                //_ = member.HasSetterTyped;

                InspectTypeDetail(member.TypeDetailBoxed, stack);
            }
            foreach (var method in methods)
            {
                _ = method.Attributes;
                _ = method.HasCaller;

                InspectTypeDetail(method.ReturnType, stack);
                foreach (var parameter in method.ParametersInfo)
                {
                    InspectTypeDetail(parameter.ParameterType.GetTypeDetail(), stack);
                }
            }
            foreach (var constructor in constructors)
            {
                _ = constructor.Attributes;
                _ = constructor.HasCreatorWithArgs;

                foreach (var parameter in constructor.ParametersInfo)
                {
                    InspectTypeDetail(parameter.ParameterType.GetTypeDetail(), stack);
                }
            }
            foreach (var attribute in attributes)
            {
                InspectTypeDetail(attribute.GetType().GetTypeDetail(), stack);
            }
            foreach (var @interface in interfaces)
            {
                InspectTypeDetail(@interface.GetTypeDetail(), stack);
            }
            foreach (var baseType in baseTypes)
            {
                InspectTypeDetail(baseType.GetTypeDetail(), stack);
            }

            stack.Pop();
        }
    }
}
