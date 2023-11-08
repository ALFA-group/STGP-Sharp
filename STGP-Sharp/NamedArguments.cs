#region

#nullable enable
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
        private readonly Dictionary<(string, Type), object> _dictionary =
            new Dictionary<(string, Type), object>();

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

        public T Get<T>(string key)
        {
            this._dictionary.TryGetValue((key, typeof(T)), out object? value);

            return (T)value ?? 
                   throw new Exception($"Object of type {typeof(T)} with key {key} not found.");
        }

        public void ReplaceOrAdd<T>(string key, T t)
        {
            if (null == t)
            {
                throw new Exception("Object added cannot be null");
            }
            this._dictionary.TryGetValue((key, typeof(T)), out object? value);

            if (null == value)
            {
                this.Add(typeof(T), t, key);
            }
            else
            {
                this._dictionary[(key, typeof(T))] = t;
            }
        }
    }
}