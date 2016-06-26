using System;
using System.Collections.Generic;

namespace Meziantou.HtmlLocalizer
{
    public class ProjectOptions
    {
        // https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes

        public static readonly string[] DefaultLocalizableAttributes = { "title", "alt", "src", "srcset", "href", "placeholder" };

        public ProjectOptions()
        {
            LocalizableAttributes = new List<string> { "title", "alt", "src", "srcset", "href", "placeholder" };
            LocalizableAttributesByTag = new Dictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "meta", new List<string> { "content" } }
            };
        }

        public IList<string> LocalizableAttributes { get; set; }

        public IDictionary<string, IList<string>> LocalizableAttributesByTag { get; set; }
    }
}