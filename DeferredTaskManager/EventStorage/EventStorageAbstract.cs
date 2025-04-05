using DTM.CollectionStrategy;
using System;
using System.Collections.Generic;
using System.Text;

namespace DTM.EventStorage
{
    public abstract class EventStorageAbstract<T>
    {
        public ICollectionStrategy<T> _collectionStrategy = default!;


        public void InitializeCollectionStrategy(CollectionType type)
        {
            _collectionStrategy = type switch
            {
                CollectionType.Bag => new BagStrategy<T>(),
                CollectionType.Queue => new QueueStrategy<T>(),
                _ => throw new ArgumentException("Unacceptable collection type"),
            };
        }
    }
}
