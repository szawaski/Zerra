// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading.Tasks;

public static class MethodWait
{
    public static void Wait(this Action it, TimeSpan timeout)
    {
        Exception? exception = null;

        var task = Task.Run(() =>
        {
            try
            {
                it();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        if (!task.Wait(timeout))
            throw new TimeoutException();
        if (exception is not null)
            throw exception;
    }
    public static void Wait<TArg>(this Action<TArg> it, TArg arg, TimeSpan timeout)
    {
        Exception? exception = null;

        var task = Task.Run(() =>
        {
            try
            {
                it(arg);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        if (!task.Wait(timeout))
            throw new TimeoutException();
        if (exception is not null)
            throw exception;
    }
    public static void Wait<TArg1, TArg2>(this Action<TArg1, TArg2> it, TArg1 arg1, TArg2 arg2, TimeSpan timeout)
    {
        Exception? exception = null;

        var task = Task.Run(() =>
        {
            try
            {
                it(arg1, arg2);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        if (!task.Wait(timeout))
            throw new TimeoutException();
        if (exception is not null)
            throw exception;
    }
    public static void Wait<TArg1, TArg2, TArg3>(this Action<TArg1, TArg2, TArg3> it, TArg1 arg1, TArg2 arg2, TArg3 arg3, TimeSpan timeout)
    {
        Exception? exception = null;

        var task = Task.Run(() =>
        {
            try
            {
                it(arg1, arg2, arg3);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        if (!task.Wait(timeout))
            throw new TimeoutException();
        if (exception is not null)
            throw exception;
    }
    public static void Wait<TArg1, TArg2, TArg3, TArg4>(this Action<TArg1, TArg2, TArg3, TArg4> it, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TimeSpan timeout)
    {
        Exception? exception = null;

        var task = Task.Run(() =>
        {
            try
            {
                it(arg1, arg2, arg3, arg4);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        if (!task.Wait(timeout))
            throw new TimeoutException();
        if (exception is not null)
            throw exception;
    }

    public static T Wait<T>(this Func<T> it, TimeSpan timeout)
    {
        T result = default!;
        Exception? exception = null;

        var task = Task.Run(() =>
        {
            try
            {
                result = it();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        if (!task.Wait(timeout))
            throw new TimeoutException();
        if (exception is not null)
            throw exception;

        return result;
    }
    public static T Wait<TArg, T>(this Func<TArg, T> it, TArg arg, TimeSpan timeout)
    {
        T result = default!;
        Exception? exception = null;

        var task = Task.Run(() =>
        {
            try
            {
                result = it(arg);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        if (!task.Wait(timeout))
            throw new TimeoutException();
        if (exception is not null)
            throw exception;

        return result;
    }
    public static T Wait<TArg1, TArg2, T>(this Func<TArg1, TArg2, T> it, TArg1 arg1, TArg2 arg2, TimeSpan timeout)
    {
        T result = default!;
        Exception? exception = null;

        var task = Task.Run(() =>
        {
            try
            {
                result = it(arg1, arg2);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        if (!task.Wait(timeout))
            throw new TimeoutException();
        if (exception is not null)
            throw exception;

        return result;
    }
    public static T Wait<TArg1, TArg2, TArg3, T>(this Func<TArg1, TArg2, TArg3, T> it, TArg1 arg1, TArg2 arg2, TArg3 arg3, TimeSpan timeout)
    {
        T result = default!;
        Exception? exception = null;

        var task = Task.Run(() =>
        {
            try
            {
                result = it(arg1, arg2, arg3);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        if (!task.Wait(timeout))
            throw new TimeoutException();
        if (exception is not null)
            throw exception;

        return result;
    }
    public static T Wait<TArg1, TArg2, TArg3, TArg4, T>(this Func<TArg1, TArg2, TArg3, TArg4, T> it, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TimeSpan timeout)
    {
        T result = default!;
        Exception? exception = null;

        var task = Task.Run(() =>
        {
            try
            {
                result = it(arg1, arg2, arg3, arg4);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        if (!task.Wait(timeout))
            throw new TimeoutException();
        if (exception is not null)
            throw exception;

        return result;
    }
}
