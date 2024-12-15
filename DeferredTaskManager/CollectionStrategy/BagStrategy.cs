﻿using System.Collections.Concurrent;

namespace DeferredTaskManager.CollectionStrategy
{
    public class BagStrategy<T> : ICollectionStrategy<T>
    {
        private readonly ConcurrentBag<T> _bag = new();
        public void Add(T item) => _bag.Add(item);
        public IEnumerable<T> GetItems() => _bag;
        public int Count => _bag.Count;
        public bool IsEmpty => _bag.IsEmpty;
        public void Clear() => _bag.Clear();       
    }
}