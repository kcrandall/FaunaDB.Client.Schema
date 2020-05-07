using System;
using System.Collections.Generic;
using System.Reflection;

using Newtonsoft.Json;

using FaunaDB.Types;
using FaunaDB.Query;
using static FaunaDB.Query.Language;
using FaunaDB.Schema.Attributes;

namespace FaunaDB.Schema.Query
{
	public partial struct LanguageExt
	{
        /// <summary>
        /// To be used with the <see cref="FaunaCollection"/> attribute. If a
        /// type is passed without the Attribute it will take the member's name
        /// </summary>
        /// <param name="type">The class's type you want to make a collection for.</param>
        /// <returns></returns>
        public static Expr CreateCollection(Type type)
        {
            FaunaCollection attr = type.GetCustomAttribute<FaunaCollection>();
            var options = new Dictionary<string, Expr>();
            options.Add("name", attr?.Name ?? type.Name);
            if (attr.Data != null)
            {
                options.Add("data", Obj(JsonConvert.DeserializeObject<Dictionary<string, Expr>>(attr.Data)));
            }
            if (attr.HistoryDays != null)
            {
                options.Add("history_days", attr.HistoryDays);
            }
            if (attr.TtlDays != null)
            {
                options.Add("ttl_days", attr.TtlDays);
            }
            if (attr.Permissions != null)
            {
                options.Add("permissions", Obj(JsonConvert.DeserializeObject<Dictionary<string, Expr>>(attr.Permissions)));
            }
            return Language.CreateCollection(Obj(options));
        }
    }
}
