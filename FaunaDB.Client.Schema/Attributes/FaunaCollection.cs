using System;
namespace FaunaDB.Schema.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class FaunaCollection : Attribute
    {
        /// <summary>
        /// The name the collection. Cannot be events, sets, self, documents, or _.
        ///
        /// If null will infer name using nameof() on the object
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Documents are deleted this many days after their last write. Optional.
        /// </summary>
        public int? TtlDays { get; }

        /// <summary>
        /// Document history is retained for at least this many days. Defaults to 30 days.
        /// </summary>
        public int? HistoryDays { get; }

        /// <summary>
        /// Optional. Most be in a const json string.
        /// </summary>
        public string Data { get; }

        /// <summary>
        /// </summary>
        public string Permissions { get; }

        public FaunaCollection(string name, int ttlDays = -1, int historyDays = 30, string data = null, string permissions = null)
        {
            Name = name;
            if (ttlDays == -1)
                TtlDays = null;
            else
                TtlDays = ttlDays;
            if (historyDays == -1)
                HistoryDays = null;
            else
                HistoryDays = historyDays;
            Data = data;
            Permissions = Permissions;
        }
    }
}
