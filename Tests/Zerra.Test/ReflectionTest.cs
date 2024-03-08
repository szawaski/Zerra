// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Zerra.Reflection;

namespace Zerra.Test
{
    [TestClass]
    public class ReflectionTest
    {
        //[TestMethod]
        public void TestTypeDetails()
        {
            var type = typeof(AllTypesModel);
            var typeDetail = type.GetTypeDetail();
            InspectTypeDetail(typeDetail, new Stack<Type>());
        }


        private void InspectTypeDetail(TypeDetail typeDetail, Stack<Type> stack)
        {
            if (typeDetail.CoreType.HasValue)
                return;

            if (stack.Contains(typeDetail.Type))
                return;

            stack.Push(typeDetail.Type);

            //properties have lazy loading so need to check all the groups

            _ = typeDetail.InnerTypes;
            _ = typeDetail.EnumUnderlyingType;
            _ = typeDetail.IsGraphLocalProperty;
            var baseTypes = typeDetail.BaseTypes;
            var interfaces = typeDetail.Interfaces;
            _ = typeDetail.IsIEnumerable;
            var members = typeDetail.MemberDetails;
            var methods = typeDetail.MethodDetails;
            var constructors = typeDetail.ConstructorDetails;
            var attributes = typeDetail.Attributes;
            _ = typeDetail.SerializableMemberDetails;
            var isIEnumerableGeneric = typeDetail.IsIEnumerableGeneric;
            if (isIEnumerableGeneric)
                _ = typeDetail.IEnumerableGenericInnerTypeDetail;
            _ = typeDetail.HasTaskResultGetter;
            _ = typeDetail.HasCreator;

            foreach (var member in members)
            {
                _ = member.Attributes;
                _ = member.HasGetter;
                _ = member.HasGetterTyped;
                _ = member.HasSetter;
                _ = member.HasSetterTyped;

                InspectTypeDetail(member.TypeDetail, stack);
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
                _ = constructor.HasCreator;

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
