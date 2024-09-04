using System.Windows.Forms.VisualStyles;

namespace Warp9.Viewer
{
    public class ObjectCache<TKey, TValue> where TKey : struct
    {
        public ObjectCache(Func<TKey, TValue> generator)
        {
            gen = generator;
        }

        readonly Dictionary<TKey, TValue> cache = new Dictionary<TKey, TValue>();
        readonly Func<TKey, TValue> gen;

        public TKey LastState { get; set; }

        public virtual TValue Get(TKey key)
        {
            LastState = key;

            if (cache.TryGetValue(key, out TValue? value))
                return value;

            TValue ret = gen(key);
            cache[key] = ret;
            return ret;
        }
    }
};
