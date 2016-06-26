using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Meziantou.HtmlLocalizer
{
    internal class ProjectContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (property.DeclaringType == typeof(Field) && property.PropertyName == nameof(Field.Values))
            {
                property.ShouldSerialize = instance => !((Field) instance).IsReference;
            }

            return property;
        }
    }
}