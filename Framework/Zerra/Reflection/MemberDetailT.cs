//// Copyright © KaKush LLC
//// Written By Steven Zawaski
//// Licensed to you under the MIT license

//using System;
//using System.Reflection;
//using System.Runtime.CompilerServices;

//namespace Zerra.Reflection
//{
//    public abstract class MemberDetail<T> : MemberDetail
//    {
//        public new MemberDetail<T>? BackingFieldDetail { get; }

//        private bool getterLoaded = false;
//        private Func<T, object?>? getter = null;
//        public new Func<T, object?> Getter
//        {
//            get
//            {
//                LoadGetter();
//                return this.getter ?? throw new NotSupportedException($"{nameof(MemberDetail)} {Name} does not have a {nameof(Getter)}");
//            }
//        }
//        public new bool HasGetter
//        {
//            get
//            {
//                LoadGetter();
//                return this.getter != null;
//            }
//        }
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private void LoadGetter()
//        {
//            if (!getterLoaded)
//            {
//                lock (locker)
//                {
//                    if (!getterLoaded)
//                    {
//                        if (MemberInfo.MemberType == MemberTypes.Property)
//                        {
//                            var property = (PropertyInfo)MemberInfo;
//                            if (!property.PropertyType.IsPointer)
//                            {
//                                if (BackingFieldDetail == null)
//                                {
//                                    this.getter = AccessorGenerator.GenerateGetter<T>(property);
//                                }
//                                else
//                                {
//                                    this.getter = BackingFieldDetail.Getter;
//                                }
//                            }
//                        }
//                        else if (MemberInfo.MemberType == MemberTypes.Field)
//                        {
//                            var field = (FieldInfo)MemberInfo;
//                            if (!field.FieldType.IsPointer)
//                            {
//                                this.getter = AccessorGenerator.GenerateGetter<T>(field);
//                            }
//                        }
//                        getterLoaded = true;
//                    }
//                }
//            }
//        }

//        private bool setterLoaded = false;
//        private Action<T, object?>? setter = null;
//        public new Action<T, object?> Setter
//        {
//            get
//            {
//                LoadSetter();
//                return this.setter ?? throw new NotSupportedException($"{nameof(MemberDetail)} {Name} does not have a {nameof(Setter)}");
//            }
//        }
//        public new bool HasSetter
//        {
//            get
//            {
//                LoadSetter();
//                return this.setter != null;
//            }
//        }
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private void LoadSetter()
//        {
//            if (!setterLoaded)
//            {
//                lock (locker)
//                {
//                    if (!setterLoaded)
//                    {
//                        if (MemberInfo.MemberType == MemberTypes.Property)
//                        {
//                            var property = (PropertyInfo)MemberInfo;
//                            if (!property.PropertyType.IsPointer)
//                            {
//                                if (BackingFieldDetail == null)
//                                {
//                                    this.setter = AccessorGenerator.GenerateSetter<T>(property);
//                                }
//                                else
//                                {
//                                    this.setter = BackingFieldDetail.Setter;
//                                }
//                            }
//                        }
//                        else if (MemberInfo.MemberType == MemberTypes.Field)
//                        {
//                            var field = (FieldInfo)MemberInfo;
//                            if (!field.FieldType.IsPointer)
//                            {
//                                this.setter = AccessorGenerator.GenerateSetter<T>(field);
//                            }
//                        }
//                        setterLoaded = true;
//                    }
//                }
//            }
//        }

//        internal MemberDetail(MemberInfo member, MemberDetail<T>? backingFieldDetail, object locker) : base(member, backingFieldDetail, locker)
//        {
//            this.BackingFieldDetail = backingFieldDetail;
//        }

//        private static readonly Type typeDetailT = typeof(MemberDetail<,>);
//        internal static MemberDetail<T> New(Type valueType, MemberInfo member, MemberDetail? backingFieldDetail, object locker)
//        {
//            var typeDetailGeneric = typeDetailT.MakeGenericType(typeof(T), valueType);
//            var obj = typeDetailGeneric.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0].Invoke(new object?[] { member, backingFieldDetail, locker });
//            return (MemberDetail<T>)obj;
//        }
//    }
//}
