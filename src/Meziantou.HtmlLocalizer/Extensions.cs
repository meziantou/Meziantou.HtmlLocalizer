using System;
using AngleSharp.Dom;
using Meziantou.Framework.Utilities;

namespace Meziantou.HtmlLocalizer
{
    internal static class Extensions
    {
        public static T GetAttribute<T>(this INode node, string name, T defaultValue)
        {
            var element = node as IElement;
            if (element == null)
                return defaultValue;

            return GetAttribute(element, name, defaultValue);
        }

        public static T GetAttribute<T>(this IElement element, string name, T defaultValue)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            return ConvertUtilities.ChangeType(element.GetAttribute(name), defaultValue);
        }
    }
}