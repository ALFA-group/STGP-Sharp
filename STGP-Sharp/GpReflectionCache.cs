#nullable enable

#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using STGP_Sharp.Utilities.GeneralCSharp;

#endregion

namespace STGP_Sharp
{
    public static class GpReflectionCache
    {
        private static readonly ConcurrentDictionary<(Type superType, Type subType), bool> DCacheIsSubclass =
            new ConcurrentDictionary<(Type superType, Type subType), bool>();


        private static readonly ConcurrentDictionary<Type, List<Type>> DCacheGetAllSubTypes =
            new ConcurrentDictionary<Type, List<Type>>();

        private static readonly ConcurrentDictionary<Type, Type> DCacheGetReturnTypeFromGpBuildingBlockSubClass =
            new ConcurrentDictionary<Type, Type>();

        public static bool IsSubclass(Type superType, Type subType)
        {
            (Type superType, Type subType) key = (superType, subType);
            if (DCacheIsSubclass.TryGetValue(key, out bool isSubclass))
            {
                return isSubclass;
            }

            if ((subType.IsGenericType && subType.GetGenericTypeDefinition() == superType) ||
                subType.BaseType == superType)
            {
                DCacheIsSubclass[key] = true;
                return true;
            }

            isSubclass =
                subType.BaseType != null &&
                IsSubclass(superType, subType.BaseType); //&& GpRunner.IsSubclassOfGpBuildingBlock(subType.BaseType);
            DCacheIsSubclass[key] = isSubclass;
            return isSubclass;
        }

        public static IEnumerable<Type> GetAllSubTypes(Type parentType)
        {
            // if (!genericType.IsGenericTypeDefinition)
            //     throw new ArgumentException("Specified type must be a generic type definition.", nameof(genericType));

            if (DCacheGetAllSubTypes.TryGetValue(parentType, out List<Type>? subTypes))
            {
                return subTypes;
            }

            subTypes = ReflectionUtilities.GetAllTypesFromAllAssemblies()
                .Where(t =>
                    t.BaseType != null &&
                    t != parentType &&
                    IsSubclass(parentType, t))
                .ToList();

            DCacheGetAllSubTypes[parentType] = subTypes;
            return subTypes;
        }

        public static Type GetReturnTypeFromGpBuildingBlockSubClass(Type type)
        {
            if (DCacheGetReturnTypeFromGpBuildingBlockSubClass.TryGetValue(type, out Type? returnType))
            {
                return returnType;
            }

            returnType = Internal_GetReturnTypeFromGpBuildingBlockSubClass(type);
            DCacheGetReturnTypeFromGpBuildingBlockSubClass[type] = returnType;
            return returnType;
        }

        private static Type Internal_GetReturnTypeFromGpBuildingBlockSubClass(Type type)
        {
            Type? parentType = type;

            while (null != parentType)
            {
                Type[] templateParameters = parentType.GetGenericArguments();
                if (templateParameters.Length > 0)
                {
                    return templateParameters[0];
                }

                parentType = parentType.BaseType;
            }

            throw new Exception($"Type {type.Name} does not descend from a generic type");
        }
    }
}