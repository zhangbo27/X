﻿using System.Reflection;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Reflection;

namespace System.Collections.Generic
{
    /// <summary>集合扩展</summary>
    public static class CollectionHelper
    {
        /// <summary>集合转为数组</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static T[] ToArray<T>(this ICollection<T> collection, Int32 index = 0)
        {
            if (collection == null) return null;

            var count = collection.Count;
            if (count == 0) return new T[0];
            lock (collection)
            {
                count = collection.Count;
                if (count == 0) return new T[0];

                var arr = new T[count - index];
                collection.CopyTo(arr, index);

                return arr;
            }
        }

        /// <summary>集合转为数组</summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="collection"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static TKey[] ToKeyArray<TKey, TValue>(this IDictionary<TKey, TValue> collection, Int32 index = 0)
        {
            if (collection == null) return null;

            if (collection.Count == 0) return new TKey[0];
            lock (collection)
            {
                var arr = new TKey[collection.Count - index];
                collection.Keys.CopyTo(arr, index);
                return arr;
            }
        }

        /// <summary>集合转为数组</summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="collection"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static TValue[] ToValueArray<TKey, TValue>(this IDictionary<TKey, TValue> collection, Int32 index = 0)
        {
            if (collection == null) return null;

            if (collection.Count == 0) return new TValue[0];
            lock (collection)
            {
                var arr = new TValue[collection.Count - index];
                collection.Values.CopyTo(arr, index);
                return arr;
            }
        }

        /// <summary>目标匿名参数对象转为字典</summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static IDictionary<String, Object> ToDictionary(this Object target)
        {
            var dic = target as IDictionary<String, Object>;
            if (dic != null) return dic;

            dic = new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
            if (target != null)
            {
                // 修正字符串字典的支持问题
                var dic2 = target as IDictionary;
                if (dic2 != null)
                {
                    foreach (DictionaryEntry item in dic2)
                    {
                        dic[item.Key + ""] = item.Value;
                    }
                }
                else
                {
                    foreach (var pi in target.GetType().GetProperties())
                    {
                        if (pi.GetIndexParameters().Length > 0) continue;
                        if (pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;

                        dic[pi.Name] = target.GetValue(pi);
                    }
                }
            }

            return dic;
        }

        /// <summary>合并字典参数</summary>
        /// <param name="dic">字典</param>
        /// <param name="target">目标对象</param>
        /// <param name="overwrite">是否覆盖同名参数</param>
        /// <param name="excludes">排除项</param>
        /// <returns></returns>
        public static IDictionary<String, Object> Merge(this IDictionary<String, Object> dic, Object target, Boolean overwrite = true, String[] excludes = null)
        {
            var exs = excludes != null ? new HashSet<String>(StringComparer.OrdinalIgnoreCase) : null;
            foreach (var item in target.ToDictionary())
            {
                if (exs == null || !exs.Contains(item.Key))
                {
                    if (overwrite || !dic.ContainsKey(item.Key)) dic[item.Key] = item.Value;
                }
            }

            return dic;
        }

        /// <summary>转为可空字典</summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="collection"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static IDictionary<TKey, TValue> ToNullable<TKey, TValue>(this IDictionary<TKey, TValue> collection, IEqualityComparer<TKey> comparer = null)
        {
            if (collection == null) return null;

            if (collection is NullableDictionary<TKey, TValue>) return collection as NullableDictionary<TKey, TValue>;

            return new NullableDictionary<TKey, TValue>(collection, comparer);
        }
    }
}