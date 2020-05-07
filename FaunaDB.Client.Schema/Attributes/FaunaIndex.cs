using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using FaunaDB.Query;
using FaunaDB.Types;
using FaunaDB.Client;
using static FaunaDB.Query.Language;
namespace FaunaDB.Schema.Attributes
{
    public enum FaunaTermType { Ts, Ref }
    public enum FaunaValueType { Ts, Ref }
    public enum IndexOrdering { Default, Reverse }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class FaunaIndex : Attribute
    {
        /// <summary>
        /// The name the index.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The terms in the index. Data terms should be given by a string ex: nameof(MyClass.FirstName)
        ///
        /// if you want to include the ref or ts in terms just pass a single value FaunaTermType.Ts/Ref
        /// </summary>
        public object[] Terms { get; }

        /// <summary>
        /// The terms in the index. Data values should be given by a string ex: nameof(MyClass.FirstName)
        ///
        /// if you want to include the ref or ts in terms just pass a single value FaunaTermType.Ts/Ref
        ///
        /// Values can also use IndexOrdering.Rerverse after the value ex: FaunaValueType.Field, nameof(MyClass.FirstName), IndexOrdering.Rerverse
        /// </summary>
        public object[] Values { get; }

        /// <summary>
        /// Optional - If true, maintains a unique constraint on combined terms and values. The default is false.
        /// </summary>
        public bool Unique { get; }

        /// <summary>
        /// Optional - If true, writes to this index are serialized with concurrent reads and writes. The default is true.
        /// </summary>
        public bool Serialized { get; }

        /// <summary>
        /// Optional. Most be in a const json string.
        /// </summary>
        public string Data { get; }

        /// <summary>
        /// Optional. Most be in a const json string.
        /// </summary>
        public string Permissions { get; }

        public FaunaIndex(string name = null, object[] terms = null, object[] values = null, bool unique = false, bool serialized = true, string data = null, string permissions = null)
        {
            Name = name;
            Terms = terms;
            Values = values;
            Unique = unique;
            Serialized = serialized;
            Data = data;
            Permissions = permissions;
        }

        public string GenerateIndexName(Type collectionType)
        {
            FaunaCollection collection = collectionType.GetCustomAttribute<FaunaCollection>();

            string s = string.Empty;
            if(Values == null ||
                (Values != null && Values.Length == 0) ||
                (Values != null && Values.Length == 1 && Values[0].GetType() == typeof(FaunaValueType) && (FaunaValueType)Enum.Parse(typeof(FaunaValueType), Values[0].ToString()) == FaunaValueType.Ref))
            {
                s += $"{collection?.Name ?? collectionType.Name}";
            }
            else
            {
                int i = 0;
                foreach(object val in Values)
                {
                    if (i > 0)
                        s += "_";

                    if (val.GetType() != typeof(FaunaValueType))
                    {
                        if(val.GetType() == typeof(IndexOrdering))
                        {
                            IndexOrdering orderingType = (IndexOrdering)Enum.Parse(typeof(IndexOrdering), val.ToString());
                            if (orderingType == IndexOrdering.Reverse)
                                s += $"reverse";
                        }
                        else
                        {
                            FaunaFieldAttribute fieldAttribute = collectionType.GetProperty(val.ToString())?.GetCustomAttribute<FaunaFieldAttribute>();
                            if (fieldAttribute == null)
                                fieldAttribute = collectionType.GetField(val.ToString())?.GetCustomAttribute<FaunaFieldAttribute>();

                            s += $"{fieldAttribute?.Name ?? val.ToString()}";
                        }
                    }
                    else
                    {
                        FaunaValueType faunaValueType = (FaunaValueType)Enum.Parse(typeof(FaunaValueType), val.ToString());
                        if (faunaValueType == FaunaValueType.Ref)
                            s += $"{collection?.Name ?? collectionType.Name}";
                        else if (faunaValueType == FaunaValueType.Ts)
                            s += $"ts";
                    }
                    i++;
                }
            }
            s += "_by";
            if(Terms != null)
            {
                for (int i = 0; i < Terms.Length; i++)
                {
                    object term = Terms[i];
                    s += "_";
                    if (term.GetType() != typeof(FaunaValueType))
                    {
                        FaunaFieldAttribute fieldAttribute = collectionType.GetProperty(term.ToString())?.GetCustomAttribute<FaunaFieldAttribute>();
                        if (fieldAttribute == null)
                            fieldAttribute = collectionType.GetField(term.ToString())?.GetCustomAttribute<FaunaFieldAttribute>();
                        s += $"{fieldAttribute?.Name ?? term.ToString()}";
                    }
                    else
                    {
                        FaunaTermType faunaValueType = (FaunaTermType)Enum.Parse(typeof(FaunaTermType), term.ToString());
                        if (faunaValueType == FaunaTermType.Ref)
                            s += $"{collection?.Name ?? collectionType.Name}";
                        else if (faunaValueType == FaunaTermType.Ts)
                            s += $"ts";
                    }
                }
            }
            else
            {
                //TODO figure out what to call it when there are no terms
            }
            return s;
        }
        
        public async Task Create(Type collectionType, FaunaClient client)
        {
            FaunaCollection collection = collectionType.GetCustomAttribute<FaunaCollection>();

            var options = new Dictionary<string, Expr>();
            if(!string.IsNullOrEmpty(this.Name))
                options.Add("name", this.Name);
            else
                options.Add("name", GenerateIndexName(collectionType));
            options.Add("source", Collection(collection?.Name ?? collectionType.Name));

            if (Terms != null && Terms.Length > 0)
                options.Add("terms", GenerateTermsObj(collectionType));

            if (Values != null && Values.Length > 0)
                options.Add("values", GenerateValuesObj(collectionType));

            if (this.Data != null)
                options.Add("data", Obj(JsonConvert.DeserializeObject<Dictionary<string, Expr>>(Data)));
            if (this.Permissions != null)
                options.Add("permissions", Obj(JsonConvert.DeserializeObject<Dictionary<string, Expr>>(Permissions)));
            options.Add("unique", this.Unique);
            options.Add("serialized", this.Serialized);

            await client.Query(CreateIndex(options));
        }
        private Expr GenerateTermsObj(Type collectionType)
        {
            List<Expr> exprs = new List<Expr>();
            foreach (var term in Terms)
            {
                if (term.GetType() == typeof(string))
                {
                    FaunaFieldAttribute fieldAttribute = collectionType.GetProperty(term.ToString())?.GetCustomAttribute<FaunaFieldAttribute>();
                    if (fieldAttribute == null)
                        fieldAttribute = collectionType.GetField(term.ToString())?.GetCustomAttribute<FaunaFieldAttribute>();
                    exprs.Add(Obj("field", Arr("data", fieldAttribute?.Name ?? term.ToString())));
                }
                else if (term.GetType() == typeof(FaunaTermType))
                {
                    FaunaTermType faunaTermType = (FaunaTermType)Enum.Parse(typeof(FaunaTermType), term.ToString());
                    if (faunaTermType == FaunaTermType.Ref)
                        exprs.Add(Obj("field", "ref"));
                    else if (faunaTermType == FaunaTermType.Ts)
                        exprs.Add(Obj("field", "ts"));
                }
            }

            return Arr(exprs.ToArray());
        }
        private Expr GenerateValuesObj(Type collectionType)
        {
            List<Expr> exprs = new List<Expr>();
            int i = 0;
            foreach (var value in Values)
            {
                bool isReverse = false;
                if (Values.Length > i + 1)
                    if(Values[i+1].GetType() == typeof(IndexOrdering) && (IndexOrdering)Enum.Parse(typeof(IndexOrdering), Values[i + 1].ToString()) == IndexOrdering.Reverse)
                        isReverse = true;

                if (value.GetType() == typeof(string))
                {
                    FaunaFieldAttribute fieldAttribute = collectionType.GetProperty(value.ToString())?.GetCustomAttribute<FaunaFieldAttribute>();
                    if (fieldAttribute == null)
                        fieldAttribute = collectionType.GetField(value.ToString())?.GetCustomAttribute<FaunaFieldAttribute>();
                    if(isReverse)
                        exprs.Add(Obj("field", Arr("data", fieldAttribute?.Name ?? value.ToString()), "reverse", true));
                    else
                        exprs.Add(Obj("field", Arr("data", fieldAttribute?.Name ?? value.ToString())));
                }
                else if (value.GetType() == typeof(FaunaTermType))
                {
                    FaunaValueType faunaValueType = (FaunaValueType)Enum.Parse(typeof(FaunaValueType), value.ToString());
                    if (faunaValueType == FaunaValueType.Ref)
                    {
                        if (isReverse)
                            exprs.Add(Obj("field", "ref", "reverse", true));
                        else
                            exprs.Add(Obj("field", "ref"));
                    }
                    else if (faunaValueType == FaunaValueType.Ts)
                    {
                        if (isReverse)
                            exprs.Add(Obj("field", "ts", "reverse", true));
                        else
                            exprs.Add(Obj("field", "ts"));
                    }
                }
                i++;
            }
            return Arr(exprs.ToArray());
        }
    }
}
