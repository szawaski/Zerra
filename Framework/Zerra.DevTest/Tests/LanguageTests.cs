// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Diagnostics;
using System.Text;

namespace Zerra.DevTest
{
    public static class LanguageTests
    {
        public static readonly Type BoolType = typeof(bool);
        public static readonly Type ByteType = typeof(byte);
        public static readonly Type SByteType = typeof(sbyte);
        public static readonly Type ShortType = typeof(short);
        public static readonly Type UShortType = typeof(ushort);
        public static readonly Type IntType = typeof(int);
        public static readonly Type UIntType = typeof(uint);
        public static readonly Type LongType = typeof(long);
        public static readonly Type ULongType = typeof(ulong);
        public static readonly Type FloatType = typeof(float);
        public static readonly Type DoubleType = typeof(double);
        public static readonly Type DecimalType = typeof(decimal);
        public static readonly Type StringType = typeof(string);
        public static readonly Type CharType = typeof(char);

        public static void TestTypeComparison()
        {
            var type = typeof(char);
            var itterations = 10000000;

            {
                var timer = Stopwatch.StartNew();
                for (var i = 0; i < itterations; i++)
                {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
                    string something = null;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
                    if (type == typeof(bool))
                    {
                        something = "something";
                    }
                    else if (type == typeof(byte))
                    {
                        something = "something";
                    }
                    else if (type == typeof(sbyte))
                    {
                        something = "something";
                    }
                    else if (type == typeof(short))
                    {
                        something = "something";
                    }
                    else if (type == typeof(ushort))
                    {
                        something = "something";
                    }
                    else if (type == typeof(int))
                    {
                        something = "something";
                    }
                    else if (type == typeof(uint))
                    {
                        something = "something";
                    }
                    else if (type == typeof(long))
                    {
                        something = "something";
                    }
                    else if (type == typeof(ulong))
                    {
                        something = "something";
                    }
                    else if (type == typeof(float))
                    {
                        something = "something";
                    }
                    else if (type == typeof(decimal))
                    {
                        something = "something";
                    }
                    else if (type == typeof(double))
                    {
                        something = "something";
                    }
                    else if (type == typeof(string))
                    {
                        something = "something";
                    }
                    else if (type == typeof(char))
                    {
                        something = "something";
                    }
                }
                timer.Stop();
                Console.WriteLine("if typeof {0}", timer.ElapsedMilliseconds);
            }

            {
                var timer = Stopwatch.StartNew();
                for (var i = 0; i < itterations; i++)
                {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
                    string something = null;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
                    if (type == BoolType)
                    {
                        something = "something";
                    }
                    else if (type == ByteType)
                    {
                        something = "something";
                    }
                    else if (type == SByteType)
                    {
                        something = "something";
                    }
                    else if (type == ShortType)
                    {
                        something = "something";
                    }
                    else if (type == UShortType)
                    {
                        something = "something";
                    }
                    else if (type == IntType)
                    {
                        something = "something";
                    }
                    else if (type == UIntType)
                    {
                        something = "something";
                    }
                    else if (type == LongType)
                    {
                        something = "something";
                    }
                    else if (type == ULongType)
                    {
                        something = "something";
                    }
                    else if (type == FloatType)
                    {
                        something = "something";
                    }
                    else if (type == DoubleType)
                    {
                        something = "something";
                    }
                    else if (type == DecimalType)
                    {
                        something = "something";
                    }
                    else if (type == StringType)
                    {
                        something = "something";
                    }
                    else if (type == CharType)
                    {
                        something = "something";
                    }
                }
                timer.Stop();
                Console.WriteLine("if Type {0}", timer.ElapsedMilliseconds);
            }

            {
                var timer = Stopwatch.StartNew();
                for (var i = 0; i < itterations; i++)
                {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
                    string something = null;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
                    if (type.Name == BoolType.Name)
                    {
                        something = "something";
                    }
                    else if (type.Name == ByteType.Name)
                    {
                        something = "something";
                    }
                    else if (type.Name == SByteType.Name)
                    {
                        something = "something";
                    }
                    else if (type.Name == ShortType.Name)
                    {
                        something = "something";
                    }
                    else if (type.Name == UShortType.Name)
                    {
                        something = "something";
                    }
                    else if (type.Name == IntType.Name)
                    {
                        something = "something";
                    }
                    else if (type.Name == UIntType.Name)
                    {
                        something = "something";
                    }
                    else if (type.Name == LongType.Name)
                    {
                        something = "something";
                    }
                    else if (type.Name == ULongType.Name)
                    {
                        something = "something";
                    }
                    else if (type.Name == FloatType.Name)
                    {
                        something = "something";
                    }
                    else if (type.Name == DoubleType.Name)
                    {
                        something = "something";
                    }
                    else if (type.Name == DecimalType.Name)
                    {
                        something = "something";
                    }
                    else if (type.Name == StringType.Name)
                    {
                        something = "something";
                    }
                    else if (type.Name == CharType.Name)
                    {
                        something = "something";
                    }
                }
                timer.Stop();
                Console.WriteLine("if Type Name {0}", timer.ElapsedMilliseconds);
            }

            {
                var timer = Stopwatch.StartNew();
                for (var i = 0; i < itterations; i++)
                {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
                    string something = null;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
                    if (type.Name == "Boolean")
                    {
                        something = "something";
                    }
                    else if (type.Name == "Byte")
                    {
                        something = "something";
                    }
                    else if (type.Name == "SByte")
                    {
                        something = "something";
                    }
                    else if (type.Name == "Int16")
                    {
                        something = "something";
                    }
                    else if (type.Name == "UInt16")
                    {
                        something = "something";
                    }
                    else if (type.Name == "Int32")
                    {
                        something = "something";
                    }
                    else if (type.Name == "UInt32")
                    {
                        something = "something";
                    }
                    else if (type.Name == "Int64")
                    {
                        something = "something";
                    }
                    else if (type.Name == "UInt64")
                    {
                        something = "something";
                    }
                    else if (type.Name == "Single")
                    {
                        something = "something";
                    }
                    else if (type.Name == "Double")
                    {
                        something = "something";
                    }
                    else if (type.Name == "Decimal")
                    {
                        something = "something";
                    }
                    else if (type.Name == "String")
                    {
                        something = "something";
                    }
                    else if (type.Name == "Char")
                    {
                        something = "something";
                    }
                }
                timer.Stop();
                Console.WriteLine("if String {0}", timer.ElapsedMilliseconds);
            }

            {
                var timer = Stopwatch.StartNew();
                for (var i = 0; i < itterations; i++)
                {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
                    string something = null;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
                    if (type.Name == nameof(Boolean))
                    {
                        something = "something";
                    }
                    else if (type.Name == nameof(Byte))
                    {
                        something = "something";
                    }
                    else if (type.Name == nameof(SByte))
                    {
                        something = "something";
                    }
                    else if (type.Name == nameof(Int16))
                    {
                        something = "something";
                    }
                    else if (type.Name == nameof(UInt16))
                    {
                        something = "something";
                    }
                    else if (type.Name == nameof(Int32))
                    {
                        something = "something";
                    }
                    else if (type.Name == nameof(UInt32))
                    {
                        something = "something";
                    }
                    else if (type.Name == nameof(Int64))
                    {
                        something = "something";
                    }
                    else if (type.Name == nameof(UInt64))
                    {
                        something = "something";
                    }
                    else if (type.Name == nameof(Single))
                    {
                        something = "something";
                    }
                    else if (type.Name == nameof(Double))
                    {
                        something = "something";
                    }
                    else if (type.Name == nameof(Decimal))
                    {
                        something = "something";
                    }
                    else if (type.Name == nameof(String))
                    {
                        something = "something";
                    }
                    else if (type.Name == nameof(Char))
                    {
                        something = "something";
                    }
                }
                timer.Stop();
                Console.WriteLine("if nameof {0}", timer.ElapsedMilliseconds);
            }

            {
                var timer = Stopwatch.StartNew();
                for (var i = 0; i < itterations; i++)
                {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
                    string something = null;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
                    switch (type.Name)
                    {
                        case nameof(Boolean): something = "something"; break;
                        case nameof(Byte): something = "something"; break;
                        case nameof(SByte): something = "something"; break;
                        case nameof(Int16): something = "something"; break;
                        case nameof(UInt16): something = "something"; break;
                        case nameof(Int32): something = "something"; break;
                        case nameof(UInt32): something = "something"; break;
                        case nameof(Int64): something = "something"; break;
                        case nameof(UInt64): something = "something"; break;
                        case nameof(Single): something = "something"; break;
                        case nameof(Double): something = "something"; break;
                        case nameof(Decimal): something = "something"; break;
                        case nameof(String): something = "something"; break;
                        case nameof(Char): something = "something"; break;
                    }
                }
                timer.Stop();
                Console.WriteLine("switch nameof {0}", timer.ElapsedMilliseconds);
            }

            {
                var timer = Stopwatch.StartNew();
                for (var i = 0; i < itterations; i++)
                {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
                    string something = null;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
                    switch (type.Name)
                    {
                        case "Boolean": something = "something"; break;
                        case "Byte": something = "something"; break;
                        case "SByte": something = "something"; break;
                        case "Int16": something = "something"; break;
                        case "UInt16": something = "something"; break;
                        case "Int32": something = "something"; break;
                        case "UInt32": something = "something"; break;
                        case "Int64": something = "something"; break;
                        case "UInt64": something = "something"; break;
                        case "Single": something = "something"; break;
                        case "Double": something = "something"; break;
                        case "Decimal": something = "something"; break;
                        case "String": something = "something"; break;
                        case "Char": something = "something"; break;
                    }
                }
                timer.Stop();
                Console.WriteLine("switch String {0}", timer.ElapsedMilliseconds);
            }
        }

        public static void TestStringBuilderBoxing()
        {
            var loops = 30000;
            var value = (object)loops;



            {
                var sb = new StringBuilder();
                var timer = Stopwatch.StartNew();
                for (var i = 0; i < loops; i++)
                {
                    sb.Append((int)value);
                    var s = sb.ToString();
                }
                timer.Stop();
                Console.WriteLine("Box: {0}", timer.ElapsedMilliseconds);
            }

            {
                var sb = new StringBuilder();
                var timer = Stopwatch.StartNew();
                for (var i = 0; i < loops; i++)
                {
                    sb.Append(value.ToString());
                    var s = sb.ToString();
                }
                timer.Stop();
                Console.WriteLine("ToString: {0}", timer.ElapsedMilliseconds);
            }

            {
                var sb = new StringBuilder();
                var timer = Stopwatch.StartNew();
                for (var i = 0; i < loops; i++)
                {
                    sb.Append((int)value);
                    var s = sb.ToString();
                }
                timer.Stop();
                Console.WriteLine("Box: {0}", timer.ElapsedMilliseconds);
            }

            {
                var sb = new StringBuilder();
                var timer = Stopwatch.StartNew();
                for (var i = 0; i < loops; i++)
                {
                    sb.Append(value.ToString());
                    var s = sb.ToString();
                }
                timer.Stop();
                Console.WriteLine("ToString: {0}", timer.ElapsedMilliseconds);
            }
        }
    }
}
