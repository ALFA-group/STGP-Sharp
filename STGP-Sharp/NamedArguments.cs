#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Collections.Extensions;

#endregion

namespace STGP_Sharp
{
    public class NamedArguments
    {
        private readonly MultiValueDictionary<(string, Type), object> _dictionary =
            new MultiValueDictionary<(string, Type), object>();

        public NamedArguments(params (string, object)[] args)
        {
            foreach ((string key, object arg) in args)
            {
                this.Add(arg.GetType(), arg, key);
            }
        }

        public void Add<T>(T t, string key)
        {
            Debug.Assert(null != t, "Null not allowed in collection");

            this._dictionary.Add((key, typeof(T)), t);
        }

        public void Add(Type t, object o, string key)
        {
            Debug.Assert(t.IsInstanceOfType(o),
                $"Trying to add object with incorrect type!  Adding {o.GetType().Name} but expected {t.Name}");

            this._dictionary.Add((key, t), o);
        }

        public void RemoveAllOfType(Type t, string key)
        {
            this._dictionary.Remove((key, t));
        }

        public void AddDynamic(object o, string key)
        {
            this.Add(o.GetType(), o, key);
        }

        public IEnumerable<T> Get<T>(string key)
        {
            if (this._dictionary.TryGetValue((key, typeof(T)), out IReadOnlyCollection<object>? collection))
            {
                return collection.Cast<T>();
            }

            return Enumerable.Empty<T>();
        }
    }
}