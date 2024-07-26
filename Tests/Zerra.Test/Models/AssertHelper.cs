// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Zerra.Reflection;

namespace Zerra.Test
{
    public static class AssertHelper
    {
        public static void AreEqual(object model1, object model2, bool convert = false)
        {
            var sb = new StringBuilder();
            var result = Compare(model1, model2, convert, sb);
            var errors = sb.ToString();
            Assert.AreEqual(String.Empty, errors);
            Assert.IsTrue(result);
        }
        public static void AreNotEqual(object model1, object model2, bool convert = false)
        {
            var result = Compare(model1, model2, convert, null);
            Assert.IsFalse(result);
        }

        private static bool Compare(object model1, object model2, bool convert, StringBuilder sb = null)
        {
            if (model1 is null)
            {
                if (model2 is null)
                    return true;
                else
                    return false;
            }
            else if (model2 is null)
            {
                return false;
            }

            var type1 = model1.GetType().GetTypeDetail();
            var type2 = model2.GetType().GetTypeDetail();

            if (type1.CoreType.HasValue)
            {
                if (!type2.CoreType.HasValue)
                    return false;
                if (type1.CoreType == CoreType.String && type2.CoreType != CoreType.String)
                    return TypeAnalyzer.Convert(model1, type2.Type).Equals(model2);
                else if (type1.CoreType != CoreType.String && type2.CoreType == CoreType.String)
                    return TypeAnalyzer.Convert(model2, type1.Type).Equals(model1);
                else if (type1.CoreType != type2.CoreType)
                    return TypeAnalyzer.Convert(model2, type1.Type).Equals(model1);
                else
                    return model1.Equals(model2);
            }

            if (type1.HasIEnumerable)
            {
                if (!type2.HasIEnumerable)
                    return false;

                if (type1.HasIDictionaryGeneric || type1.HasIReadOnlyDictionaryGeneric)
                {
                    if (!type2.HasIEnumerable && !type2.HasIReadOnlyDictionaryGeneric)
                        return false;

                    var method = compareIDictionaryTMethod.GetGenericMethodDetail(type1.DictionaryInnerTypeDetail.InnerTypes[0], type1.DictionaryInnerTypeDetail.InnerTypes[1], type2.DictionaryInnerTypeDetail.InnerTypes[0], type2.DictionaryInnerTypeDetail.InnerTypes[1]);
                    return (bool)method.Caller(null, [model1, model2, convert, sb]);
                }
                else if (type1.HasIDictionary)
                {
                    if (!type2.HasIDictionary)
                        return false;

                    return CompareIDictionary((IDictionary)model1, (IDictionary)model2, convert, sb);
                }
                else if (type1.HasISetGeneric || type1.HasIReadOnlySetGeneric)
                {
                    if (!type2.HasISetGeneric && !type2.HasIReadOnlySetGeneric)
                        return false;

                    return CompareUnorderedEnumerable((IEnumerable)model1, (IEnumerable)model2, convert);
                }
                else
                {
                    return CompareOrderedEnumerable((IEnumerable)model1, (IEnumerable)model2, convert, sb);
                }
            }

            var valid = true;
            foreach (var member1 in type1.MemberDetails)
            {
                if (!type2.TryGetMember(member1.Name, out var member2))
                    continue;

                var value1 = member1.GetterBoxed(model1);
                var value2 = member2.GetterBoxed(model2);

                var result = Compare(value1, value2, convert, sb);
                if (!result)
                {
                    sb?.AppendLine($"{type1.Type.Name}.{member1.Name} {value1 ?? "null"} != {value2 ?? "null"}");
                    valid = false;
                }
            }

            return valid;
        }

        private static bool CompareOrderedEnumerable(IEnumerable model1, IEnumerable model2, bool convert, StringBuilder sb)
        {
            if (model1 is null)
            {
                if (model2 is null)
                    return true;
                else
                    return false;
            }
            else if (model2 is null)
            {
                return false;
            }

            var count1 = 0;
            var count2 = 0;
            foreach (var item1 in model1)
                count1++;
            foreach (var item2 in model2)
                count2++;
            if (count1 != count2)
                return false;

            var enumerator1 = model1.GetEnumerator();
            var enumerator2 = model2.GetEnumerator();
            while (enumerator1.MoveNext())
            {
                _ = enumerator2.MoveNext();
                var item1 = enumerator1.Current;
                var item2 = enumerator2.Current;

                var result = Compare(item1, item2, convert, sb);
                if (!result)
                    return false;
            }
            return true;
        }
        private static bool CompareUnorderedEnumerable(IEnumerable model1, IEnumerable model2, bool convert)
        {
            if (model1 is null)
            {
                if (model2 is null)
                    return true;
                else
                    return false;
            }
            else if (model2 is null)
            {
                return false;
            }

            var count1 = 0;
            var count2 = 0;
            foreach (var item1 in model1)
                count1++;
            foreach (var item2 in model2)
                count2++;
            if (count1 != count2)
                return false;

            foreach (var item1 in model1)
            {
                var found = false;
                foreach (var item2 in model2)
                {
                    var result = Compare(item1, item2, convert, null);
                    if (result)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    return false;
            }
            return true;
        }

        private static MethodDetail compareIDictionaryTMethod = typeof(AssertHelper).GetTypeDetail().GetMethod(nameof(CompareIDictionaryT));
        private static bool CompareIDictionaryT<TKey1, TValue1, TKey2, TValue2>(IEnumerable<KeyValuePair<TKey1, TValue1>> model1, IEnumerable<KeyValuePair<TKey2, TValue2>> model2, bool convert, StringBuilder sb)
            where TKey1 : notnull
            where TKey2 : notnull
        {
            if (model1 is null)
            {
                if (model2 is null)
                    return true;
                else
                    return false;
            }
            else if (model2 is null)
            {
                return false;
            }

            var count1 = 0;
            var count2 = 0;
            foreach (var item1 in model1)
                count1++;
            foreach (var item2 in model2)
                count2++;
            if (count1 != count2)
                return false;

            foreach (var item1 in model1)
            {
                var found = false;
                foreach (var item2 in model2)
                {
                    if (item1.Key.Equals(item2.Key))
                    {
                        found = true;
                        var result = Compare(item1.Value, item2.Value, convert, sb);
                        if (!result)
                            return false;
                        break;
                    }
                }
                if (!found)
                    return false;
            }
            return true;
        }
        private static bool CompareIDictionary(IDictionary model1, IDictionary model2, bool convert, StringBuilder sb)
        {
            if (model1 is null)
            {
                if (model2 is null)
                    return true;
                else
                    return false;
            }
            else if (model2 is null)
            {
                return false;
            }

            var count1 = 0;
            var count2 = 0;
            foreach (var item1 in model1)
                count1++;
            foreach (var item2 in model2)
                count2++;
            if (count1 != count2)
                return false;

            foreach (DictionaryEntry item1 in model1)
            {
                var found = false;
                foreach (DictionaryEntry item2 in model2)
                {
                    if (item1.Key.Equals(item2.Key))
                    {
                        found = true;

                        var result = Compare(item1.Value, item2.Value, convert, sb);
                        if (!result)
                            return false;
                        break;
                    }
                }
                if (!found)
                    return false;
            }
            return true;
        }
    }
}
