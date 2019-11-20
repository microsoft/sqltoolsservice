//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


namespace Microsoft.Kusto.ServiceLayer.Formatter
{
    /// <summary>
    /// Describes a string editing action which requests that a particular
    /// substring found at a given location be replaced by another string
    /// </summary>
    public class Replacement
    {
        public Replacement(int startIndex, string oldValue, string newValue)
        {
            StartIndex = startIndex;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public int StartIndex { get; private set; }
        public string OldValue { get; private set; }
        public string NewValue { get; private set; }
        
        public int EndIndex
        {
            get
            {
                return StartIndex + OldValue.Length;
            }
        }

        /// <summary>
        /// Checks whether the replacement will have any effect.
        /// </summary>
        /// <returns></returns>
        internal bool IsIdentity()
        {
            return OldValue.Equals(NewValue);
        }

        /// <summary>
        /// Reports the relative change in text length (number of characters)
        /// between the initial and the formatted code introduced by this
        /// particular replacement.
        /// </summary>
        public int InducedOffset
        {
            get
            {
                return NewValue.Length - OldValue.Length;
            }
        }

        /// <summary>
        /// Replacements will often change the length of the code, making
        /// indexing relative to the original text ambiguous. The CumulatedOffset
        /// can be used to adjust the relative indexing between the original and the
        /// edited text as perceived at the start of this replacement and help
        /// compensate for the difference.
        /// </summary>
        public int CumulativeOffset { set; private get; }

        /// <summary>
        /// A delegate responsible for applying the replacement. Each application assumes
        /// nothing about other replacements which might have taken place before or which
        /// might take place after the current one.
        /// </summary>
        /// <param name="pos">Position of the begining of the replacement relative to the beginig of the character stream.</param>
        /// <param name="len">The number of consecutive characters which are to be replaced.</param>
        /// <param name="with">The characters which are to replace the old ones. Note that the length of this string might be greater or smaller than the number of replaced characters.</param>
        public delegate void OnReplace(int pos, int len, string with);

        /// <summary>
        /// Applies a replacement action according to a given strategy defined by the delegate procedure.
        /// </summary>
        /// <param name="replace">This delegate function implements the strategy for applying the replacement.</param>
        public void Apply(OnReplace replace)
        {
            replace(StartIndex + CumulativeOffset, OldValue.Length, NewValue);
        }

    }
}
