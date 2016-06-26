using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Meziantou.HtmlLocalizer
{
    public class FieldCollection : IList<Field>
    {
        private readonly IList<Field> _fields = new List<Field>();

        public Field this[string name]
        {
            get { return FindField(name); }
        }

        public IEnumerator<Field> GetEnumerator()
        {
            return _fields.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _fields).GetEnumerator();
        }

        public void Add(Field item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            _fields.Add(item);
        }

        public void Clear()
        {
            _fields.Clear();
        }

        public bool Contains(Field item)
        {
            return _fields.Contains(item);
        }

        public void CopyTo(Field[] array, int arrayIndex)
        {
            _fields.CopyTo(array, arrayIndex);
        }

        public bool Remove(Field item)
        {
            return _fields.Remove(item);
        }

        public int Count
        {
            get { return _fields.Count; }
        }

        public bool IsReadOnly
        {
            get { return _fields.IsReadOnly; }
        }

        public int IndexOf(Field item)
        {
            return _fields.IndexOf(item);
        }

        public void Insert(int index, Field item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            _fields.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _fields.RemoveAt(index);
        }

        public Field this[int index]
        {
            get { return _fields[index]; }
            set { _fields[index] = value; }
        }

        private Field FindField(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            return _fields.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.Ordinal));
        }
    }
}