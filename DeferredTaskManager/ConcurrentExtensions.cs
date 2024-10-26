using System.Collections.Concurrent;

namespace DeferringTasksManager
{
    public static class ConcurrentExtensions
    {
        public static void AddOrUpdate<K, V>(this ConcurrentDictionary<K, V> dictionary, K key, V value)
        {
            dictionary.AddOrUpdate(key, value, (oldkey, oldvalue) => value);
        }
    }
}
