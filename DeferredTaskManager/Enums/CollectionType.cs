namespace DTM
{
    /// <summary>
    /// Acceptable collection types for use in event storage
    /// </summary>
    public enum CollectionType
    {
        /// <summary>
        /// Unordered collection of objects (it works faster)
        /// </summary>
        Bag,
        /// <summary>
        /// Ordered collection of objects
        /// </summary>
        Queue
    }
}
