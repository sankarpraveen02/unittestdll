﻿/*=============================================================================
*
*	(C) Copyright 2011, Michael Carlisle (mike.carlisle@thecodeking.co.uk)
*
*   http://www.TheCodeKing.co.uk
*  
*	All rights reserved.
*	The code and information is provided "as-is" without waranty of any kind,
*	either expressed or implied.
*
*=============================================================================
*/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using AutoObjectBuilder.Extensions;
using AutoObjectBuilder.Interfaces;

namespace AutoObjectBuilder.Core
{
    internal class AutoBuilder : IAutoBuilder
    {
        private readonly IList<string> constructCache = new List<string>();
        private readonly IDictionary<string, object> cache = new Dictionary<string, object>();
        private readonly IAutoConfigurationResolver configuration;
        private readonly IAutoFiller filler;
        private readonly IAutoBuilder interfaceBuilder;

        internal AutoBuilder(IAutoConfigurationResolver configuration,
                             Func<IAutoConfigurationResolver, IAutoBuilder, IObjectParser, IAutoFiller> filler,
                             IObjectParser parser, IAutoBuilder interfaceBuilder)
        {
            this.filler = filler(configuration, this, parser);
            this.configuration = configuration;
            this.interfaceBuilder = interfaceBuilder;
        }

        #region IAutoBuilder Members

        public T CreateObject<T>()
        {
            return (T) CreateObject(typeof (T));
        }

        public object CreateObject(Type type)
        {
            if (type.IsOfRawGenericTypeDefinition(typeof (IDictionary<,>)))
            {
                var args = type.GetGenericArguments();
                type = typeof (Dictionary<,>).MakeGenericType(args);
                return CreateObject(type);
            }

            if (type.IsOfRawGenericTypeDefinition(typeof (IList<>)))
            {
                type = typeof (List<>).MakeGenericType(type.GetGenericArguments());
                return CreateObject(type);
            }
            if (type.IsOfRawGenericTypeDefinition(typeof (ICollection<>)))
            {
                type = typeof (Collection<>).MakeGenericType(type.GetGenericArguments()[0]);
                return CreateObject(type);
            }

            if (type.IsOfRawGenericTypeDefinition(typeof (IEnumerable<>)))
            {
                return CreateArrayObject(type.GetGenericArguments()[0]);
            }

            if (type.IsArray)
            {
                return CreateArrayObject(type.GetElementType());
            }
            var f = configuration.GetFactory(type);
            if (f != null)
            {
                return f(type);
            }
            if (type.IsEnum)
            {
                f = configuration.GetFactory(typeof (Enum));
                if (f != null)
                {
                    return f(type);
                }
            }
            return GetNewObject(type) ?? type.GetDefault();
        }

        #endregion

        private object CreateArrayObject(Type type)
        {
            if (type == null)
            {
                return null;
            }
            var o = CreateObject(type);
            var arr = Array.CreateInstance(type, configuration.GetEnumerableSize());
            for (var i = 0; i < arr.Length; i++)
            {
                arr.SetValue(o, i);
            }
            return arr;
        }

        private object GetNewObject(Type type)
        {
            var key = type.CreateKey();
            object item;
            if (cache.TryGetValue(key, out item))
            {
                return item;
            }
            object o;
            try
            {
                o = CreateNewObject(type);
            }
            catch (TargetInvocationException)
            {
                o = null;
            }
            finally
            {
                constructCache.Remove(key);
            }

            if (o != null)
            {
                cache[key] = o;
                filler.FillObject(o);
            }
            return o;
        }

        private object CreateNewObject(Type type)
        {
            if (type.IsInterface || type.IsAbstract)
            {
                return interfaceBuilder.CreateObject(type);
            }

            // if recursing within constructor, attempt to return 
            // unconstructed instance or null
            var key = type.CreateKey();
            if (constructCache.Contains(key))
            {
                return ConstructorlessType(type);
            }

            // prevent constructor recursion, further attempts to generate this type
            // within it's constructor return null
            constructCache.Add(key);

            // try public constructors
            var arr = type.GetConstructors().Where(c => ConstructorDoesNotContainType(c, type)).Select(p => p).ToArray();
            if (arr.Length != 0)
            {
                // try construct using least parameters
                var info = arr.OrderBy(o => GetConstructorParamsWeighting(o.GetParameters())).ElementAtOrDefault(0);
                if (info != null)
                {
                    var pArr = info.GetParameters();
                    var args = pArr.Select(p => CreateObject(p.ParameterType)).ToArray();
                    return info.Invoke(args);
                }
            }

            // try non-public constructors
            arr = type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Where(
                c => ConstructorDoesNotContainType(c, type)).Select(p => p).ToArray();
            if (arr.Length != 0)
            {
                // try construct using least parameters
                var info = arr.OrderBy(o => GetConstructorParamsWeighting(o.GetParameters())).ElementAtOrDefault(0);
                if (info != null)
                {
                    var pArr = info.GetParameters();
                    var args = pArr.Select(p => CreateObject(p.ParameterType)).ToArray();
                    return info.Invoke(args);
                }
            }

            // initialize without constructor
            return ConstructorlessType(type);
        }

        private object ConstructorlessType(Type type)
        {
            // initialize without constructor
            return FormatterServices.GetUninitializedObject(type);
        }

        // 
        private int GetConstructorParamsWeighting(ParameterInfo[] parameterInfo)
        {
            // prefer empty constructors
            if (parameterInfo.Length == 0)
            {
                return -1;
            }
            return parameterInfo.Select(p => p.ParameterType.IsPrimitive ? 0 : 1).Sum();
        }

        private static bool ConstructorDoesNotContainType(ConstructorInfo info, Type type)
        {
            // avoid recursion and avoid initializing objects with Pointers (can cause unhandled exceptions)
            return
                info.GetParameters().ToList().Find(p => p.ParameterType == type || p.ParameterType == typeof (IntPtr)) ==
                null;
        }
    }
}