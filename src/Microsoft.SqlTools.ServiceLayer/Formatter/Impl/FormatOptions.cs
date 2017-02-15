//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.ComponentModel;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{

    public enum CasingOptions { None, Uppercase, Lowercase };

    /// <summary>
    /// The supported options to use when formatting text
    /// </summary>
    public class FormatOptions : INotifyPropertyChanged
    {

        private int spacesPerIndent;
        private bool useTabs = false;
        private bool encloseIdentifiersInSquareBrackets;
        private bool placeCommasBeforeNextStatement;
        private bool placeEachReferenceOnNewLineInQueryStatements;
        private CasingOptions keywordCasing;
        private CasingOptions datatypeCasing;
        private bool alignColumnDefinitionsInColumns;

        internal FormatOptions()
        {
            SpacesPerIndent = 4;
            UseTabs = false;
            PlaceCommasBeforeNextStatement = false;
            EncloseIdentifiersInSquareBrackets = false;
            PlaceEachReferenceOnNewLineInQueryStatements = false;
        }

        public int SpacesPerIndent 
        { 
            get { return spacesPerIndent; }
            set { spacesPerIndent = value; 
                RaisePropertyChanged("SpacesPerIndent"); }
        }

        public bool UseTabs
        {
            get { return useTabs; }
            set
            {
                useTabs = value;
                // raise UseTabs & UseSpaces property changed events
                RaisePropertyChanged("UseTabs");
                RaisePropertyChanged("UseSpaces");
            }
        }

        public bool UseSpaces
        {
            get { return !UseTabs; }
            set { UseTabs = !value; }
        }

        public bool EncloseIdentifiersInSquareBrackets
        {
            get { return encloseIdentifiersInSquareBrackets; }
            set
            {
                encloseIdentifiersInSquareBrackets = value;
                RaisePropertyChanged("EncloseIdentifiersInSquareBrackets");
            }
        }

        public bool PlaceCommasBeforeNextStatement
        {
            get { return placeCommasBeforeNextStatement; }
            set
            {
                placeCommasBeforeNextStatement = value;
                RaisePropertyChanged("PlaceCommasBeforeNextStatement");
            }
        }

        public bool PlaceEachReferenceOnNewLineInQueryStatements
        {
            get { return placeEachReferenceOnNewLineInQueryStatements; }
            set
            {
                placeEachReferenceOnNewLineInQueryStatements = value;
                RaisePropertyChanged("PlaceEachReferenceOnNewLineInQueryStatements");
            }
        }

        public CasingOptions KeywordCasing
        {
            get { return keywordCasing; }
            set
            {
                keywordCasing = value;
                RaisePropertyChanged("KeywordCasing");
            }
        }

        public bool UppercaseKeywords
        {
            get { return KeywordCasing == CasingOptions.Uppercase; }
        }
        public bool LowercaseKeywords
        {
            get { return KeywordCasing == CasingOptions.Lowercase; }
        }

        public bool DoNotFormatKeywords
        {
            get { return KeywordCasing == CasingOptions.None; }
        }
        
        public CasingOptions DatatypeCasing
        {
            get { return datatypeCasing; }
            set
            {
                datatypeCasing = value;
                RaisePropertyChanged("DatatypeCasing");
            }
        }

        public bool UppercaseDataTypes
        {
            get { return DatatypeCasing == CasingOptions.Uppercase; }
        }
        public bool LowercaseDataTypes
        {
            get { return DatatypeCasing == CasingOptions.Lowercase; }
        }
        public bool DoNotFormatDataTypes
        {
            get { return DatatypeCasing == CasingOptions.None; }
        }
        
        public bool AlignColumnDefinitionsInColumns
        {
            get { return alignColumnDefinitionsInColumns; }
            set
            {
                alignColumnDefinitionsInColumns = value;
                RaisePropertyChanged("AlignColumnDefinitionsInColumns");
            }
        }
        
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static void Copy(FormatOptions target, FormatOptions source)
        {
            target.AlignColumnDefinitionsInColumns = source.AlignColumnDefinitionsInColumns;
            target.DatatypeCasing = source.DatatypeCasing;
            target.EncloseIdentifiersInSquareBrackets = source.EncloseIdentifiersInSquareBrackets;
            target.KeywordCasing = source.KeywordCasing;
            target.PlaceCommasBeforeNextStatement = source.PlaceCommasBeforeNextStatement;
            target.PlaceEachReferenceOnNewLineInQueryStatements = source.PlaceEachReferenceOnNewLineInQueryStatements;
            target.SpacesPerIndent = source.SpacesPerIndent;
            target.UseSpaces = source.UseSpaces;
            target.UseTabs = source.UseTabs;
        }
    }

}
