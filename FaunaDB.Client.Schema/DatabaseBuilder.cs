using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using static FaunaDB.Schema.Query.LanguageExt;
using FaunaDB.Client;

using FaunaDB.Schema.Attributes;
namespace FaunaDB.Schema
{
    public static class DatabaseBuilder
    {
        /// <summary>
        /// Creates a schema from the base class including indexes
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="onlyDecoratedChildren"></param>
        /// <param name="ignoreBaseClass"></param>
        /// <returns></returns>
        public static async Task CreateDatabaseSchema(FaunaClient client, Type baseType, bool onlyDecoratedChildren = true, bool ignoreBaseClass= true)
        {
            var types = new List<Type>(Assembly.GetAssembly(baseType).GetTypes().Where(t => t.BaseType == baseType));

            if (ignoreBaseClass == false)
                types.Add(baseType);

            await CreateDatabaseSchema(client, types, onlyDecoratedChildren);
        }
        public static async Task CreateDatabaseSchema(FaunaClient client, List<Type> types, bool onlyDecoratedChildren = true)
        {
            foreach (Type type in types)
            {
                System.Diagnostics.Debug.WriteLine("Creating fauna collection for " + type.Name + "...");
                await client.Query(CreateCollection(type));
                System.Diagnostics.Debug.WriteLine("created");

                var indexAttributes = type.GetCustomAttributes<FaunaIndex>();
                if (indexAttributes != null)
                {
                    foreach (FaunaIndex index in indexAttributes)
                    {
                        System.Diagnostics.Debug.WriteLine("Creating fauna index for " + type.Name + "...");
                        if (index != null)
                        {
                            await index.Create(type, client);
                        }
                    }
                }
            }
        }
    }
}
