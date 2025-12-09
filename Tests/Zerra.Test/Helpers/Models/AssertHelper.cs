// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Reflection;
using Zerra.Reflection;

namespace Zerra.Test.Helpers.Models
{
    public static class AssertHelper
    {
        public static void AreEqual(object model1, object model2)
        {
            var sb = new StringBuilder();
            var result = Compare(model1, model2, sb);
            var errors = sb.ToString();
            Assert.Equal(String.Empty, errors);
            Assert.True(result);
        }
        public static void AreNotEqual(object model1, object model2)
        {
            var result = Compare(model1, model2, null);
            Assert.False(result);
        }

        private static bool Compare(object model1, object model2, StringBuilder sb = null)
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
                if (type1.CoreType == CoreType.String && type2.CoreType == CoreType.DateTime)
                {
                    var converted = DateTime.Parse((string)model1, null, DateTimeStyles.RoundtripKind);
                    return converted.Equals(model2);
                }
                else if (type1.CoreType == CoreType.DateTime && type2.CoreType == CoreType.String)
                {
                    var converted = DateTime.Parse((string)model2, null, DateTimeStyles.RoundtripKind);
                    return converted.Equals(model1);
                }
                if (type1.CoreType == CoreType.String && type2.CoreType != CoreType.String)
                {
                    var converted = TypeAnalyzer.Convert(model1, type2.Type);
                    return converted.Equals(model2);
                }
                else if (type1.CoreType != CoreType.String && type2.CoreType == CoreType.String)
                {
                    var converted = TypeAnalyzer.Convert(model2, type1.Type);
                    return converted.Equals(model1);
                }
                else if (type1.CoreType != type2.CoreType)
                {
                    var converted = TypeAnalyzer.Convert(model2, type1.Type);
                    return converted.Equals(model1);
                }
                else
                {
                    return model1.Equals(model2);
                }
            }

            if (type1.Type.Name == "RuntimeType")
            {
                if (type2.Type.Name != "RuntimeType")
                    return false;
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

                    var method = compareIDictionaryTMethod.MakeGenericMethod(type1.DictionaryInnerTypeDetail!.InnerTypes[0], type1.DictionaryInnerTypeDetail.InnerTypes[1], type2.DictionaryInnerTypeDetail!.InnerTypes[0], type2.DictionaryInnerTypeDetail.InnerTypes[1]);
                    return (bool)method.Invoke(null, [model1, model2, sb])!;
                }
                else if (type1.HasIDictionary)
                {
                    if (!type2.HasIDictionary)
                        return false;

                    return CompareIDictionary((IDictionary)model1, (IDictionary)model2, sb);
                }
                else if (type1.HasISetGeneric || type1.HasIReadOnlySetGeneric)
                {
                    if (!type2.HasISetGeneric && !type2.HasIReadOnlySetGeneric)
                        return false;

                    return CompareUnorderedEnumerable((IEnumerable)model1, (IEnumerable)model2);
                }
                else
                {
                    return CompareOrderedEnumerable((IEnumerable)model1, (IEnumerable)model2);
                }
            }

            var valid = true;
            foreach (var member1 in type1.SerializableMemberDetails)
            {
                var member2 = type2.SerializableMemberDetails.FirstOrDefault(x => x.Name == member1.Name);
                if (member2 is null)
                    continue;

                var value1 = member1.GetterBoxed(model1);
                var value2 = member2.GetterBoxed(model2);

                var result = Compare(value1, value2, sb);
                if (!result)
                {
                    _ = (sb?.AppendLine($"{type1.Type.Name}.{member1.Name} {value1 ?? "null"} != {value2 ?? "null"}"));
                    valid = false;
                }
            }

            return valid;
        }

        private static bool CompareOrderedEnumerable(IEnumerable model1, IEnumerable model2)
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

                var result = Compare(item1, item2);
                if (!result)
                    return false;
            }
            return true;
        }
        private static bool CompareUnorderedEnumerable(IEnumerable model1, IEnumerable model2)
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
                    var result = Compare(item1, item2);
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

        private static MethodInfo compareIDictionaryTMethod = typeof(AssertHelper).GetMethod(nameof(CompareIDictionaryT), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static bool CompareIDictionaryT<TKey1, TValue1, TKey2, TValue2>(IEnumerable<KeyValuePair<TKey1, TValue1>> model1, IEnumerable<KeyValuePair<TKey2, TValue2>> model2, StringBuilder sb)
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
                    if (Compare(item1.Key, item2.Key))
                    {
                        found = true;
                        var result = Compare(item1.Value, item2.Value, sb);
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
        private static bool CompareIDictionary(IDictionary model1, IDictionary model2, StringBuilder sb)
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
                    if (Compare(item1.Key, item2.Key))
                    {
                        found = true;

                        var result = Compare(item1.Value, item2.Value, sb);
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
