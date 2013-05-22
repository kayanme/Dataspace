using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("Resources.Test")]
namespace Dataspace.Common
{
    internal static class Utilities
    {

        /// <summary>
        /// Возвращает последовательность от 0 до заданного числа -1.
        /// </summary>
        /// <param name="count">Общая длина последовательности.</param>
        /// <remarks>При count = 2 последовательность - [0;1]</remarks>
        /// <returns>Последовательность</returns>
        [DebuggerStepThrough]
        internal static IEnumerable<int> Times(this int count)
        {

            for (int i = 0; i < count; i++)
                yield return i;
        }



        [DebuggerStepThrough]
        public static T CastSingle<T>(this object obj)
        {
            return (T)obj;
        }

        /// <summary>
        /// Выполняет действие над объектом или возвращает значение.
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <typeparam name="K">Тип возвращаемого значения</typeparam>
        /// <param name="obj">Объект.</param>
        /// <param name="func">Действие.</param>
        /// <param name="def">Значение по умолчанию.</param>
        /// <returns>Результат</returns>
        [DebuggerStepThrough]
        public static K ByDefault<T, K>(this T obj, Func<T, K> func, K def)
        {

            return obj != null && !obj.Equals(default(T)) ? func(obj) : def;
        }


        /// <summary>
        /// Строит последовательность объектов, пока удовлетворяется заданное условие.
        /// </summary>
        /// <typeparam name="T">Тип выходного объекта</typeparam>
        /// <param name="obj">Первый объект последовательности</param>
        /// <param name="cond">Условие продолжения.</param>
        /// <param name="act">Функция получения нового объекта из предыдущего.</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static IEnumerable<T> Construct<T>(this T obj, Predicate<T> cond, Func<T, T> act)
        {
            for (var i = obj; cond(i); i = act(i))
                yield return i;
        }


        /// <summary>
        /// Выполняет действие над объектом или возвращает значение.
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <typeparam name="K">Тип возвращаемого значения</typeparam>
        /// <param name="obj">Объект.</param>
        /// <param name="func">Действие.</param>     
        /// <returns>Результат или null, если входной аргумент null</returns>
        [DebuggerStepThrough]
        public static K ByDefault<T, K>(this T obj, Func<T, K> func) where K : class
        {

            return obj != null ? func(obj) : null;
        }



        /// <summary>
        /// Возвращает значение по умолчанию, если объект - null.
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <param name="obj">Объект.</param>
        /// <param name="def">Действие.</param>
        /// <returns>Результат</returns>
        [DebuggerStepThrough]
        public static T ByDefault<T>(this T obj, Func<T> def) where T : class
        {
            return obj ?? def();
        }

    }
}