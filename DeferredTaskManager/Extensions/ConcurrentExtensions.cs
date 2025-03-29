using System.Collections.Concurrent;

namespace DTM.Extensions
{
    internal static class ConcurrentExtensions
    {
        internal static void AddOrUpdate<K, V>(this ConcurrentDictionary<K, V> dictionary, K key, V value)
        {
            dictionary.AddOrUpdate(key, value, (oldkey, oldvalue) => value);
        }
    }
}
