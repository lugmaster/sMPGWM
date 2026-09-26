
using System;

namespace sMPGWM.Scripts.FileLoadingSystem;

public sealed class LoadResult<T>
{
    private LoadResult(T? value, DataLoadError? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }
    public DataLoadError? Error { get; }
    public bool IsSuccess => Error is null;

    public static LoadResult<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new LoadResult<T>(value, null);
    }

    public static LoadResult<T> Failure(DataLoadError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new LoadResult<T>(default, error);
    }
}