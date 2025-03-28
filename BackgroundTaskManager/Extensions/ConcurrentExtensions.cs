using System.Collections.Concurrent;

namespace BTM.Extensions
{
    internal static class ConcurrentExtensions
    {
        internal static void AddOrUpdate<K, V>(this ConcurrentDictionary<K, V> dictionary, K key, V value)
        {
            dictionary.AddOrUpdate(key, value, (oldkey, oldvalue) => value);
        }
    }
}
