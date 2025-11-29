// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using Xunit;
using Zerra.Test.Helpers.Models;

namespace Zerra.Test.Helpers.TypesModels
{
    [Zerra.SourceGeneration.SourceGenerationTypeDetail]
    public class TypesAllAsStringsModel
    {
        public string BooleanThing { get; set; }
        public string ByteThing { get; set; }
        public string SByteThing { get; set; }
        public string Int16Thing { get; set; }
        public string UInt16Thing { get; set; }
        public string Int32Thing { get; set; }
        public string UInt32Thing { get; set; }
        public string Int64Thing { get; set; }
        public string UInt64Thing { get; set; }
        public string SingleThing { get; set; }
        public string DoubleThing { get; set; }
        public string DecimalThing { get; set; }
        public string CharThing { get; set; }
        public string DateTimeThing { get; set; }
        public string DateTimeOffsetThing { get; set; }
        public string TimeSpanThing { get; set; }
#if NET6_0_OR_GREATER
        public string DateOnlyThing { get; set; }
        public string TimeOnlyThing { get; set; }
#endif
        public string GuidThing { get; set; }

        public string BooleanThingNullable { get; set; }
        public string ByteThingNullable { get; set; }
        public string SByteThingNullable { get; set; }
        public string Int16ThingNullable { get; set; }
        public string UInt16ThingNullable { get; set; }
        public string Int32ThingNullable { get; set; }
        public string UInt32ThingNullable { get; set; }
        public string Int64ThingNullable { get; set; }
        public string UInt64ThingNullable { get; set; }
        public string SingleThingNullable { get; set; }
        public string DoubleThingNullable { get; set; }
        public string DecimalThingNullable { get; set; }
        public string CharThingNullable { get; set; }
        public string DateTimeThingNullable { get; set; }
        public string DateTimeOffsetThingNullable { get; set; }
        public string TimeSpanThingNullable { get; set; }
#if NET6_0_OR_GREATER
        public string DateOnlyThingNullable { get; set; }
        public string TimeOnlyThingNullable { get; set; }
#endif
        public string GuidThingNullable { get; set; }

        public string BooleanThingNullableNull { get; set; }
        public string ByteThingNullableNull { get; set; }
        public string SByteThingNullableNull { get; set; }
        public string Int16ThingNullableNull { get; set; }
        public string UInt16ThingNullableNull { get; set; }
        public string Int32ThingNullableNull { get; set; }
        public string UInt32ThingNullableNull { get; set; }
        public string Int64ThingNullableNull { get; set; }
        public string UInt64ThingNullableNull { get; set; }
        public string SingleThingNullableNull { get; set; }
        public string DoubleThingNullableNull { get; set; }
        public string DecimalThingNullableNull { get; set; }
        public string CharThingNullableNull { get; set; }
        public string DateTimeThingNullableNull { get; set; }
        public string DateTimeOffsetThingNullableNull { get; set; }
        public string TimeSpanThingNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public string DateOnlyThingNullableNull { get; set; }
        public string TimeOnlyThingNullableNull { get; set; }
#endif
        public string GuidThingNullableNull { get; set; }

        public string StringThing { get; set; }
        public string StringThingNull { get; set; }
        public string StringThingEmpty { get; set; }

        public string EnumThing { get; set; }
        public string EnumThingNullable { get; set; }
        public string EnumThingNullableNull { get; set; }

        public string[] BooleanArray { get; set; }
        public string ByteArray { get; set; } //special conversion
        public string[] SByteArray { get; set; }
        public string[] Int16Array { get; set; }
        public string[] UInt16Array { get; set; }
        public string[] Int32Array { get; set; }
        public string[] UInt32Array { get; set; }
        public string[] Int64Array { get; set; }
        public string[] UInt64Array { get; set; }
        public string[] SingleArray { get; set; }
        public string[] DoubleArray { get; set; }
        public string[] DecimalArray { get; set; }
        public string[] CharArray { get; set; }
        public string[] DateTimeArray { get; set; }
        public string[] DateTimeOffsetArray { get; set; }
        public string[] TimeSpanArray { get; set; }
#if NET6_0_OR_GREATER
        public string[] DateOnlyArray { get; set; }
        public string[] TimeOnlyArray { get; set; }
#endif
        public string[] GuidArray { get; set; }

        public string[] BooleanArrayEmpty { get; set; }
        public string ByteArrayEmpty { get; set; } //special conversion
        public string[] SByteArrayEmpty { get; set; }
        public string[] Int16ArrayEmpty { get; set; }
        public string[] UInt16ArrayEmpty { get; set; }
        public string[] Int32ArrayEmpty { get; set; }
        public string[] UInt32ArrayEmpty { get; set; }
        public string[] Int64ArrayEmpty { get; set; }
        public string[] UInt64ArrayEmpty { get; set; }
        public string[] SingleArrayEmpty { get; set; }
        public string[] DoubleArrayEmpty { get; set; }
        public string[] DecimalArrayEmpty { get; set; }
        public string[] CharArrayEmpty { get; set; }
        public string[] DateTimeArrayEmpty { get; set; }
        public string[] DateTimeOffsetArrayEmpty { get; set; }
        public string[] TimeSpanArrayEmpty { get; set; }
#if NET6_0_OR_GREATER
        public string[] DateOnlyArrayEmpty { get; set; }
        public string[] TimeOnlyArrayEmpty { get; set; }
#endif
        public string[] GuidArrayEmpty { get; set; }

        public string[] BooleanArrayNull { get; set; }
        public string ByteArrayNull { get; set; } //special conversion
        public string[] SByteArrayNull { get; set; }
        public string[] Int16ArrayNull { get; set; }
        public string[] UInt16ArrayNull { get; set; }
        public string[] Int32ArrayNull { get; set; }
        public string[] UInt32ArrayNull { get; set; }
        public string[] Int64ArrayNull { get; set; }
        public string[] UInt64ArrayNull { get; set; }
        public string[] SingleArrayNull { get; set; }
        public string[] DoubleArrayNull { get; set; }
        public string[] DecimalArrayNull { get; set; }
        public string[] CharArrayNull { get; set; }
        public string[] DateTimeArrayNull { get; set; }
        public string[] DateTimeOffsetArrayNull { get; set; }
        public string[] TimeSpanArrayNull { get; set; }
#if NET6_0_OR_GREATER
        public string[] DateOnlyArrayNull { get; set; }
        public string[] TimeOnlyArrayNull { get; set; }
#endif
        public string[] GuidArrayNull { get; set; }

        public string[] BooleanArrayNullable { get; set; }
        public string[] ByteArrayNullable { get; set; }
        public string[] SByteArrayNullable { get; set; }
        public string[] Int16ArrayNullable { get; set; }
        public string[] UInt16ArrayNullable { get; set; }
        public string[] Int32ArrayNullable { get; set; }
        public string[] UInt32ArrayNullable { get; set; }
        public string[] Int64ArrayNullable { get; set; }
        public string[] UInt64ArrayNullable { get; set; }
        public string[] SingleArrayNullable { get; set; }
        public string[] DoubleArrayNullable { get; set; }
        public string[] DecimalArrayNullable { get; set; }
        public string[] CharArrayNullable { get; set; }
        public string[] DateTimeArrayNullable { get; set; }
        public string[] DateTimeOffsetArrayNullable { get; set; }
        public string[] TimeSpanArrayNullable { get; set; }
#if NET6_0_OR_GREATER
        public string[] DateOnlyArrayNullable { get; set; }
        public string[] TimeOnlyArrayNullable { get; set; }
#endif
        public string[] GuidArrayNullable { get; set; }

        public string[] BooleanArrayNullableEmpty { get; set; }
        public string[] ByteArrayNullableEmpty { get; set; }
        public string[] SByteArrayNullableEmpty { get; set; }
        public string[] Int16ArrayNullableEmpty { get; set; }
        public string[] UInt16ArrayNullableEmpty { get; set; }
        public string[] Int32ArrayNullableEmpty { get; set; }
        public string[] UInt32ArrayNullableEmpty { get; set; }
        public string[] Int64ArrayNullableEmpty { get; set; }
        public string[] UInt64ArrayNullableEmpty { get; set; }
        public string[] SingleArrayNullableEmpty { get; set; }
        public string[] DoubleArrayNullableEmpty { get; set; }
        public string[] DecimalArrayNullableEmpty { get; set; }
        public string[] CharArrayNullableEmpty { get; set; }
        public string[] DateTimeArrayNullableEmpty { get; set; }
        public string[] DateTimeOffsetArrayNullableEmpty { get; set; }
        public string[] TimeSpanArrayNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public string[] DateOnlyArrayNullableEmpty { get; set; }
        public string[] TimeOnlyArrayNullableEmpty { get; set; }
#endif
        public string[] GuidArrayNullableEmpty { get; set; }

        public string[] BooleanArrayNullableNull { get; set; }
        public string[] ByteArrayNullableNull { get; set; }
        public string[] SByteArrayNullableNull { get; set; }
        public string[] Int16ArrayNullableNull { get; set; }
        public string[] UInt16ArrayNullableNull { get; set; }
        public string[] Int32ArrayNullableNull { get; set; }
        public string[] UInt32ArrayNullableNull { get; set; }
        public string[] Int64ArrayNullableNull { get; set; }
        public string[] UInt64ArrayNullableNull { get; set; }
        public string[] SingleArrayNullableNull { get; set; }
        public string[] DoubleArrayNullableNull { get; set; }
        public string[] DecimalArrayNullableNull { get; set; }
        public string[] CharArrayNullableNull { get; set; }
        public string[] DateTimeArrayNullableNull { get; set; }
        public string[] DateTimeOffsetArrayNullableNull { get; set; }
        public string[] TimeSpanArrayNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public string[] DateOnlyArrayNullableNull { get; set; }
        public string[] TimeOnlyArrayNullableNull { get; set; }
#endif
        public string[] GuidArrayNullableNull { get; set; }

        public string[] StringArray { get; set; }
        public string[] StringArrayEmpty { get; set; }
        public string[] StringArrayEmptyNull { get; set; }

        public string[] EnumArray { get; set; }
        public string[] EnumArrayEmpty { get; set; }
        public string[] EnumArrayNull { get; set; }

        public string[] EnumArrayNullable { get; set; }
        public string[] EnumArrayNullableEmpty { get; set; }
        public string[] EnumArrayNullableNull { get; set; }

        public List<string> BooleanListT { get; set; }
        public List<string> ByteListT { get; set; }
        public List<string> SByteListT { get; set; }
        public List<string> Int16ListT { get; set; }
        public List<string> UInt16ListT { get; set; }
        public List<string> Int32ListT { get; set; }
        public List<string> UInt32ListT { get; set; }
        public List<string> Int64ListT { get; set; }
        public List<string> UInt64ListT { get; set; }
        public List<string> SingleListT { get; set; }
        public List<string> DoubleListT { get; set; }
        public List<string> DecimalListT { get; set; }
        public List<string> CharListT { get; set; }
        public List<string> DateTimeListT { get; set; }
        public List<string> DateTimeOffsetListT { get; set; }
        public List<string> TimeSpanListT { get; set; }
#if NET6_0_OR_GREATER
        public List<string> DateOnlyListT { get; set; }
        public List<string> TimeOnlyListT { get; set; }
#endif
        public List<string> GuidListT { get; set; }

        public List<string> BooleanListTEmpty { get; set; }
        public List<string> ByteListTEmpty { get; set; }
        public List<string> SByteListTEmpty { get; set; }
        public List<string> Int16ListTEmpty { get; set; }
        public List<string> UInt16ListTEmpty { get; set; }
        public List<string> Int32ListTEmpty { get; set; }
        public List<string> UInt32ListTEmpty { get; set; }
        public List<string> Int64ListTEmpty { get; set; }
        public List<string> UInt64ListTEmpty { get; set; }
        public List<string> SingleListTEmpty { get; set; }
        public List<string> DoubleListTEmpty { get; set; }
        public List<string> DecimalListTEmpty { get; set; }
        public List<string> CharListTEmpty { get; set; }
        public List<string> DateTimeListTEmpty { get; set; }
        public List<string> DateTimeOffsetListTEmpty { get; set; }
        public List<string> TimeSpanListTEmpty { get; set; }
#if NET6_0_OR_GREATER
        public List<string> DateOnlyListTEmpty { get; set; }
        public List<string> TimeOnlyListTEmpty { get; set; }
#endif
        public List<string> GuidListTEmpty { get; set; }

        public List<string> BooleanListTNull { get; set; }
        public List<string> ByteListTNull { get; set; }
        public List<string> SByteListTNull { get; set; }
        public List<string> Int16ListTNull { get; set; }
        public List<string> UInt16ListTNull { get; set; }
        public List<string> Int32ListTNull { get; set; }
        public List<string> UInt32ListTNull { get; set; }
        public List<string> Int64ListTNull { get; set; }
        public List<string> UInt64ListTNull { get; set; }
        public List<string> SingleListTNull { get; set; }
        public List<string> DoubleListTNull { get; set; }
        public List<string> DecimalListTNull { get; set; }
        public List<string> CharListTNull { get; set; }
        public List<string> DateTimeListTNull { get; set; }
        public List<string> DateTimeOffsetListTNull { get; set; }
        public List<string> TimeSpanListTNull { get; set; }
#if NET6_0_OR_GREATER
        public List<string> DateOnlyListTNull { get; set; }
        public List<string> TimeOnlyListTNull { get; set; }
#endif
        public List<string> GuidListTNull { get; set; }

        public List<string> BooleanListTNullable { get; set; }
        public List<string> ByteListTNullable { get; set; }
        public List<string> SByteListTNullable { get; set; }
        public List<string> Int16ListTNullable { get; set; }
        public List<string> UInt16ListTNullable { get; set; }
        public List<string> Int32ListTNullable { get; set; }
        public List<string> UInt32ListTNullable { get; set; }
        public List<string> Int64ListTNullable { get; set; }
        public List<string> UInt64ListTNullable { get; set; }
        public List<string> SingleListTNullable { get; set; }
        public List<string> DoubleListTNullable { get; set; }
        public List<string> DecimalListTNullable { get; set; }
        public List<string> CharListTNullable { get; set; }
        public List<string> DateTimeListTNullable { get; set; }
        public List<string> DateTimeOffsetListTNullable { get; set; }
        public List<string> TimeSpanListTNullable { get; set; }
#if NET6_0_OR_GREATER
        public List<string> DateOnlyListTNullable { get; set; }
        public List<string> TimeOnlyListTNullable { get; set; }
#endif
        public List<string> GuidListTNullable { get; set; }

        public List<string> BooleanListTNullableEmpty { get; set; }
        public List<string> ByteListTNullableEmpty { get; set; }
        public List<string> SByteListTNullableEmpty { get; set; }
        public List<string> Int16ListTNullableEmpty { get; set; }
        public List<string> UInt16ListTNullableEmpty { get; set; }
        public List<string> Int32ListTNullableEmpty { get; set; }
        public List<string> UInt32ListTNullableEmpty { get; set; }
        public List<string> Int64ListTNullableEmpty { get; set; }
        public List<string> UInt64ListTNullableEmpty { get; set; }
        public List<string> SingleListTNullableEmpty { get; set; }
        public List<string> DoubleListTNullableEmpty { get; set; }
        public List<string> DecimalListTNullableEmpty { get; set; }
        public List<string> CharListTNullableEmpty { get; set; }
        public List<string> DateTimeListTNullableEmpty { get; set; }
        public List<string> DateTimeOffsetListTNullableEmpty { get; set; }
        public List<string> TimeSpanListTNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public List<string> DateOnlyListTNullableEmpty { get; set; }
        public List<string> TimeOnlyListTNullableEmpty { get; set; }
#endif
        public List<string> GuidListTNullableEmpty { get; set; }

        public List<string> BooleanListTNullableNull { get; set; }
        public List<string> ByteListTNullableNull { get; set; }
        public List<string> SByteListTNullableNull { get; set; }
        public List<string> Int16ListTNullableNull { get; set; }
        public List<string> UInt16ListTNullableNull { get; set; }
        public List<string> Int32ListTNullableNull { get; set; }
        public List<string> UInt32ListTNullableNull { get; set; }
        public List<string> Int64ListTNullableNull { get; set; }
        public List<string> UInt64ListTNullableNull { get; set; }
        public List<string> SingleListTNullableNull { get; set; }
        public List<string> DoubleListTNullableNull { get; set; }
        public List<string> DecimalListTNullableNull { get; set; }
        public List<string> CharListTNullableNull { get; set; }
        public List<string> DateTimeListTNullableNull { get; set; }
        public List<string> DateTimeOffsetListTNullableNull { get; set; }
        public List<string> TimeSpanListTNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public List<string> DateOnlyListTNullableNull { get; set; }
        public List<string> TimeOnlyListTNullableNull { get; set; }
#endif
        public List<string> GuidListTNullableNull { get; set; }

        public List<string> StringListT { get; set; }
        public List<string> StringListTEmpty { get; set; }
        public List<string> StringListTNull { get; set; }

        public List<string> EnumListT { get; set; }
        public List<string> EnumListTEmpty { get; set; }
        public List<string> EnumListTNull { get; set; }

        public List<string> EnumListTNullable { get; set; }
        public List<string> EnumListTNullableEmpty { get; set; }
        public List<string> EnumListTNullableNull { get; set; }

        public IList<string> BooleanIListT { get; set; }
        public IList<string> ByteIListT { get; set; }
        public IList<string> SByteIListT { get; set; }
        public IList<string> Int16IListT { get; set; }
        public IList<string> UInt16IListT { get; set; }
        public IList<string> Int32IListT { get; set; }
        public IList<string> UInt32IListT { get; set; }
        public IList<string> Int64IListT { get; set; }
        public IList<string> UInt64IListT { get; set; }
        public IList<string> SingleIListT { get; set; }
        public IList<string> DoubleIListT { get; set; }
        public IList<string> DecimalIListT { get; set; }
        public IList<string> CharIListT { get; set; }
        public IList<string> DateTimeIListT { get; set; }
        public IList<string> DateTimeOffsetIListT { get; set; }
        public IList<string> TimeSpanIListT { get; set; }
#if NET6_0_OR_GREATER
        public IList<string> DateOnlyIListT { get; set; }
        public IList<string> TimeOnlyIListT { get; set; }
#endif
        public IList<string> GuidIListT { get; set; }

        public IList<string> BooleanIListTEmpty { get; set; }
        public IList<string> ByteIListTEmpty { get; set; }
        public IList<string> SByteIListTEmpty { get; set; }
        public IList<string> Int16IListTEmpty { get; set; }
        public IList<string> UInt16IListTEmpty { get; set; }
        public IList<string> Int32IListTEmpty { get; set; }
        public IList<string> UInt32IListTEmpty { get; set; }
        public IList<string> Int64IListTEmpty { get; set; }
        public IList<string> UInt64IListTEmpty { get; set; }
        public IList<string> SingleIListTEmpty { get; set; }
        public IList<string> DoubleIListTEmpty { get; set; }
        public IList<string> DecimalIListTEmpty { get; set; }
        public IList<string> CharIListTEmpty { get; set; }
        public IList<string> DateTimeIListTEmpty { get; set; }
        public IList<string> DateTimeOffsetIListTEmpty { get; set; }
        public IList<string> TimeSpanIListTEmpty { get; set; }
#if NET6_0_OR_GREATER
        public IList<string> DateOnlyIListTEmpty { get; set; }
        public IList<string> TimeOnlyIListTEmpty { get; set; }
#endif
        public IList<string> GuidIListTEmpty { get; set; }

        public IList<string> BooleanIListTNull { get; set; }
        public IList<string> ByteIListTNull { get; set; }
        public IList<string> SByteIListTNull { get; set; }
        public IList<string> Int16IListTNull { get; set; }
        public IList<string> UInt16IListTNull { get; set; }
        public IList<string> Int32IListTNull { get; set; }
        public IList<string> UInt32IListTNull { get; set; }
        public IList<string> Int64IListTNull { get; set; }
        public IList<string> UInt64IListTNull { get; set; }
        public IList<string> SingleIListTNull { get; set; }
        public IList<string> DoubleIListTNull { get; set; }
        public IList<string> DecimalIListTNull { get; set; }
        public IList<string> CharIListTNull { get; set; }
        public IList<string> DateTimeIListTNull { get; set; }
        public IList<string> DateTimeOffsetIListTNull { get; set; }
        public IList<string> TimeSpanIListTNull { get; set; }
#if NET6_0_OR_GREATER
        public IList<string> DateOnlyIListTNull { get; set; }
        public IList<string> TimeOnlyIListTNull { get; set; }
#endif
        public IList<string> GuidIListTNull { get; set; }

        public IList<string> BooleanIListTNullable { get; set; }
        public IList<string> ByteIListTNullable { get; set; }
        public IList<string> SByteIListTNullable { get; set; }
        public IList<string> Int16IListTNullable { get; set; }
        public IList<string> UInt16IListTNullable { get; set; }
        public IList<string> Int32IListTNullable { get; set; }
        public IList<string> UInt32IListTNullable { get; set; }
        public IList<string> Int64IListTNullable { get; set; }
        public IList<string> UInt64IListTNullable { get; set; }
        public IList<string> SingleIListTNullable { get; set; }
        public IList<string> DoubleIListTNullable { get; set; }
        public IList<string> DecimalIListTNullable { get; set; }
        public IList<string> CharIListTNullable { get; set; }
        public IList<string> DateTimeIListTNullable { get; set; }
        public IList<string> DateTimeOffsetIListTNullable { get; set; }
        public IList<string> TimeSpanIListTNullable { get; set; }
#if NET6_0_OR_GREATER
        public IList<string> DateOnlyIListTNullable { get; set; }
        public IList<string> TimeOnlyIListTNullable { get; set; }
#endif
        public IList<string> GuidIListTNullable { get; set; }

        public IList<string> BooleanIListTNullableEmpty { get; set; }
        public IList<string> ByteIListTNullableEmpty { get; set; }
        public IList<string> SByteIListTNullableEmpty { get; set; }
        public IList<string> Int16IListTNullableEmpty { get; set; }
        public IList<string> UInt16IListTNullableEmpty { get; set; }
        public IList<string> Int32IListTNullableEmpty { get; set; }
        public IList<string> UInt32IListTNullableEmpty { get; set; }
        public IList<string> Int64IListTNullableEmpty { get; set; }
        public IList<string> UInt64IListTNullableEmpty { get; set; }
        public IList<string> SingleIListTNullableEmpty { get; set; }
        public IList<string> DoubleIListTNullableEmpty { get; set; }
        public IList<string> DecimalIListTNullableEmpty { get; set; }
        public IList<string> CharIListTNullableEmpty { get; set; }
        public IList<string> DateTimeIListTNullableEmpty { get; set; }
        public IList<string> DateTimeOffsetIListTNullableEmpty { get; set; }
        public IList<string> TimeSpanIListTNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public IList<string> DateOnlyIListTNullableEmpty { get; set; }
        public IList<string> TimeOnlyIListTNullableEmpty { get; set; }
#endif
        public IList<string> GuidIListTNullableEmpty { get; set; }

        public IList<string> BooleanIListTNullableNull { get; set; }
        public IList<string> ByteIListTNullableNull { get; set; }
        public IList<string> SByteIListTNullableNull { get; set; }
        public IList<string> Int16IListTNullableNull { get; set; }
        public IList<string> UInt16IListTNullableNull { get; set; }
        public IList<string> Int32IListTNullableNull { get; set; }
        public IList<string> UInt32IListTNullableNull { get; set; }
        public IList<string> Int64IListTNullableNull { get; set; }
        public IList<string> UInt64IListTNullableNull { get; set; }
        public IList<string> SingleIListTNullableNull { get; set; }
        public IList<string> DoubleIListTNullableNull { get; set; }
        public IList<string> DecimalIListTNullableNull { get; set; }
        public IList<string> CharIListTNullableNull { get; set; }
        public IList<string> DateTimeIListTNullableNull { get; set; }
        public IList<string> DateTimeOffsetIListTNullableNull { get; set; }
        public IList<string> TimeSpanIListTNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public IList<string> DateOnlyIListTNullableNull { get; set; }
        public IList<string> TimeOnlyIListTNullableNull { get; set; }
#endif
        public IList<string> GuidIListTNullableNull { get; set; }

        public IList<string> StringIListT { get; set; }
        public IList<string> StringIListTEmpty { get; set; }
        public IList<string> StringIListTNull { get; set; }

        public IList<string> EnumIListT { get; set; }
        public IList<string> EnumIListTEmpty { get; set; }
        public IList<string> EnumIListTNull { get; set; }

        public IList<string> EnumIListTNullable { get; set; }
        public IList<string> EnumIListTNullableEmpty { get; set; }
        public IList<string> EnumIListTNullableNull { get; set; }

        public IReadOnlyList<string> BooleanIReadOnlyListT { get; set; }
        public IReadOnlyList<string> ByteIReadOnlyListT { get; set; }
        public IReadOnlyList<string> SByteIReadOnlyListT { get; set; }
        public IReadOnlyList<string> Int16IReadOnlyListT { get; set; }
        public IReadOnlyList<string> UInt16IReadOnlyListT { get; set; }
        public IReadOnlyList<string> Int32IReadOnlyListT { get; set; }
        public IReadOnlyList<string> UInt32IReadOnlyListT { get; set; }
        public IReadOnlyList<string> Int64IReadOnlyListT { get; set; }
        public IReadOnlyList<string> UInt64IReadOnlyListT { get; set; }
        public IReadOnlyList<string> SingleIReadOnlyListT { get; set; }
        public IReadOnlyList<string> DoubleIReadOnlyListT { get; set; }
        public IReadOnlyList<string> DecimalIReadOnlyListT { get; set; }
        public IReadOnlyList<string> CharIReadOnlyListT { get; set; }
        public IReadOnlyList<string> DateTimeIReadOnlyListT { get; set; }
        public IReadOnlyList<string> DateTimeOffsetIReadOnlyListT { get; set; }
        public IReadOnlyList<string> TimeSpanIReadOnlyListT { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyList<string> DateOnlyIReadOnlyListT { get; set; }
        public IReadOnlyList<string> TimeOnlyIReadOnlyListT { get; set; }
#endif
        public IReadOnlyList<string> GuidIReadOnlyListT { get; set; }

        public IReadOnlyList<string> BooleanIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<string> ByteIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<string> SByteIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<string> Int16IReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<string> UInt16IReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<string> Int32IReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<string> UInt32IReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<string> Int64IReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<string> UInt64IReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<string> SingleIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<string> DoubleIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<string> DecimalIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<string> CharIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<string> DateTimeIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<string> DateTimeOffsetIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<string> TimeSpanIReadOnlyListTEmpty { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyList<string> DateOnlyIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<string> TimeOnlyIReadOnlyListTEmpty { get; set; }
#endif
        public IReadOnlyList<string> GuidIReadOnlyListTEmpty { get; set; }

        public IReadOnlyList<string> BooleanIReadOnlyListTNull { get; set; }
        public IReadOnlyList<string> ByteIReadOnlyListTNull { get; set; }
        public IReadOnlyList<string> SByteIReadOnlyListTNull { get; set; }
        public IReadOnlyList<string> Int16IReadOnlyListTNull { get; set; }
        public IReadOnlyList<string> UInt16IReadOnlyListTNull { get; set; }
        public IReadOnlyList<string> Int32IReadOnlyListTNull { get; set; }
        public IReadOnlyList<string> UInt32IReadOnlyListTNull { get; set; }
        public IReadOnlyList<string> Int64IReadOnlyListTNull { get; set; }
        public IReadOnlyList<string> UInt64IReadOnlyListTNull { get; set; }
        public IReadOnlyList<string> SingleIReadOnlyListTNull { get; set; }
        public IReadOnlyList<string> DoubleIReadOnlyListTNull { get; set; }
        public IReadOnlyList<string> DecimalIReadOnlyListTNull { get; set; }
        public IReadOnlyList<string> CharIReadOnlyListTNull { get; set; }
        public IReadOnlyList<string> DateTimeIReadOnlyListTNull { get; set; }
        public IReadOnlyList<string> DateTimeOffsetIReadOnlyListTNull { get; set; }
        public IReadOnlyList<string> TimeSpanIReadOnlyListTNull { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyList<string> DateOnlyIReadOnlyListTNull { get; set; }
        public IReadOnlyList<string> TimeOnlyIReadOnlyListTNull { get; set; }
#endif
        public IReadOnlyList<string> GuidIReadOnlyListTNull { get; set; }

        public IReadOnlyList<string> BooleanIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<string> ByteIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<string> SByteIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<string> Int16IReadOnlyListTNullable { get; set; }
        public IReadOnlyList<string> UInt16IReadOnlyListTNullable { get; set; }
        public IReadOnlyList<string> Int32IReadOnlyListTNullable { get; set; }
        public IReadOnlyList<string> UInt32IReadOnlyListTNullable { get; set; }
        public IReadOnlyList<string> Int64IReadOnlyListTNullable { get; set; }
        public IReadOnlyList<string> UInt64IReadOnlyListTNullable { get; set; }
        public IReadOnlyList<string> SingleIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<string> DoubleIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<string> DecimalIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<string> CharIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<string> DateTimeIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<string> DateTimeOffsetIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<string> TimeSpanIReadOnlyListTNullable { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyList<string> DateOnlyIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<string> TimeOnlyIReadOnlyListTNullable { get; set; }
#endif
        public IReadOnlyList<string> GuidIReadOnlyListTNullable { get; set; }

        public IReadOnlyList<string> BooleanIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<string> ByteIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<string> SByteIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<string> Int16IReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<string> UInt16IReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<string> Int32IReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<string> UInt32IReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<string> Int64IReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<string> UInt64IReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<string> SingleIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<string> DoubleIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<string> DecimalIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<string> CharIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<string> DateTimeIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<string> DateTimeOffsetIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<string> TimeSpanIReadOnlyListTNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyList<string> DateOnlyIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<string> TimeOnlyIReadOnlyListTNullableEmpty { get; set; }
#endif
        public IReadOnlyList<string> GuidIReadOnlyListTNullableEmpty { get; set; }

        public IReadOnlyList<string> BooleanIReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<string> ByteIReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<string> SByteIReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<string> Int16IReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<string> UInt16IReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<string> Int32IReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<string> UInt32IReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<string> Int64IReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<string> UInt64IReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<string> SingleIReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<string> DoubleIReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<string> DecimalIReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<string> CharIReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<string> DateTimeIReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<string> DateTimeOffsetIReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<string> TimeSpanIReadOnlyListTNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyList<string> DateOnlyIReadOnlyListTNullableNull { get; set; }
        public IReadOnlyList<string> TimeOnlyIReadOnlyListTNullableNull { get; set; }
#endif
        public IReadOnlyList<string> GuidIReadOnlyListTNullableNull { get; set; }

        public IReadOnlyList<string> StringIReadOnlyListT { get; set; }
        public IReadOnlyList<string> StringIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<string> StringIReadOnlyListTNull { get; set; }

        public IReadOnlyList<string> EnumIReadOnlyListT { get; set; }
        public IReadOnlyList<string> EnumIReadOnlyListTEmpty { get; set; }
        public IReadOnlyList<string> EnumIReadOnlyListTNull { get; set; }

        public IReadOnlyList<string> EnumIReadOnlyListTNullable { get; set; }
        public IReadOnlyList<string> EnumIReadOnlyListTNullableEmpty { get; set; }
        public IReadOnlyList<string> EnumIReadOnlyListTNullableNull { get; set; }

        public IList BooleanIReadOnlyList { get; set; }
        public IList ByteIReadOnlyList { get; set; }
        public IList SByteIReadOnlyList { get; set; }
        public IList Int16IReadOnlyList { get; set; }
        public IList UInt16IReadOnlyList { get; set; }
        public IList Int32IReadOnlyList { get; set; }
        public IList UInt32IReadOnlyList { get; set; }
        public IList Int64IReadOnlyList { get; set; }
        public IList UInt64IReadOnlyList { get; set; }
        public IList SingleIReadOnlyList { get; set; }
        public IList DoubleIReadOnlyList { get; set; }
        public IList DecimalIReadOnlyList { get; set; }
        public IList CharIReadOnlyList { get; set; }
        public IList DateTimeIReadOnlyList { get; set; }
        public IList DateTimeOffsetIReadOnlyList { get; set; }
        public IList TimeSpanIReadOnlyList { get; set; }
#if NET6_0_OR_GREATER
        public IList DateOnlyIReadOnlyList { get; set; }
        public IList TimeOnlyIReadOnlyList { get; set; }
#endif
        public IList GuidIReadOnlyList { get; set; }

        public IList BooleanIReadOnlyListEmpty { get; set; }
        public IList ByteIReadOnlyListEmpty { get; set; }
        public IList SByteIReadOnlyListEmpty { get; set; }
        public IList Int16IReadOnlyListEmpty { get; set; }
        public IList UInt16IReadOnlyListEmpty { get; set; }
        public IList Int32IReadOnlyListEmpty { get; set; }
        public IList UInt32IReadOnlyListEmpty { get; set; }
        public IList Int64IReadOnlyListEmpty { get; set; }
        public IList UInt64IReadOnlyListEmpty { get; set; }
        public IList SingleIReadOnlyListEmpty { get; set; }
        public IList DoubleIReadOnlyListEmpty { get; set; }
        public IList DecimalIReadOnlyListEmpty { get; set; }
        public IList CharIReadOnlyListEmpty { get; set; }
        public IList DateTimeIReadOnlyListEmpty { get; set; }
        public IList DateTimeOffsetIReadOnlyListEmpty { get; set; }
        public IList TimeSpanIReadOnlyListEmpty { get; set; }
#if NET6_0_OR_GREATER
        public IList DateOnlyIReadOnlyListEmpty { get; set; }
        public IList TimeOnlyIReadOnlyListEmpty { get; set; }
#endif
        public IList GuidIReadOnlyListEmpty { get; set; }

        public IList BooleanIReadOnlyListNull { get; set; }
        public IList ByteIReadOnlyListNull { get; set; }
        public IList SByteIReadOnlyListNull { get; set; }
        public IList Int16IReadOnlyListNull { get; set; }
        public IList UInt16IReadOnlyListNull { get; set; }
        public IList Int32IReadOnlyListNull { get; set; }
        public IList UInt32IReadOnlyListNull { get; set; }
        public IList Int64IReadOnlyListNull { get; set; }
        public IList UInt64IReadOnlyListNull { get; set; }
        public IList SingleIReadOnlyListNull { get; set; }
        public IList DoubleIReadOnlyListNull { get; set; }
        public IList DecimalIReadOnlyListNull { get; set; }
        public IList CharIReadOnlyListNull { get; set; }
        public IList DateTimeIReadOnlyListNull { get; set; }
        public IList DateTimeOffsetIReadOnlyListNull { get; set; }
        public IList TimeSpanIReadOnlyListNull { get; set; }
#if NET6_0_OR_GREATER
        public IList DateOnlyIReadOnlyListNull { get; set; }
        public IList TimeOnlyIReadOnlyListNull { get; set; }
#endif
        public IList GuidIReadOnlyListNull { get; set; }

        public IList BooleanIReadOnlyListNullable { get; set; }
        public IList ByteIReadOnlyListNullable { get; set; }
        public IList SByteIReadOnlyListNullable { get; set; }
        public IList Int16IReadOnlyListNullable { get; set; }
        public IList UInt16IReadOnlyListNullable { get; set; }
        public IList Int32IReadOnlyListNullable { get; set; }
        public IList UInt32IReadOnlyListNullable { get; set; }
        public IList Int64IReadOnlyListNullable { get; set; }
        public IList UInt64IReadOnlyListNullable { get; set; }
        public IList SingleIReadOnlyListNullable { get; set; }
        public IList DoubleIReadOnlyListNullable { get; set; }
        public IList DecimalIReadOnlyListNullable { get; set; }
        public IList CharIReadOnlyListNullable { get; set; }
        public IList DateTimeIReadOnlyListNullable { get; set; }
        public IList DateTimeOffsetIReadOnlyListNullable { get; set; }
        public IList TimeSpanIReadOnlyListNullable { get; set; }
#if NET6_0_OR_GREATER
        public IList DateOnlyIReadOnlyListNullable { get; set; }
        public IList TimeOnlyIReadOnlyListNullable { get; set; }
#endif
        public IList GuidIReadOnlyListNullable { get; set; }

        public IList BooleanIReadOnlyListNullableEmpty { get; set; }
        public IList ByteIReadOnlyListNullableEmpty { get; set; }
        public IList SByteIReadOnlyListNullableEmpty { get; set; }
        public IList Int16IReadOnlyListNullableEmpty { get; set; }
        public IList UInt16IReadOnlyListNullableEmpty { get; set; }
        public IList Int32IReadOnlyListNullableEmpty { get; set; }
        public IList UInt32IReadOnlyListNullableEmpty { get; set; }
        public IList Int64IReadOnlyListNullableEmpty { get; set; }
        public IList UInt64IReadOnlyListNullableEmpty { get; set; }
        public IList SingleIReadOnlyListNullableEmpty { get; set; }
        public IList DoubleIReadOnlyListNullableEmpty { get; set; }
        public IList DecimalIReadOnlyListNullableEmpty { get; set; }
        public IList CharIReadOnlyListNullableEmpty { get; set; }
        public IList DateTimeIReadOnlyListNullableEmpty { get; set; }
        public IList DateTimeOffsetIReadOnlyListNullableEmpty { get; set; }
        public IList TimeSpanIReadOnlyListNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public IList DateOnlyIReadOnlyListNullableEmpty { get; set; }
        public IList TimeOnlyIReadOnlyListNullableEmpty { get; set; }
#endif
        public IList GuidIReadOnlyListNullableEmpty { get; set; }

        public IList BooleanIReadOnlyListNullableNull { get; set; }
        public IList ByteIReadOnlyListNullableNull { get; set; }
        public IList SByteIReadOnlyListNullableNull { get; set; }
        public IList Int16IReadOnlyListNullableNull { get; set; }
        public IList UInt16IReadOnlyListNullableNull { get; set; }
        public IList Int32IReadOnlyListNullableNull { get; set; }
        public IList UInt32IReadOnlyListNullableNull { get; set; }
        public IList Int64IReadOnlyListNullableNull { get; set; }
        public IList UInt64IReadOnlyListNullableNull { get; set; }
        public IList SingleIReadOnlyListNullableNull { get; set; }
        public IList DoubleIReadOnlyListNullableNull { get; set; }
        public IList DecimalIReadOnlyListNullableNull { get; set; }
        public IList CharIReadOnlyListNullableNull { get; set; }
        public IList DateTimeIReadOnlyListNullableNull { get; set; }
        public IList DateTimeOffsetIReadOnlyListNullableNull { get; set; }
        public IList TimeSpanIReadOnlyListNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public IList DateOnlyIReadOnlyListNullableNull { get; set; }
        public IList TimeOnlyIReadOnlyListNullableNull { get; set; }
#endif
        public IList GuidIReadOnlyListNullableNull { get; set; }

        public IList StringIReadOnlyList { get; set; }
        public IList StringIReadOnlyListEmpty { get; set; }
        public IList StringIReadOnlyListNull { get; set; }

        public IList EnumIReadOnlyList { get; set; }
        public IList EnumIReadOnlyListEmpty { get; set; }
        public IList EnumIReadOnlyListNull { get; set; }

        public IList EnumIReadOnlyListNullable { get; set; }
        public IList EnumIReadOnlyListNullableEmpty { get; set; }
        public IList EnumIReadOnlyListNullableNull { get; set; }

        public ICollection<string> BooleanICollectionT { get; set; }
        public ICollection<string> ByteICollectionT { get; set; }
        public ICollection<string> SByteICollectionT { get; set; }
        public ICollection<string> Int16ICollectionT { get; set; }
        public ICollection<string> UInt16ICollectionT { get; set; }
        public ICollection<string> Int32ICollectionT { get; set; }
        public ICollection<string> UInt32ICollectionT { get; set; }
        public ICollection<string> Int64ICollectionT { get; set; }
        public ICollection<string> UInt64ICollectionT { get; set; }
        public ICollection<string> SingleICollectionT { get; set; }
        public ICollection<string> DoubleICollectionT { get; set; }
        public ICollection<string> DecimalICollectionT { get; set; }
        public ICollection<string> CharICollectionT { get; set; }
        public ICollection<string> DateTimeICollectionT { get; set; }
        public ICollection<string> DateTimeOffsetICollectionT { get; set; }
        public ICollection<string> TimeSpanICollectionT { get; set; }
#if NET6_0_OR_GREATER
        public ICollection<string> DateOnlyICollectionT { get; set; }
        public ICollection<string> TimeOnlyICollectionT { get; set; }
#endif
        public ICollection<string> GuidICollectionT { get; set; }

        public ICollection<string> BooleanICollectionTEmpty { get; set; }
        public ICollection<string> ByteICollectionTEmpty { get; set; }
        public ICollection<string> SByteICollectionTEmpty { get; set; }
        public ICollection<string> Int16ICollectionTEmpty { get; set; }
        public ICollection<string> UInt16ICollectionTEmpty { get; set; }
        public ICollection<string> Int32ICollectionTEmpty { get; set; }
        public ICollection<string> UInt32ICollectionTEmpty { get; set; }
        public ICollection<string> Int64ICollectionTEmpty { get; set; }
        public ICollection<string> UInt64ICollectionTEmpty { get; set; }
        public ICollection<string> SingleICollectionTEmpty { get; set; }
        public ICollection<string> DoubleICollectionTEmpty { get; set; }
        public ICollection<string> DecimalICollectionTEmpty { get; set; }
        public ICollection<string> CharICollectionTEmpty { get; set; }
        public ICollection<string> DateTimeICollectionTEmpty { get; set; }
        public ICollection<string> DateTimeOffsetICollectionTEmpty { get; set; }
        public ICollection<string> TimeSpanICollectionTEmpty { get; set; }
#if NET6_0_OR_GREATER
        public ICollection<string> DateOnlyICollectionTEmpty { get; set; }
        public ICollection<string> TimeOnlyICollectionTEmpty { get; set; }
#endif
        public ICollection<string> GuidICollectionTEmpty { get; set; }

        public ICollection<string> BooleanICollectionTNull { get; set; }
        public ICollection<string> ByteICollectionTNull { get; set; }
        public ICollection<string> SByteICollectionTNull { get; set; }
        public ICollection<string> Int16ICollectionTNull { get; set; }
        public ICollection<string> UInt16ICollectionTNull { get; set; }
        public ICollection<string> Int32ICollectionTNull { get; set; }
        public ICollection<string> UInt32ICollectionTNull { get; set; }
        public ICollection<string> Int64ICollectionTNull { get; set; }
        public ICollection<string> UInt64ICollectionTNull { get; set; }
        public ICollection<string> SingleICollectionTNull { get; set; }
        public ICollection<string> DoubleICollectionTNull { get; set; }
        public ICollection<string> DecimalICollectionTNull { get; set; }
        public ICollection<string> CharICollectionTNull { get; set; }
        public ICollection<string> DateTimeICollectionTNull { get; set; }
        public ICollection<string> DateTimeOffsetICollectionTNull { get; set; }
        public ICollection<string> TimeSpanICollectionTNull { get; set; }
#if NET6_0_OR_GREATER
        public ICollection<string> DateOnlyICollectionTNull { get; set; }
        public ICollection<string> TimeOnlyICollectionTNull { get; set; }
#endif
        public ICollection<string> GuidICollectionTNull { get; set; }

        public ICollection<string> BooleanICollectionTNullable { get; set; }
        public ICollection<string> ByteICollectionTNullable { get; set; }
        public ICollection<string> SByteICollectionTNullable { get; set; }
        public ICollection<string> Int16ICollectionTNullable { get; set; }
        public ICollection<string> UInt16ICollectionTNullable { get; set; }
        public ICollection<string> Int32ICollectionTNullable { get; set; }
        public ICollection<string> UInt32ICollectionTNullable { get; set; }
        public ICollection<string> Int64ICollectionTNullable { get; set; }
        public ICollection<string> UInt64ICollectionTNullable { get; set; }
        public ICollection<string> SingleICollectionTNullable { get; set; }
        public ICollection<string> DoubleICollectionTNullable { get; set; }
        public ICollection<string> DecimalICollectionTNullable { get; set; }
        public ICollection<string> CharICollectionTNullable { get; set; }
        public ICollection<string> DateTimeICollectionTNullable { get; set; }
        public ICollection<string> DateTimeOffsetICollectionTNullable { get; set; }
        public ICollection<string> TimeSpanICollectionTNullable { get; set; }
#if NET6_0_OR_GREATER
        public ICollection<string> DateOnlyICollectionTNullable { get; set; }
        public ICollection<string> TimeOnlyICollectionTNullable { get; set; }
#endif
        public ICollection<string> GuidICollectionTNullable { get; set; }

        public ICollection<string> BooleanICollectionTNullableEmpty { get; set; }
        public ICollection<string> ByteICollectionTNullableEmpty { get; set; }
        public ICollection<string> SByteICollectionTNullableEmpty { get; set; }
        public ICollection<string> Int16ICollectionTNullableEmpty { get; set; }
        public ICollection<string> UInt16ICollectionTNullableEmpty { get; set; }
        public ICollection<string> Int32ICollectionTNullableEmpty { get; set; }
        public ICollection<string> UInt32ICollectionTNullableEmpty { get; set; }
        public ICollection<string> Int64ICollectionTNullableEmpty { get; set; }
        public ICollection<string> UInt64ICollectionTNullableEmpty { get; set; }
        public ICollection<string> SingleICollectionTNullableEmpty { get; set; }
        public ICollection<string> DoubleICollectionTNullableEmpty { get; set; }
        public ICollection<string> DecimalICollectionTNullableEmpty { get; set; }
        public ICollection<string> CharICollectionTNullableEmpty { get; set; }
        public ICollection<string> DateTimeICollectionTNullableEmpty { get; set; }
        public ICollection<string> DateTimeOffsetICollectionTNullableEmpty { get; set; }
        public ICollection<string> TimeSpanICollectionTNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public ICollection<string> DateOnlyICollectionTNullableEmpty { get; set; }
        public ICollection<string> TimeOnlyICollectionTNullableEmpty { get; set; }
#endif
        public ICollection<string> GuidICollectionTNullableEmpty { get; set; }

        public ICollection<string> BooleanICollectionTNullableNull { get; set; }
        public ICollection<string> ByteICollectionTNullableNull { get; set; }
        public ICollection<string> SByteICollectionTNullableNull { get; set; }
        public ICollection<string> Int16ICollectionTNullableNull { get; set; }
        public ICollection<string> UInt16ICollectionTNullableNull { get; set; }
        public ICollection<string> Int32ICollectionTNullableNull { get; set; }
        public ICollection<string> UInt32ICollectionTNullableNull { get; set; }
        public ICollection<string> Int64ICollectionTNullableNull { get; set; }
        public ICollection<string> UInt64ICollectionTNullableNull { get; set; }
        public ICollection<string> SingleICollectionTNullableNull { get; set; }
        public ICollection<string> DoubleICollectionTNullableNull { get; set; }
        public ICollection<string> DecimalICollectionTNullableNull { get; set; }
        public ICollection<string> CharICollectionTNullableNull { get; set; }
        public ICollection<string> DateTimeICollectionTNullableNull { get; set; }
        public ICollection<string> DateTimeOffsetICollectionTNullableNull { get; set; }
        public ICollection<string> TimeSpanICollectionTNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public ICollection<string> DateOnlyICollectionTNullableNull { get; set; }
        public ICollection<string> TimeOnlyICollectionTNullableNull { get; set; }
#endif
        public ICollection<string> GuidICollectionTNullableNull { get; set; }

        public ICollection<string> StringICollectionT { get; set; }
        public ICollection<string> StringICollectionTEmpty { get; set; }
        public ICollection<string> StringICollectionTNull { get; set; }

        public ICollection<string> EnumICollectionT { get; set; }
        public ICollection<string> EnumICollectionTEmpty { get; set; }
        public ICollection<string> EnumICollectionTNull { get; set; }

        public ICollection<string> EnumICollectionTNullable { get; set; }
        public ICollection<string> EnumICollectionTNullableEmpty { get; set; }
        public ICollection<string> EnumICollectionTNullableNull { get; set; }

        public IReadOnlyCollection<string> BooleanIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<string> ByteIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<string> SByteIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<string> Int16IReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<string> UInt16IReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<string> Int32IReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<string> UInt32IReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<string> Int64IReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<string> UInt64IReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<string> SingleIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<string> DoubleIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<string> DecimalIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<string> CharIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<string> DateTimeIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<string> DateTimeOffsetIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<string> TimeSpanIReadOnlyCollectionT { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyCollection<string> DateOnlyIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<string> TimeOnlyIReadOnlyCollectionT { get; set; }
#endif
        public IReadOnlyCollection<string> GuidIReadOnlyCollectionT { get; set; }

        public IReadOnlyCollection<string> BooleanIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<string> ByteIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<string> SByteIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<string> Int16IReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<string> UInt16IReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<string> Int32IReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<string> UInt32IReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<string> Int64IReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<string> UInt64IReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<string> SingleIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<string> DoubleIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<string> DecimalIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<string> CharIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<string> DateTimeIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<string> DateTimeOffsetIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<string> TimeSpanIReadOnlyCollectionTEmpty { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyCollection<string> DateOnlyIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<string> TimeOnlyIReadOnlyCollectionTEmpty { get; set; }
#endif
        public IReadOnlyCollection<string> GuidIReadOnlyCollectionTEmpty { get; set; }

        public IReadOnlyCollection<string> BooleanIReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<string> ByteIReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<string> SByteIReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<string> Int16IReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<string> UInt16IReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<string> Int32IReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<string> UInt32IReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<string> Int64IReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<string> UInt64IReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<string> SingleIReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<string> DoubleIReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<string> DecimalIReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<string> CharIReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<string> DateTimeIReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<string> DateTimeOffsetIReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<string> TimeSpanIReadOnlyCollectionTNull { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyCollection<string> DateOnlyIReadOnlyCollectionTNull { get; set; }
        public IReadOnlyCollection<string> TimeOnlyIReadOnlyCollectionTNull { get; set; }
#endif
        public IReadOnlyCollection<string> GuidIReadOnlyCollectionTNull { get; set; }

        public IReadOnlyCollection<string> BooleanIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<string> ByteIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<string> SByteIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<string> Int16IReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<string> UInt16IReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<string> Int32IReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<string> UInt32IReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<string> Int64IReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<string> UInt64IReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<string> SingleIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<string> DoubleIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<string> DecimalIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<string> CharIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<string> DateTimeIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<string> DateTimeOffsetIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<string> TimeSpanIReadOnlyCollectionTNullable { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyCollection<string> DateOnlyIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<string> TimeOnlyIReadOnlyCollectionTNullable { get; set; }
#endif
        public IReadOnlyCollection<string> GuidIReadOnlyCollectionTNullable { get; set; }

        public IReadOnlyCollection<string> BooleanIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<string> ByteIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<string> SByteIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<string> Int16IReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<string> UInt16IReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<string> Int32IReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<string> UInt32IReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<string> Int64IReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<string> UInt64IReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<string> SingleIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<string> DoubleIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<string> DecimalIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<string> CharIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<string> DateTimeIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<string> DateTimeOffsetIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<string> TimeSpanIReadOnlyCollectionTNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyCollection<string> DateOnlyIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<string> TimeOnlyIReadOnlyCollectionTNullableEmpty { get; set; }
#endif
        public IReadOnlyCollection<string> GuidIReadOnlyCollectionTNullableEmpty { get; set; }

        public IReadOnlyCollection<string> BooleanIReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<string> ByteIReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<string> SByteIReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<string> Int16IReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<string> UInt16IReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<string> Int32IReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<string> UInt32IReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<string> Int64IReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<string> UInt64IReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<string> SingleIReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<string> DoubleIReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<string> DecimalIReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<string> CharIReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<string> DateTimeIReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<string> DateTimeOffsetIReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<string> TimeSpanIReadOnlyCollectionTNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlyCollection<string> DateOnlyIReadOnlyCollectionTNullableNull { get; set; }
        public IReadOnlyCollection<string> TimeOnlyIReadOnlyCollectionTNullableNull { get; set; }
#endif
        public IReadOnlyCollection<string> GuidIReadOnlyCollectionTNullableNull { get; set; }

        public IReadOnlyCollection<string> StringIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<string> StringIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<string> StringIReadOnlyCollectionTNull { get; set; }

        public IReadOnlyCollection<string> EnumIReadOnlyCollectionT { get; set; }
        public IReadOnlyCollection<string> EnumIReadOnlyCollectionTEmpty { get; set; }
        public IReadOnlyCollection<string> EnumIReadOnlyCollectionTNull { get; set; }

        public IReadOnlyCollection<string> EnumIReadOnlyCollectionTNullable { get; set; }
        public IReadOnlyCollection<string> EnumIReadOnlyCollectionTNullableEmpty { get; set; }
        public IReadOnlyCollection<string> EnumIReadOnlyCollectionTNullableNull { get; set; }

        public ICollection BooleanICollection { get; set; }
        public ICollection ByteICollection { get; set; }
        public ICollection SByteICollection { get; set; }
        public ICollection Int16ICollection { get; set; }
        public ICollection UInt16ICollection { get; set; }
        public ICollection Int32ICollection { get; set; }
        public ICollection UInt32ICollection { get; set; }
        public ICollection Int64ICollection { get; set; }
        public ICollection UInt64ICollection { get; set; }
        public ICollection SingleICollection { get; set; }
        public ICollection DoubleICollection { get; set; }
        public ICollection DecimalICollection { get; set; }
        public ICollection CharICollection { get; set; }
        public ICollection DateTimeICollection { get; set; }
        public ICollection DateTimeOffsetICollection { get; set; }
        public ICollection TimeSpanICollection { get; set; }
#if NET6_0_OR_GREATER
        public ICollection DateOnlyICollection { get; set; }
        public ICollection TimeOnlyICollection { get; set; }
#endif
        public ICollection GuidICollection { get; set; }

        public ICollection BooleanICollectionEmpty { get; set; }
        public ICollection ByteICollectionEmpty { get; set; }
        public ICollection SByteICollectionEmpty { get; set; }
        public ICollection Int16ICollectionEmpty { get; set; }
        public ICollection UInt16ICollectionEmpty { get; set; }
        public ICollection Int32ICollectionEmpty { get; set; }
        public ICollection UInt32ICollectionEmpty { get; set; }
        public ICollection Int64ICollectionEmpty { get; set; }
        public ICollection UInt64ICollectionEmpty { get; set; }
        public ICollection SingleICollectionEmpty { get; set; }
        public ICollection DoubleICollectionEmpty { get; set; }
        public ICollection DecimalICollectionEmpty { get; set; }
        public ICollection CharICollectionEmpty { get; set; }
        public ICollection DateTimeICollectionEmpty { get; set; }
        public ICollection DateTimeOffsetICollectionEmpty { get; set; }
        public ICollection TimeSpanICollectionEmpty { get; set; }
#if NET6_0_OR_GREATER
        public ICollection DateOnlyICollectionEmpty { get; set; }
        public ICollection TimeOnlyICollectionEmpty { get; set; }
#endif
        public ICollection GuidICollectionEmpty { get; set; }

        public ICollection BooleanICollectionNull { get; set; }
        public ICollection ByteICollectionNull { get; set; }
        public ICollection SByteICollectionNull { get; set; }
        public ICollection Int16ICollectionNull { get; set; }
        public ICollection UInt16ICollectionNull { get; set; }
        public ICollection Int32ICollectionNull { get; set; }
        public ICollection UInt32ICollectionNull { get; set; }
        public ICollection Int64ICollectionNull { get; set; }
        public ICollection UInt64ICollectionNull { get; set; }
        public ICollection SingleICollectionNull { get; set; }
        public ICollection DoubleICollectionNull { get; set; }
        public ICollection DecimalICollectionNull { get; set; }
        public ICollection CharICollectionNull { get; set; }
        public ICollection DateTimeICollectionNull { get; set; }
        public ICollection DateTimeOffsetICollectionNull { get; set; }
        public ICollection TimeSpanICollectionNull { get; set; }
#if NET6_0_OR_GREATER
        public ICollection DateOnlyICollectionNull { get; set; }
        public ICollection TimeOnlyICollectionNull { get; set; }
#endif
        public ICollection GuidICollectionNull { get; set; }

        public ICollection BooleanICollectionNullable { get; set; }
        public ICollection ByteICollectionNullable { get; set; }
        public ICollection SByteICollectionNullable { get; set; }
        public ICollection Int16ICollectionNullable { get; set; }
        public ICollection UInt16ICollectionNullable { get; set; }
        public ICollection Int32ICollectionNullable { get; set; }
        public ICollection UInt32ICollectionNullable { get; set; }
        public ICollection Int64ICollectionNullable { get; set; }
        public ICollection UInt64ICollectionNullable { get; set; }
        public ICollection SingleICollectionNullable { get; set; }
        public ICollection DoubleICollectionNullable { get; set; }
        public ICollection DecimalICollectionNullable { get; set; }
        public ICollection CharICollectionNullable { get; set; }
        public ICollection DateTimeICollectionNullable { get; set; }
        public ICollection DateTimeOffsetICollectionNullable { get; set; }
        public ICollection TimeSpanICollectionNullable { get; set; }
#if NET6_0_OR_GREATER
        public ICollection DateOnlyICollectionNullable { get; set; }
        public ICollection TimeOnlyICollectionNullable { get; set; }
#endif
        public ICollection GuidICollectionNullable { get; set; }

        public ICollection BooleanICollectionNullableEmpty { get; set; }
        public ICollection ByteICollectionNullableEmpty { get; set; }
        public ICollection SByteICollectionNullableEmpty { get; set; }
        public ICollection Int16ICollectionNullableEmpty { get; set; }
        public ICollection UInt16ICollectionNullableEmpty { get; set; }
        public ICollection Int32ICollectionNullableEmpty { get; set; }
        public ICollection UInt32ICollectionNullableEmpty { get; set; }
        public ICollection Int64ICollectionNullableEmpty { get; set; }
        public ICollection UInt64ICollectionNullableEmpty { get; set; }
        public ICollection SingleICollectionNullableEmpty { get; set; }
        public ICollection DoubleICollectionNullableEmpty { get; set; }
        public ICollection DecimalICollectionNullableEmpty { get; set; }
        public ICollection CharICollectionNullableEmpty { get; set; }
        public ICollection DateTimeICollectionNullableEmpty { get; set; }
        public ICollection DateTimeOffsetICollectionNullableEmpty { get; set; }
        public ICollection TimeSpanICollectionNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public ICollection DateOnlyICollectionNullableEmpty { get; set; }
        public ICollection TimeOnlyICollectionNullableEmpty { get; set; }
#endif
        public ICollection GuidICollectionNullableEmpty { get; set; }

        public ICollection BooleanICollectionNullableNull { get; set; }
        public ICollection ByteICollectionNullableNull { get; set; }
        public ICollection SByteICollectionNullableNull { get; set; }
        public ICollection Int16ICollectionNullableNull { get; set; }
        public ICollection UInt16ICollectionNullableNull { get; set; }
        public ICollection Int32ICollectionNullableNull { get; set; }
        public ICollection UInt32ICollectionNullableNull { get; set; }
        public ICollection Int64ICollectionNullableNull { get; set; }
        public ICollection UInt64ICollectionNullableNull { get; set; }
        public ICollection SingleICollectionNullableNull { get; set; }
        public ICollection DoubleICollectionNullableNull { get; set; }
        public ICollection DecimalICollectionNullableNull { get; set; }
        public ICollection CharICollectionNullableNull { get; set; }
        public ICollection DateTimeICollectionNullableNull { get; set; }
        public ICollection DateTimeOffsetICollectionNullableNull { get; set; }
        public ICollection TimeSpanICollectionNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public ICollection DateOnlyICollectionNullableNull { get; set; }
        public ICollection TimeOnlyICollectionNullableNull { get; set; }
#endif
        public ICollection GuidICollectionNullableNull { get; set; }

        public ICollection StringICollection { get; set; }
        public ICollection StringICollectionEmpty { get; set; }
        public ICollection StringICollectionNull { get; set; }

        public ICollection EnumICollection { get; set; }
        public ICollection EnumICollectionEmpty { get; set; }
        public ICollection EnumICollectionNull { get; set; }

        public ICollection EnumICollectionNullable { get; set; }
        public ICollection EnumICollectionNullableEmpty { get; set; }
        public ICollection EnumICollectionNullableNull { get; set; }

        public SimpleModel ClassThing { get; set; }
        public SimpleModel ClassThingNull { get; set; }

        public SimpleModel[] ClassArray { get; set; }
        public SimpleModel[] ClassArrayEmpty { get; set; }
        public SimpleModel[] ClassArrayNull { get; set; }

        public IEnumerable<SimpleModel> ClassEnumerable { get; set; }

        public List<SimpleModel> ClassList { get; set; }
        public List<SimpleModel> ClassListEmpty { get; set; }
        public List<SimpleModel> ClassListNull { get; set; }

        public IList<SimpleModel> ClassIList { get; set; }
        public IList<SimpleModel> ClassIListEmpty { get; set; }
        public IList<SimpleModel> ClassIListNull { get; set; }

        public IReadOnlyList<SimpleModel> ClassIReadOnlyList { get; set; }
        public IReadOnlyList<SimpleModel> ClassIReadOnlyListEmpty { get; set; }
        public IReadOnlyList<SimpleModel> ClassIReadOnlyListNull { get; set; }

        public ICollection<SimpleModel> ClassICollection { get; set; }
        public ICollection<SimpleModel> ClassICollectionEmpty { get; set; }
        public ICollection<SimpleModel> ClassICollectionNull { get; set; }

        public IReadOnlyCollection<SimpleModel> ClassIReadOnlyCollection { get; set; }
        public IReadOnlyCollection<SimpleModel> ClassIReadOnlyCollectionEmpty { get; set; }
        public IReadOnlyCollection<SimpleModel> ClassIReadOnlyCollectionNull { get; set; }

        public Dictionary<int, string> DictionaryThing1 { get; set; }
        public Dictionary<int, SimpleModel> DictionaryThing2 { get; set; }
        public IDictionary<int, string> DictionaryThing3 { get; set; }
        public IDictionary<int, SimpleModel> DictionaryThing4 { get; set; }
        public IReadOnlyDictionary<int, string> DictionaryThing5 { get; set; }
        public IReadOnlyDictionary<int, SimpleModel> DictionaryThing6 { get; set; }
        public ConcurrentDictionary<int, string> DictionaryThing7 { get; set; }
        public ConcurrentDictionary<int, SimpleModel> DictionaryThing8 { get; set; }

        public string[][] StringArrayOfArrayThing { get; set; }

        public static void AreEqual(TypesAllModel model1, TypesAllAsStringsModel model2)
        {
            Assert.NotNull(model1);
            Assert.NotNull(model2);
            Assert.NotEqual<object>(model1, model2);

            Assert.Equal(model1.BooleanThing.ToString().ToLower(), model2.BooleanThing);
            Assert.Equal(model1.ByteThing.ToString(), model2.ByteThing);
            Assert.Equal(model1.SByteThing.ToString(), model2.SByteThing);
            Assert.Equal(model1.Int16Thing.ToString(), model2.Int16Thing);
            Assert.Equal(model1.UInt16Thing.ToString(), model2.UInt16Thing);
            Assert.Equal(model1.Int32Thing.ToString(), model2.Int32Thing);
            Assert.Equal(model1.UInt32Thing.ToString(), model2.UInt32Thing);
            Assert.Equal(model1.Int64Thing.ToString(), model2.Int64Thing);
            Assert.Equal(model1.UInt64Thing.ToString(), model2.UInt64Thing);
            Assert.Equal(model1.SingleThing.ToString(), model2.SingleThing);
            Assert.Equal(model1.DoubleThing.ToString(), model2.DoubleThing);
            Assert.Equal(model1.DecimalThing.ToString(), model2.DecimalThing);
            Assert.Equal(model1.CharThing.ToString(), model2.CharThing);
            Assert.Equal(model1.DateTimeThing, DateTime.Parse(model2.DateTimeThing, null, DateTimeStyles.RoundtripKind)); //extra zeros removed at end of fractional sectons
            Assert.Equal(model1.DateTimeOffsetThing, DateTimeOffset.Parse(model2.DateTimeOffsetThing, null, DateTimeStyles.RoundtripKind));//extra zeros removed at end of fractional sectons
            Assert.Equal(model1.TimeSpanThing, TimeSpan.Parse(model2.TimeSpanThing));
#if NET6_0_OR_GREATER
            Assert.Equal(model1.DateOnlyThing, DateOnly.Parse(model2.DateOnlyThing)); //extra zeros removed at end of fractional sectons
            Assert.Equal(model1.TimeOnlyThing, TimeOnly.Parse(model2.TimeOnlyThing));
#endif
            Assert.Equal(model1.GuidThing, Guid.Parse(model2.GuidThing));

            Assert.Equal(model1.BooleanThingNullable?.ToString().ToLower(), model2.BooleanThingNullable);
            Assert.Equal(model1.ByteThingNullable?.ToString(), model2.ByteThingNullable);
            Assert.Equal(model1.SByteThingNullable?.ToString(), model2.SByteThingNullable);
            Assert.Equal(model1.Int16ThingNullable?.ToString(), model2.Int16ThingNullable);
            Assert.Equal(model1.UInt16ThingNullable?.ToString(), model2.UInt16ThingNullable);
            Assert.Equal(model1.Int32ThingNullable?.ToString(), model2.Int32ThingNullable);
            Assert.Equal(model1.UInt32ThingNullable?.ToString(), model2.UInt32ThingNullable);
            Assert.Equal(model1.Int64ThingNullable?.ToString(), model2.Int64ThingNullable);
            Assert.Equal(model1.UInt64ThingNullable?.ToString(), model2.UInt64ThingNullable);
            Assert.Equal(model1.SingleThingNullable?.ToString(), model2.SingleThingNullable);
            Assert.Equal(model1.DoubleThingNullable?.ToString(), model2.DoubleThingNullable);
            Assert.Equal(model1.DecimalThingNullable?.ToString(), model2.DecimalThingNullable);
            Assert.Equal(model1.CharThingNullable?.ToString(), model2.CharThingNullable);
            Assert.Equal(model1.DateTimeThingNullable, DateTime.Parse(model2.DateTimeThingNullable, null, DateTimeStyles.RoundtripKind));//extra zeros removed at end of fractional sectons
            Assert.Equal(model1.DateTimeOffsetThingNullable, DateTimeOffset.Parse(model2.DateTimeOffsetThingNullable));//extra zeros removed at end of fractional sectons
            Assert.Equal(model1.TimeSpanThingNullable, TimeSpan.Parse(model2.TimeSpanThingNullable));
#if NET6_0_OR_GREATER
            Assert.Equal(model1.DateOnlyThingNullable, DateOnly.Parse(model2.DateOnlyThingNullable)); //extra zeros removed at end of fractional sectons
            Assert.Equal(model1.TimeOnlyThingNullable, TimeOnly.Parse(model2.TimeOnlyThingNullable));
#endif
            Assert.Equal(model1.GuidThingNullable, Guid.Parse(model2.GuidThingNullable));

            Assert.Null(model1.ByteThingNullableNull);
            Assert.Null(model1.SByteThingNullableNull);
            Assert.Null(model1.Int16ThingNullableNull);
            Assert.Null(model1.UInt16ThingNullableNull);
            Assert.Null(model1.Int32ThingNullableNull);
            Assert.Null(model1.UInt32ThingNullableNull);
            Assert.Null(model1.Int64ThingNullableNull);
            Assert.Null(model1.UInt64ThingNullableNull);
            Assert.Null(model1.SingleThingNullableNull);
            Assert.Null(model1.DoubleThingNullableNull);
            Assert.Null(model1.DecimalThingNullableNull);
            Assert.Null(model1.CharThingNullableNull);
            Assert.Null(model1.DateTimeThingNullableNull);
            Assert.Null(model1.DateTimeOffsetThingNullableNull);
            Assert.Null(model1.TimeSpanThingNullableNull);
#if NET6_0_OR_GREATER
            Assert.Null(model1.DateOnlyThingNullableNull);
            Assert.Null(model1.TimeOnlyThingNullableNull);
#endif
            Assert.Null(model1.GuidThingNullableNull);

            Assert.Equal(model1.EnumThing.ToString(), model2.EnumThing);
            Assert.Equal(model1.EnumThingNullable?.ToString(), model2.EnumThingNullable);
            Assert.Equal(model1.EnumThingNullableNull?.ToString(), model2.EnumThingNullableNull);

            Assert.Equal(model1.StringThing, model2.StringThing);

            Assert.Null(model1.StringThingNull);
            Assert.Null(model2.StringThingNull);

            Assert.Equal(String.Empty, model1.StringThingEmpty);
            Assert.Equal(String.Empty, model2.StringThingEmpty);
        }
    }
}
