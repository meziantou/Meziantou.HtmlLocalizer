using System;

namespace Meziantou.HtmlLocalizer
{
    [Flags]
    public enum ExtractFieldOptions
    {
        None = 0x0,
        TrimInnerHtml = 0x1,

        Default = TrimInnerHtml
    }
}