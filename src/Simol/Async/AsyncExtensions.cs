/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using Coditate.Common.Util;

namespace Simol.Async
{
    /// <summary>
    /// Defines extension methods for calling Simol asynchronously.
    /// </summary>
    /// <remarks>
    /// See <see cref="SimolClient"/> for a usage example.
    /// </remarks>
    public static class AsyncExtensions
    {
        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.Put"/>.
        /// </summary>
        public static IAsyncResult BeginPut(this ISimol simol, object item, AsyncCallback callback,
                                            object state)
        {
            return simol.BeginAction(s => s.Put(item), callback, state);
        }

        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.Put{T}(List{T})"/>.
        /// </summary>
        public static IAsyncResult BeginPut<T>(this ISimol simol, List<T> items, AsyncCallback callback,
                                    object state)
        {
            return simol.BeginAction(s => s.Put(items), callback, state);
        }

        /// <summary>
        /// End asynchronous invocation of <see cref="ISimol.Put"/>.
        /// </summary>
        public static void EndPut(this ISimol simol, IAsyncResult result)
        {
            EndAction<ISimol>(result);
        }

        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.Get{T}"/>.
        /// </summary>
        public static IAsyncResult BeginGet<T>(this ISimol simol, object itemName, AsyncCallback callback,
                                               object state)
        {
            return simol.BeginFunction(s => s.Get<T>(itemName), callback, state);
        }

        /// <summary>
        /// End asynchronous invocation of <see cref="ISimol.Get{T}"/>.
        /// </summary>
        public static T EndGet<T>(this ISimol simol, IAsyncResult result)
        {
            return EndFunction<ISimol, T>(result);
        }

        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.GetAttributes{T}"/>.
        /// </summary>
        public static IAsyncResult BeginGetAttributes<T>(this ISimol simol, object itemName,
                                                         string[] propertyNames, AsyncCallback callback,
                                                         object state)
        {
            return simol.BeginFunction(s => s.GetAttributes<T>(itemName, propertyNames), callback, state);
        }

        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.GetAttributes"/>.
        /// </summary>
        public static IAsyncResult BeginGetAttributes(this ISimol simol, ItemMapping mapping, object itemName,
                                                      string[] propertyNames, AsyncCallback callback,
                                                      object state)
        {
            return simol.BeginFunction(s => s.GetAttributes(mapping, itemName, propertyNames), callback, state);
        }

        /// <summary>
        /// End asynchronous invocation of <see cref="ISimol.GetAttributes"/>.
        /// </summary>
        public static PropertyValues EndGetAttributes(this ISimol simol, IAsyncResult result)
        {
            return EndFunction<ISimol, PropertyValues>(result);
        }

        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.DeleteAttributes(ItemMapping,object,string[])"/>.
        /// </summary>
        public static IAsyncResult BeginDeleteAttributes(this ISimol simol, ItemMapping mapping,
                                                         object itemName,
                                                         string[] propertyNames, AsyncCallback callback,
                                                         object state)
        {
            return simol.BeginAction(s => s.DeleteAttributes(mapping, itemName, propertyNames), callback, state);
        }

        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.DeleteAttributes(ItemMapping,object,string[])"/>.
        /// </summary>
        public static IAsyncResult BeginDeleteAttributes(this ISimol simol, ItemMapping mapping,
                                                         List<object> itemNames,
                                                         string[] propertyNames, AsyncCallback callback,
                                                         object state)
        {
            return simol.BeginAction(s => s.DeleteAttributes(mapping, itemNames, propertyNames), callback, state);
        }

        /// <summary>
        /// End asynchronous invocation of <see cref="ISimol.DeleteAttributes(ItemMapping,object,string[])"/>.
        /// </summary>
        public static void EndDeleteAttributes(this ISimol simol, IAsyncResult result)
        {
            EndAction<ISimol>(result);
        }

        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.SelectAttributes"/>.
        /// </summary>
        public static IAsyncResult BeginSelectAttributes(this ISimol simol, SelectCommand command,
                                                         AsyncCallback callback,
                                                         object state)
        {
            return simol.BeginFunction(s => s.SelectAttributes(command), callback, state);
        }

        /// <summary>
        /// End asynchronous invocation of <see cref="ISimol.SelectAttributes"/>.
        /// </summary>
        public static SelectResults<PropertyValues> EndSelectAttributes(this ISimol simol, IAsyncResult result)
        {
            return EndFunction<ISimol, SelectResults<PropertyValues>>(result);
        }

        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.SelectScalar"/>.
        /// </summary>
        public static IAsyncResult BeginSelectScalar(this ISimol simol, SelectCommand command,
                                                     AsyncCallback callback,
                                                     object state)
        {
            return simol.BeginFunction(s => s.SelectScalar(command), callback, state);
        }

        /// <summary>
        /// End asynchronous invocation of <see cref="ISimol.SelectScalar"/>.
        /// </summary>
        public static object EndSelectScalar(this ISimol simol, IAsyncResult result)
        {
            return EndFunction<ISimol, object>(result);
        }

        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.PutAttributes{T}(List{PropertyValues})"/>.
        /// </summary>
        public static IAsyncResult BeginPutAttributes<T>(this ISimol simol, List<PropertyValues> items,
                                                         AsyncCallback callback,
                                                         object state)
        {
            return simol.BeginAction(s => s.PutAttributes<T>(items), callback, state);
        }

        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.PutAttributes{T}(PropertyValues)"/>.
        /// </summary>
        public static IAsyncResult BeginPutAttributes<T>(this ISimol simol, PropertyValues item,
                                                         AsyncCallback callback,
                                                         object state)
        {
            return simol.BeginAction(s => s.PutAttributes<T>(item), callback, state);
        }

        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.PutAttributes(ItemMapping,List{PropertyValues})"/>.
        /// </summary>
        public static IAsyncResult BeginPutAttributes(this ISimol simol, ItemMapping mapping,
                                                      List<PropertyValues> items,
                                                      AsyncCallback callback,
                                                      object state)
        {
            return simol.BeginAction(s => s.PutAttributes(mapping, items), callback, state);
        }

        /// <summary>
        /// End asynchronous invocation of <see cref="ISimol.PutAttributes(ItemMapping,List{PropertyValues})"/>.
        /// </summary>
        public static void EndPutAttributes(this ISimol simol, IAsyncResult result)
        {
            EndAction<ISimol>(result);
        }

        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.Delete{T}(object)"/>.
        /// </summary>
        public static IAsyncResult BeginDelete<T>(this ISimol simol, object itemName,
                                                  AsyncCallback callback,
                                                  object state)
        {
            return simol.BeginAction(s => s.Delete<T>(itemName), callback, state);
        }

        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.Delete{T}(IList)"/>.
        /// </summary>
        public static IAsyncResult BeginDelete<T>(this ISimol simol, List<object> itemNames,
                                                  AsyncCallback callback,
                                                  object state)
        {
            return simol.BeginAction(s => s.Delete<T>(itemNames), callback, state);
        }

        /// <summary>
        /// End asynchronous invocation of <see cref="ISimol.Delete{T}(object)"/>.
        /// </summary>
        public static void EndDelete(this ISimol simol, IAsyncResult result)
        {
            EndAction<ISimol>(result);
        }

        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.DeleteAttributes{T}(object,string[])"/>.
        /// </summary>
        public static IAsyncResult BeginDeleteAttributes<T>(this ISimol simol, object itemName,
                                                            string[] propertyNames,
                                                            AsyncCallback callback,
                                                            object state)
        {
            return simol.BeginAction(s => s.DeleteAttributes<T>(itemName, propertyNames), callback, state);
        }

        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.DeleteAttributes{T}(object,string[])"/>.
        /// </summary>
        public static IAsyncResult BeginDeleteAttributes<T>(this ISimol simol, List<object> itemNames,
                                                            string[] propertyNames,
                                                            AsyncCallback callback,
                                                            object state)
        {
            return simol.BeginAction(s => s.DeleteAttributes<T>(itemNames, propertyNames), callback, state);
        }

        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.SelectAttributes{T}(SelectCommand{T})"/>.
        /// </summary>
        public static IAsyncResult BeginSelectAttributes<T>(this ISimol simol, SelectCommand<T> command,
                                                            AsyncCallback callback,
                                                            object state)
        {
            return simol.BeginFunction(s => s.SelectAttributes(command), callback, state);
        }

        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.Select{T}(SelectCommand{T})"/>.
        /// </summary>
        public static IAsyncResult BeginSelect<T>(this ISimol simol, SelectCommand<T> command,
                                                  AsyncCallback callback,
                                                  object state)
        {
            return simol.BeginFunction(s => s.Select(command), callback, state);
        }

        /// <summary>
        /// End asynchronous invocation of <see cref="ISimol.Select{T}(SelectCommand{T})"/>.
        /// </summary>
        public static SelectResults<T> EndSelect<T>(this ISimol simol, IAsyncResult result)
        {
            return EndFunction<ISimol, SelectResults<T>>(result);
        }

        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.SelectScalar{T}(string, CommandParameter[])"/>.
        /// </summary>
        public static IAsyncResult BeginSelectScalar<T>(this ISimol simol, string selectStatement,
                                                        CommandParameter[] selectParams,
                                                        AsyncCallback callback,
                                                        object state)
        {
            return simol.BeginFunction(s => s.SelectScalar<T>(selectStatement, selectParams), callback, state);
        }

        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.Find{T}"/>.
        /// </summary>
        public static IAsyncResult BeginFind<T>(this ISimol simol, string queryText, int resultStartIndex,
                                                int resultCount, string searchProperty,
                                                AsyncCallback callback,
                                                object state)
        {
            return simol.BeginFunction(s => s.Find<T>(queryText, resultStartIndex, resultCount, searchProperty),
                                        callback, state);
        }

        /// <summary>
        /// End asynchronous invocation of <see cref="ISimol.Find{T}"/>.
        /// </summary>
        public static List<T> EndFind<T>(this ISimol simol, IAsyncResult result)
        {
            return EndFunction<ISimol, List<T>>(result);
        }

        /// <summary>
        /// Begin asynchronous invocation of <see cref="ISimol.FindAttributes{T}"/>.
        /// </summary>
        public static IAsyncResult BeginFindAttributes<T>(this ISimol simol, string queryText,
                                                          int resultStartIndex, int resultCount, string searchProperty,
                                                          string[] propertyNames,
                                                          AsyncCallback callback,
                                                          object state)
        {
            return
                simol.BeginFunction(
                    s => s.FindAttributes<T>(queryText, resultStartIndex, resultCount, searchProperty, propertyNames),
                    callback, state);
        }

        /// <summary>
        /// End asynchronous invocation of <see cref="ISimol.FindAttributes{T}"/>.
        /// </summary>
        public static List<PropertyValues> EndFindAttributes(this ISimol simol, IAsyncResult result)
        {
            return EndFunction<ISimol, List<PropertyValues>>(result);
        }

        private static IAsyncResult BeginAction<T>(this T simol, Action<T> action,
                                                   AsyncCallback callback,
                                                   object state)
        {
            return action.BeginInvoke(simol, callback, state);
        }

        private static IAsyncResult BeginFunction<T, R>(this T simol, Func<T, R> func,
                                                        AsyncCallback callback,
                                                        object state)
        {
            return func.BeginInvoke(simol, callback, state);
        }

        private static void EndAction<T>(IAsyncResult result)
        {
            var resultImpl = (AsyncResult) result;
            var action = resultImpl.AsyncDelegate as Action<T>;
            if (action == null)
            {
                throw new InvalidOperationException(
                    "The provided IAsyncResult was returned from a Begin method that does not match the current End method.");
            }
            action.EndInvoke(result);
        }

        private static R EndFunction<T, R>(IAsyncResult result)
        {
            var resultImpl = (AsyncResult) result;
            var func = resultImpl.AsyncDelegate as Func<T, R>;
            if (func == null)
            {
                throw new InvalidOperationException(
                    "The provided IAsyncResult was returned from a Begin method that does not match the current End method.");
            }
            return func.EndInvoke(result);
        }
    }
}