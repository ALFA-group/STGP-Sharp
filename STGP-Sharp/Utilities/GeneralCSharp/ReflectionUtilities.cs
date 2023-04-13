#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#endregion

namespace STGP_Sharp.Utilities.GeneralCSharp
{
    public static class ReflectionUtilities
    {
        private static readonly ConcurrentDictionary<(Type type, Type attribute), bool> _dCacheHasAttribute =
            new ConcurrentDictionary<(Type type, Type attribute), bool>();

        public static bool HasAttribute(this Type t, Type requiredAttribute)
        {
            (Type t, Type requiredAttribute) key = (t, requiredAttribute);
            if (_dCacheHasAttribute.TryGetValue(key, out bool hasAttribute))
            {
                return hasAttribute;
            }

            hasAttribute = null != t.GetCustomAttribute(requiredAttribute);
            _dCacheHasAttribute[key] = hasAttribute;

            return hasAttribute;
        }

        public static IEnumerable<Type> GetAllTypesFromAllAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes());
        }
    }
}