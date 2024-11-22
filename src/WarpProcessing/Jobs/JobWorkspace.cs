using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Warp9.Jobs
{
    public class JobWorkspace
    {
        Dictionary<string, object?> repository = new Dictionary<string, object?>();

        public void Set<T>(string key, int index, T value)
        {
            if (repository.TryGetValue(key, out var val) && val is List<T> list)
            {
            }
            else
            {
                list = new List<T>();
                repository[key] = list;
            }
            
            list.EnsureCapacity(index + 1);
            list[index] = value;
        }

        public void Remove(string key)
        {
            repository.Remove(key);
        }

        public bool TryGet<T>(string key, [MaybeNullWhen(false)] out T value)
        {
            if (repository.TryGetValue(key, out var val) && val is T tval)
            {
                value = tval;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGet<T>(string key, int index, [MaybeNullWhen(false)] out T value)
        {
            if (repository.TryGetValue(key, out var val) && 
                val is List<T> list &&
                list.Count > index)
            {
                value = list[index];
                return true;
            }

            value = default;
            return false;
        }

    }
}
