//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    /// <summary>
    /// Provides a sorted collection of associated String keys and Object values that can be accessed either with the key or with the index.
    /// </summary>
    public class NameObjectCollection
    {
        #region struct NameValuePair

        [DebuggerDisplay("{Name}:{Value}")]        
        internal struct NameValuePair
        {
            private string name;
            private object value;

            internal NameValuePair(string name)
            {
                this.name = name;
                this.value = null;
            }

            internal NameValuePair(string name, object value)
            {
                this.name = name;
                this.value = ConvertValue(value);
            }

            internal string Name
            {
                get { return this.name; }
            }

            internal object Value
            {
                get { return this.value; }
                set { this.value = ConvertValue(value); }
            }

            public override bool Equals(object obj)
            {
                if (obj is NameValuePair)
                {
                    return Equals((NameValuePair)obj);
                }
                return false;
            }

            public bool Equals(NameValuePair other)
            {
                return Name == other.Name;
            }

            public override int GetHashCode()
            {
                return this.name.GetHashCode();
            }

            public static bool operator ==(NameValuePair p1, NameValuePair p2)
            {
                return p1.Equals(p2);
            }

            public static bool operator !=(NameValuePair p1, NameValuePair p2)
            {
                return !p1.Equals(p2);
            }

            private static object ConvertValue(object value)
            {
                // Depending of whether values come from a DataTable or 
                // a data reader, some property values can be either enum values or integers. 
                // For example the value of LoginType can be either SqlLogin or 1.
                // Enums cause a problems because they gets converted to a string rather than an
                // integer value in OE expression evaluation code. 
                // Since originally all the values came from data tables and the rest of OE code expects
                // integers we are going to conver any enum valus to integers here
                if (value != null && value.GetType().IsEnum)
                {
                    value = Convert.ToInt32(value);
                }

                return value;
            }
        }
        #endregion

        #region Property

        internal class Property : ISfcProperty
        {
            private NameValuePair pair;

            internal Property(NameValuePair pair)
            {
                this.pair = pair;
            }

            #region ISfcProperty implementation
            /// <summary>
            /// Name of property
            /// </summary>
            public string Name
            {
                get { return pair.Name; }
            }

            /// <summary>
            /// Type of property
            /// </summary>
            public Type Type
            {
                get { return pair.Value.GetType(); }
            }

            /// <summary>
            /// Check whether the value is enabled or not
            /// </summary>
            public bool Enabled
            {
                get { return true; }
            }

            /// <summary>
            /// Value of property
            /// </summary>
            public object Value
            {
                get { return pair.Value; }
                set { throw new NotSupportedException(); }
            }

            /// <summary>
            /// Indicates whether the property is required to persist the current state of the object
            /// </summary>
            public bool Required
            {
                get { return false; }
            }

            /// <summary>
            /// Indicates that Consumer should be theat this property as read-only
            /// </summary>
            public bool Writable
            {
                get { return false; }
            }

            /// <summary>
            /// Indicates whether the property value has been changed.
            /// </summary>
            public bool Dirty
            {
                get { return false; }
            }

            /// <summary>
            /// Indicates whether the properties data has been read, and is null
            /// </summary>
            public bool IsNull
            {
                get { return pair.Value == null || pair.Value is DBNull; }
            }

            /// <summary>
            /// Aggregated list of custom attributes associated with property
            /// </summary>
            public AttributeCollection Attributes
            {
                get { return null; }
            }

            #endregion
        }

        #endregion

        private List<NameValuePair> pairs;

        /// <summary>
        /// Initializes a new instance of the NameObjectCollection class that is empty.
        /// </summary>
        public NameObjectCollection()
        {
            this.pairs = new List<NameValuePair>();
        }

        /// <summary>
        /// Initializes a new instance of the NameObjectCollection class that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The approximate number of entries that the NameObjectCollection instance can initially contain.</param>
        public NameObjectCollection(int capacity)
        {
            this.pairs = new List<NameValuePair>(capacity);
        }

        /// <summary>
        /// Adds an entry with the specified key and value into the NameObjectCollection instance.
        /// </summary>
        /// <param name="name">The String key of the entry to add. The key can be null.</param>
        /// <param name="value">The Object value of the entry to add. The value can be null.</param>
        public void Add(string name, object value)
        {
            this.pairs.Add(new NameValuePair(name, value));
        }

        /// <summary>
        /// Removes all entries from the NameObjectCollection instance.
        /// </summary>
        public void Clear()
        {
            this.pairs.Clear();
        }

        /// <summary>
        /// Gets the value of the specified entry from the NameObjectCollection instance. C# indexer
        /// </summary>
        public object this[int index]
        {
            get
            {
                return Get(index);
            }
            set
            {
                Set(index, value);
            }
        }
        
        /// <summary>
        /// Gets the value of the specified entry from the NameObjectCollection instance. C# indexer
        /// </summary>
        public object this[string name]
        {
            get
            {
                return Get(name);
            }
            set
            {
                Set(name, value);
            }
        }
        
        /// <summary>
        /// Gets the value of the specified entry from the NameObjectCollection instance.
        /// </summary>
        /// <param name="index">Gets the value of the entry at the specified index of the NameObjectCollection instance.</param>
        /// <returns>An Object that represents the value of the first entry with the specified key, if found; otherwise null</returns>
        public object Get(int index)
        {
            return this.pairs[index].Value;
        }
        
        /// <summary>
        /// Gets the value of the first entry with the specified key from the NameObjectCollection instance.
        /// </summary>
        /// <param name="name">The String key of the entry to get. The key can be a null reference (Nothing in Visual Basic). </param>
        /// <returns>An Object that represents the value of the first entry with the specified key, if found; otherwise null</returns>
        public object Get(string name)
        {
            int index = IndexOf(name);
            return index >= 0 ? this.pairs[index].Value : null;
        }
        
        /// <summary>
        /// Returns a String array that contains all the keys in the NameObjectCollection instance.
        /// </summary>
        /// <returns>A String array that contains all the keys in the NameObjectCollection instance.</returns>
        public string[] GetAllKeys()
        {
            string[] keys = new string[this.pairs.Count];

            for (int i = 0; i < this.pairs.Count; i++)
            {
                keys[i] = this.pairs[i].Name;
            }

            return keys;
        }
        
        /// <summary>
        /// Returns an array that contains all the values in the NameObjectCollection instance.
        /// </summary>
        /// <returns>An Object array that contains all the values in the NameObjectCollection instance.</returns>
        public object[] GetAllValues()
        {
            object[] values = new object[this.pairs.Count];

            for (int i = 0; i < this.pairs.Count; i++)
            {
                values[i] = this.pairs[i].Value;
            }

            return values;
        }

        /// <summary>
        /// Gets the key of the entry at the specified index of the NameObjectCollection instance.
        /// </summary>
        /// <param name="index">The zero-based index of the key to get. </param>
        /// <returns>A String that represents the key of the entry at the specified index.</returns>
        public string GetKey(int index)
        {
            return this.pairs[index].Name;
        }
        
        /// <summary>
        /// Gets a value indicating whether the NameObjectCollection instance contains entries whose keys are not null.
        /// </summary>
        /// <returns>true if the NameObjectCollection instance contains entries whose keys are not a null reference (Nothing in Visual Basic); otherwise, false.</returns>
        public bool HasKeys()
        {
            return this.pairs.Count > 0;
        }
        
        /// <summary>
        /// Removes the entries with the specified key from the NameObjectCollection instance.
        /// </summary>
        /// <param name="name"></param>
        public void Remove(string name)
        {
            RemoveAt(IndexOf(name));
        }
        
        /// <summary>
        /// Removes the entry at the specified index of the NameObjectCollection instance.
        /// </summary>
        /// <param name="index">The zero-based index of the entry to remove. </param>
        public void RemoveAt(int index)
        {
            this.pairs.RemoveAt(index);
        }
        
        /// <summary>
        /// Sets the value of the entry at the specified index of the NameObjectCollection instance.
        /// </summary>
        /// <param name="index">The zero-based index of the entry to set.</param>
        /// <param name="value">The Object that represents the new value of the entry to set. The value can be null.</param>
        public void Set(int index, object value)
        {
            NameValuePair pair = this.pairs[index];
            pair.Value = value;
            this.pairs[index] = pair;
        }
        
        /// <summary>
        /// Sets the value of the first entry with the specified key in the NameObjectCollection instance, if found; otherwise, adds an entry with the specified key and value into the NameObjectCollection instance.
        /// </summary>
        /// <param name="name">The String key of the entry to set. The key can be null.</param>
        /// <param name="value">The Object that represents the new value of the entry to set. The value can be null.</param>
        public void Set(string name, object value)
        {
            int index = IndexOf(name);
            if (index >= 0)
            {
                Set(index, value);
            }
            else
            {
                Add(name, value);
            }
        }
        
        /// <summary>
        /// Copies elements of this collection to an Array starting at a particular array index
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the elements copied from NameObjectCollection. The Array must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in array at which copying begins.</param>
        public void CopyTo(object[] array, int index)
        {
            GetAllValues().CopyTo(array, index);
        }

        /// <summary>
        /// Gets internal index of NameValuePair
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private int IndexOf(string name)
        {
            // This version does simple iteration. It relies on GetHashCode for optimization
            NameValuePair pair = new NameValuePair(name);
            for (int i = 0; i < this.pairs.Count; i++)
            {
                if (this.pairs[i].Equals(pair))
                {
                    return i;
                }
            }
            return -1;
        }

        #region ISfcPropertySet implementation

        /// <summary>
        /// Checks if the property with specified name exists
        /// </summary>
        /// <param name="propertyName">property name</param>
        /// <returns>true if succeeded</returns>
        public bool Contains(string propertyName)
        {
            return IndexOf(propertyName) >= 0;
        }

        /// <summary>
        /// Checks if the property with specified metadata exists
        /// </summary>
        /// <param name="property">Property</param>
        /// <returns>true if succeeded</returns>
        public bool Contains(ISfcProperty property)
        {
            return Contains(property.Name);
        }

        /// <summary>
        /// Checks if the property with specified name and type exists
        /// </summary>
        /// <typeparam name="T">property type</typeparam>
        /// <param name="name">property name</param>
        /// <returns>true if succeeded</returns>
        public bool Contains<T>(string name)
        {
            int index = IndexOf(name);
            return index >= 0 && this.pairs[index].Value != null && this.pairs[index].Value.GetType() == typeof(T);
        }

        /// <summary>
        /// Attempts to get property value from provider
        /// </summary>
        /// <typeparam name="T">property type</typeparam>
        /// <param name="name">name name</param>
        /// <param name="value">property value</param>
        /// <returns>true if succeeded</returns>
        public bool TryGetPropertyValue<T>(string name, out T value)
        {
            value = default(T);
            int index = IndexOf(name);

            if (index >= 0)
            {
                value = (T)this.pairs[index].Value;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to get property value from provider
        /// </summary>
        /// <param name="name">property name</param>
        /// <param name="value">property value</param>
        /// <returns>true if succeeded</returns>
        public bool TryGetPropertyValue(string name, out object value)
        {
            value = null;
            int index = IndexOf(name);

            if (index >= 0)
            {
                value = this.pairs[index].Value;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to get property metadata
        /// </summary>
        /// <param name="name">property name</param>
        /// <param name="value">propetty information</param>
        /// <returns></returns>
        public bool TryGetProperty(string name, out ISfcProperty property)
        {
            property = null;
            int index = IndexOf(name);

            if (index >= 0)
            {
                property = new Property(this.pairs[index]);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Enumerates all properties
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ISfcProperty> EnumProperties()
        {
            foreach (NameValuePair pair in this.pairs)
            {
                yield return new Property(pair);
            }
        }

        #endregion

        #region ICollection implementation

        public void CopyTo(Array array, int index)
        {
            Array.Copy(this.pairs.ToArray(), array, index);
        }

        public IEnumerator GetEnumerator()
        {
            // Existing code expects this to enumerate property names
            return GetAllKeys().GetEnumerator();
        }

        public int Count
        {
            get { return this.pairs.Count; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return null; }
        }


        #endregion

        public override string ToString()
        {
            StringBuilder textBuilder = new StringBuilder();

            foreach (NameValuePair pair in this.pairs)
            {
                if (textBuilder.Length > 0)
                {
                    textBuilder.Append(", ");
                }
                textBuilder.AppendFormat("{0}={1}", pair.Name, pair.Value);
            }

            return textBuilder.ToString();
        }
    }
}