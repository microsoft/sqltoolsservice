//-----------------------------------------------------------------------
// <copyright file="PickerItemBase.cs" company="Microsoft">
//   (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.ComponentModel;

namespace Microsoft.Data.Relational.Design.Controls
{
    /// <summary>
    /// base view model for item picker controls (such as a ComboBox) that
    /// provides text filtering functionality on the list of items
    /// </summary>
    public class PickerItemBase : INotifyPropertyChanged
    {
        private string _textBeforeMatch;
        private string _filterTextMatch;
        private string _textAfterMatch;
        private string _name;

        public string Name
        {
            get
            {
                return _name;
            }

            protected set
            {
                if (_name != value)
                {
                    _name = value;
                }
            }
        }
        
        public string Category { get; private set; }

        public override string ToString()
        {
            return Name;
        }

        public PickerItemBase(string name, string category = null)
        {
            _name = name;
            this.Category = category;
        }

        public virtual void Initialize()
        {
            this.SetNoTextMatch();
        }

        public string TextBeforeMatch
        {
            get { return _textBeforeMatch; }
        }
        public string FilterTextMatch
        {
            get { return _filterTextMatch; }
        }
        public string TextAfterMatch
        {
            get { return _textAfterMatch; }
        }

        public virtual void ApplyFilter(string filter)
        {
            string previousTextBeforeMatch = _textBeforeMatch;
            string previousFilterTextMatch = _filterTextMatch;
            string previousTextAfterMatch = _textAfterMatch;

            if (string.IsNullOrEmpty(filter))
            {
                SetNoTextMatch();
            }
            else
            {
                string displayName = this.Name;
                int index = displayName.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase);
                if (index >= 0)
                {
                    int filterLength = filter.Length;
                    _textBeforeMatch = displayName.Substring(0, index);
                    _filterTextMatch = displayName.Substring(index, filterLength);
                    _textAfterMatch = displayName.Substring(index + filterLength);
                }
                else
                {
                    SetNoTextMatch();
                }
            }

            if (previousTextBeforeMatch != _textBeforeMatch)
            {
                OnPropertyChanged("TextBeforeMatch");
            }
            if (previousFilterTextMatch != _filterTextMatch)
            {
                OnPropertyChanged("FilterTextMatch");
            }
            if (previousTextAfterMatch != _textAfterMatch)
            {
                OnPropertyChanged("TextAfterMatch");
            }
        }

        protected void SetNoTextMatch()
        {
            _textBeforeMatch = this.Name;
            _filterTextMatch = _textAfterMatch = string.Empty;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }
    }
}