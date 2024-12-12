using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;

namespace Warp9.Jobs
{
    public class JobWorkspace
    {
        Dictionary<string, object?> repository = new Dictionary<string, object?>();

        public IReadOnlyDictionary<string, object?> Items => repository;

        public bool TryCopy(string src, string dest)
        {
            if (repository.TryGetValue(src, out object? x) && x is not null)
            {
                repository[dest] = x;
                return true;
            }

            return false;
        }

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
            
            Grow(list, index + 1);
            list[index] = value;
        }

        public void Set<T>(string key, T value)
        {
            repository[key] = value;
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

        static void Grow<T>(List<T> list, int size, T element = default)
        {
            int count = list.Count;

            if (size < count)
            {
            }
            else if (size > count)
            {
                if (size > list.Capacity)   // Optimization
                    list.Capacity = size;

                list.AddRange(Enumerable.Repeat(element, size - count));
            }
        }

    }
}
