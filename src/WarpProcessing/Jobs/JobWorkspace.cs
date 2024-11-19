using System;
using System.Diagnostics.CodeAnalysis;

namespace Warp9.Jobs
{
    public class JobWorkspace
    {
        public void Set<T>(string key, int index, T value)
        {
            
        }

        public void Remove(string key)
        {
        }

        public bool TryGet<T>(string key, [MaybeNullWhen(false)] out T value)
        {
            throw new NotImplementedException();
        }

        public bool TryGet<T>(string key, int index, [MaybeNullWhen(false)] out T value)
        {
            throw new NotImplementedException();
        }
    }
}
