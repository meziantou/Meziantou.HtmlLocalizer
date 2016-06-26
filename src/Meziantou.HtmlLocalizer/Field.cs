using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace Meziantou.HtmlLocalizer
{
    public class Field
    {
        private const char ReferenceSeparator = '#';

        [JsonIgnore]
        public HtmlFile File { get; set; }

        public string Name { get; set; }

        [DefaultValue(-1)]
        public int SortOrder { get; set; } = -1;

        public string SourceHtml { set; get; }
        public IDictionary<string, IDictionary<string, string>> Values { get; } = new Dictionary<string, IDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        [DefaultValue(true)]
        public bool Exists { get; set; } = true;

        [JsonIgnore]
        public bool IsReference
        {
            get { return Name.IndexOf(ReferenceSeparator) >= 0; }
        }

        public Field ResolveReference()
        {
            var index = Name.IndexOf(ReferenceSeparator);
            if (index < 0)
                return null;

            var path = Name.Substring(0, index);
            var fieldName = Name.Substring(index + 1);

            FieldCollection fields = null;
            if (!string.IsNullOrEmpty(path))
            {
                HtmlFile file = null;
                if (File?.Project != null)
                {
                    // TODO handle relative path "../../SR.html"
                    file = File.Project.Files.FirstOrDefault(f => f.Path == path);
                }

                if (file == null)
                    return null;

                fields = file.Fields;
            }
            else
            {
                fields = File?.Fields;
            }

            if (fields == null)
                return null;

            return fields[fieldName];
        }

        public void SetAttributeValue(CultureInfo cultureInfo, string name, string value)
        {
            if (cultureInfo == null) throw new ArgumentNullException(nameof(cultureInfo));
            if (name == null) throw new ArgumentNullException(nameof(name));

            IDictionary<string, string> values;
            if (!Values.TryGetValue(name, out values))
            {
                values = new Dictionary<string, string>();
                Values.Add(name, values);
            }

            values[cultureInfo.Name] = value;
        }

        public bool TryGetAttributeValue(CultureInfo cultureInfo, string name, out string value)
        {
            if (cultureInfo == null) throw new ArgumentNullException(nameof(cultureInfo));
            if (name == null) throw new ArgumentNullException(nameof(name));

            IDictionary<string, string> values;
            if (!Values.TryGetValue(name, out values))
            {
                value = null;
                return false;
            }
            
            if (!values.TryGetValue(cultureInfo.Name, out value))
                return false;

            return true;
        }

        public void MergeValues(Field field)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));

            foreach (var attribute in field.Values)
            {
                IDictionary<string, string> localizations;
                if (!Values.TryGetValue(attribute.Key, out localizations))
                {
                    localizations = new Dictionary<string, string>();
                    Values.Add(attribute.Key, localizations);
                }

                foreach (var l in attribute.Value)
                {
                    localizations[l.Key] = l.Value;
                }
            }
        }
    }
}