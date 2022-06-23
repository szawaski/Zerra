// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Threading
{
    public class Concurrent<T>
    {
        private readonly object locker = new object();

        private T value;
        public T Value
        {
            get
            {
                lock (locker)
                {
                    return this.value;
                }
            }
            set
            {
                lock (locker)
                {
                    this.value = value;
                }
            }
        }

        public Concurrent() { }
        public Concurrent(T value)
        {
            this.value = value;
        }

        public static bool operator ==(Concurrent<T> a, Concurrent<T> b)
        {
            if (ReferenceEquals(a, null))
            {
                if (ReferenceEquals(b, null))
                    return false;
                else
                    return true;
            }
            else if (ReferenceEquals(b, null))
            {
                return false;
            }
            else
            {
                return a.Value.Equals(b.Value);
            }
        }
        public static bool operator !=(Concurrent<T> a, Concurrent<T> b)
        {
            if (ReferenceEquals(a, null))
            {
                if (ReferenceEquals(b, null))
                    return true;
                else
                    return false;
            }
            else if (ReferenceEquals(b, null))
            {
                return true;
            }
            else
            {
                return !a.Value.Equals(b.Value);
            }
        }

        public static bool operator ==(Concurrent<T> a, T b)
        {
            if (ReferenceEquals(a, null))
            {
                if (ReferenceEquals(b, null))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else if (ReferenceEquals(b, null))
            {
                return false;
            }
            else
            {
                return a.Value.Equals(b);
            }
        }
        public static bool operator !=(Concurrent<T> a, T b)
        {
            if (ReferenceEquals(a, null))
            {
                if (ReferenceEquals(b, null))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (ReferenceEquals(b, null))
            {
                return true;
            }
            else
            {
                return !a.Value.Equals(b);
            }
        }

        public static implicit operator T(Concurrent<T> it)
        {
            if (ReferenceEquals(it, null))
                return default;
            return it.Value;
        }
        public static explicit operator Concurrent<T>(T it)
        {
            return new Concurrent<T>(it);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (ReferenceEquals(obj, null))
                return false;

            if (obj is Concurrent<T> concurrentT)
            {
                var concurrentTValue = concurrentT.Value;
                var thisValue = Value;
                if (concurrentTValue == null)
                {
                    if (thisValue == null)
                        return true;
                    else
                        return false;
                }
                
                if (thisValue == null)
                    return false;
                return concurrentTValue.Equals(thisValue);
            }

            if (obj is T t)
            {
                var thisValue = Value;
                if (t == null)
                {
                    if (thisValue == null)
                        return true;
                    else
                        return false;
                }
                if (thisValue == null)
                    return false;
                return t.Equals(thisValue);
            }

            return false;
        }

        public override int GetHashCode()
        {
            var thisValue = Value;
            return thisValue == null ? default : thisValue.GetHashCode();
        }
    }
}
