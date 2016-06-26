using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Extensions;
using AngleSharp.Parser.Html;
using Newtonsoft.Json;

namespace Meziantou.HtmlLocalizer
{
    public class HtmlFile
    {
        private const string LocalizableNamespacePrefix = "loc:";
        private const string InnerTextOrInnerHtmlAttributeName = "innerTextOrInnerHtml";
        private const string InnerTextAttributeName = "innerText";
        private const string InnerHtmlAttributeName = "innerHtml";

        [JsonIgnore]
        public Project Project { get; set; }

        public string Path { get; set; }
        public FileOptions Options { get; set; }

        public FieldCollection Fields { get; } = new FieldCollection();

        [JsonIgnore]
        public INodeList Nodes { get; set; }

        public void LoadHtml(string html)
        {
            if (html == null) throw new ArgumentNullException(nameof(html));

            Nodes = ParseHtml(html);
        }

        public void LoadHtml()
        {
            if (string.IsNullOrEmpty(Path))
                return;

            string fullPath;
            if (Project?.BaseDirectory != null)
            {
                fullPath = System.IO.Path.Combine(Project.BaseDirectory.FullName, Path);
            }
            else
            {
                fullPath = Path;
            }

            if (!File.Exists(fullPath))
                return;

            var html = File.ReadAllText(fullPath);
            LoadHtml(html);
        }

        protected virtual INodeList ParseHtml(string html)
        {
            var parser = new HtmlParser();
            var fragment = parser.ParseFragment(html, null);
            if (fragment.Length == 1)
            {
                var htmlElement = fragment[0] as IHtmlHtmlElement;
                if (htmlElement != null)
                {
                    if (HasLocalizationAttribute(htmlElement))
                        return fragment;

                    if (htmlElement.ChildElementCount != 2)
                        return fragment;
                    
                    var headElement = htmlElement.QuerySelector("head");
                    if (headElement != null && headElement.HasChildNodes)
                        return fragment;

                    var bodyElement = htmlElement.QuerySelector("body");
                    if (bodyElement == null || HasLocalizationAttribute(bodyElement))
                        return fragment;

                    return bodyElement.ChildNodes;
                }
            }

            return fragment;
        }

        public virtual void ExtractFields()
        {
            foreach (var field in Fields)
            {
                field.Exists = false;
            }

            var sortOrder = 0;
            foreach (var node in Nodes)
            {
                var fields = ExtractFields(node, CultureInfo.InvariantCulture);
                foreach (var field in fields)
                {
                    field.SortOrder = sortOrder++;

                    var existingField = Fields[field.Name];
                    if (existingField != null)
                    {
                        existingField.Exists = true;
                        existingField.SortOrder = field.SortOrder;
                        existingField.MergeValues(field);
                    }
                    else
                    {
                        Fields.Add(field);
                    }
                }
            }
        }

        protected virtual IEnumerable<Field> ExtractFields(INode node, CultureInfo culture)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            var nodes = new Queue<INode>();
            nodes.Enqueue(node);

            while (nodes.Any())
            {
                var currentNode = nodes.Dequeue();
                Options = currentNode.GetAttribute(LocalizableNamespacePrefix + "fileOptions", Options);
                var field = ExtractField(currentNode, culture);
                if (field != null)
                {
                    yield return field;
                }

                if (currentNode.HasChildNodes)
                {
                    foreach (var child in currentNode.ChildNodes)
                    {
                        nodes.Enqueue(child);
                    }
                }
            }
        }

        private string GetFieldName(IElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            return element.GetAttribute(LocalizableNamespacePrefix + "name");
        }

        protected virtual Field ExtractField(INode node, CultureInfo culture)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (culture == null) throw new ArgumentNullException(nameof(culture));

            var element = node as IElement;
            if (element == null)
                return null;

            var name = GetFieldName(element);
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var field = new Field();
            field.File = this;
            field.Name = name;
            field.SourceHtml = GetSourceHtml(element);

            var options = element.GetAttribute(LocalizableNamespacePrefix + "options", ExtractFieldOptions.Default);
            var attributes = GetLocalizableAttributeNames(element);
            if (attributes != null)
            {
                foreach (var attributeName in attributes)
                {
                    var actualAttributeName = attributeName;
                    if (attributeName == InnerTextOrInnerHtmlAttributeName)
                    {
                        if (element.ChildElementCount == 0)
                        {
                            actualAttributeName = InnerTextAttributeName;
                        }
                        else
                        {
                            actualAttributeName = InnerHtmlAttributeName;
                        }
                    }

                    if (actualAttributeName == InnerHtmlAttributeName)
                    {
                        var value = element.InnerHtml;
                        if (value != null && options.HasFlag(ExtractFieldOptions.TrimInnerHtml))
                        {
                            value = value.Trim();
                        }

                        field.SetAttributeValue(culture, actualAttributeName, value);
                    }
                    else if (actualAttributeName == InnerTextAttributeName)
                    {
                        var value = element.TextContent;
                        if (value != null && options.HasFlag(ExtractFieldOptions.TrimInnerHtml))
                        {
                            value = value.Trim();
                        }

                        field.SetAttributeValue(culture, actualAttributeName, value);
                    }
                    else
                    {
                        var value = element.GetAttribute(actualAttributeName);
                        if (value != null)
                        {
                            field.SetAttributeValue(culture, actualAttributeName, value);
                        }
                    }
                }
            }

            return field;
        }

        protected virtual string GetSourceHtml(INode node)
        {
            var element = node as IElement;
            if (element == null)
                return null;

            element = node.Clone() as IElement;
            if (element == null)
                return null;

            RemoveLocalizationAttributes(element);
            return element.OuterHtml;
        }

        private bool HasLocalizationAttribute(INode node)
        {
            var element = node as IElement;
            if (element == null)
                return false;

            foreach (var attribute in element.Attributes)
            {
                if (attribute.Name.StartsWith(LocalizableNamespacePrefix))
                    return true;
            }

            return false;
        }

        protected virtual IEnumerable<string> GetLocalizableAttributeNames(IElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            var attributes = element.GetAttribute<string>(LocalizableNamespacePrefix + "attributes", null);
            if (attributes != null)
            {
                var attributeNames = attributes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(_ => _.Trim());
                return attributeNames;
            }

            IList<string> attributesByTag = null;
            if (Project?.Options.LocalizableAttributesByTag.TryGetValue(element.TagName, out attributesByTag) == true)
                return attributesByTag;

            IEnumerable<string> defaultLocalizableAttributes = Project?.Options.LocalizableAttributes ?? ProjectOptions.DefaultLocalizableAttributes;
            if (IsVoidElement(element))
                return defaultLocalizableAttributes;

            return Enumerable.Prepend(defaultLocalizableAttributes, InnerTextOrInnerHtmlAttributeName);
        }

        private bool IsVoidElement(IElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            string[] voidElements = { "area", "base", "br", "col", "embed", "hr", "img", "input", "keygen", "link", "meta", "param", "source", "track", "wbr" };

            return voidElements.Contains(element.TagName, StringComparer.OrdinalIgnoreCase);
        }

        private void RemoveLocalizationAttributes(IElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            foreach (var attribute in element.Attributes.ToList())
            {
                if (attribute.Name.StartsWith(LocalizableNamespacePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    element.RemoveAttribute(attribute.Name);
                }
            }
        }

        public virtual bool CanLocalizeFile()
        {
            return (Options & FileOptions.ReferencesOnly) != FileOptions.ReferencesOnly;
        }

        public virtual string Localize(CultureInfo cultureInfo)
        {
            if (!CanLocalizeFile())
                return null;

            var sb = new StringBuilder();
            foreach (var node in Nodes)
            {
                sb.Append(Localize(node, cultureInfo));
            }

            return sb.ToString();
        }

        protected virtual string Localize(INode node, CultureInfo cultureInfo)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (cultureInfo == null) throw new ArgumentNullException(nameof(cultureInfo));

            node = node.Clone(); // Do not change the original node

            var nodes = new Queue<INode>();
            nodes.Enqueue(node);

            while (nodes.Any())
            {
                var currentNode = nodes.Dequeue();
                var element = currentNode as IElement;
                if (element != null)
                {
                    LocalizeElement(cultureInfo, element);

                    RemoveLocalizationAttributes(element);
                }

                // Transform children
                if (currentNode.HasChildNodes)
                {
                    foreach (var child in currentNode.ChildNodes)
                    {
                        nodes.Enqueue(child);
                    }
                }
            }

            return node.ToHtml();
        }

        private void LocalizeElement(CultureInfo cultureInfo, IElement element)
        {
            var fieldName = GetFieldName(element);
            if (fieldName == null)
                return;

            var field = Fields[fieldName];
            if (field == null)
                return;

            if (field.IsReference)
            {
                field = field.ResolveReference();
                if (field == null)
                    return;
            }

            foreach (var attributeName in field.Values.Keys)
            {
                if (!CanLocalizeAttribute(element, attributeName))
                    continue;

                string localizedValue;
                if (field.TryGetAttributeValue(cultureInfo, attributeName, out localizedValue))
                {
                    if (attributeName == InnerHtmlAttributeName)
                    {
                        element.InnerHtml = localizedValue;
                    }
                    else if (attributeName == InnerTextAttributeName)
                    {
                        element.TextContent = localizedValue;
                    }
                    else
                    {
                        element.SetAttribute(attributeName, localizedValue);
                    }
                }
            }
        }

        protected virtual bool CanLocalizeAttribute(IElement element, string attributeName)
        {
            return attributeName == InnerHtmlAttributeName ||
                   attributeName == InnerTextAttributeName ||
                   element.GetAttribute(attributeName) != null;
        }
    }
}