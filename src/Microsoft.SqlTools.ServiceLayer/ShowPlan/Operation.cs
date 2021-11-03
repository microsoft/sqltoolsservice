//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan
{
    public sealed class Operation
    {


        /// <summary>
        /// Constructs Operation.
        /// </summary>
        /// <param name="name">Operator name</param>
        /// <param name="displayNameKey">Display name resource ID</param>
        /// <param name="descriptionKey">Description resource ID</param>
        /// <param name="imageName">Image name</param>
        public Operation(string name, string displayNameKey, string descriptionKey, string imageName)
        {
            this.name = name;
            this.displayNameKey = displayNameKey;
            this.descriptionKey = descriptionKey;
            this.imageName = imageName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">Operator name</param>
        /// <param name="displayNameKey">Display name resource ID</param>
        /// <returns></returns>
        public Operation(string name, string displayNameKey): this(name, displayNameKey, null, null)
        {

        }

        /// <summary>
        /// Gets operator name.
        /// </summary>
        public string Name
        {
            get { return this.name; }
        }

        /// <summary>
        /// Gets localized display name.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (this.displayName == null && this.displayNameKey != null)
                {
                    this.displayName = SR.Keys.GetString(this.displayNameKey);
                    Debug.Assert(this.displayName != null);
                }

                return this.displayName != null ? this.displayName : this.name;
            }
        }

        /// <summary>
        /// Gets localized description.
        /// </summary>
        public string Description
        {
            get
            {
                if (this.description == null && this.descriptionKey != null)
                {
                    this.description = SR.Keys.GetString(this.descriptionKey);
                    Debug.Assert(this.description != null);
                }

                return this.description;
            }
        }

        /// <summary>
        /// Gets image.
        /// </summary>
        public string Image
        {
            get
            {
                if (this.image == null && this.imageName != null)
                {
                    this.image = this.imageName;
                }
                return this.image;
            }
        }

        /// <summary>
        /// Creates one-off operation with only display name.
        /// </summary>
        /// <param name="operationDisplayName">Operation display name.</param>
        public static Operation CreateUnknown(string operationDisplayName, string iconName)
        {
            return new Operation(operationDisplayName, null, null, iconName);
        }

        /// <summary>
        /// Unknown operation
        /// </summary>
        public static Operation Unknown
        {
            get { return Operation.unknown; }
        }

        private string name;
        private string displayNameKey;
        private string descriptionKey;
        private string imageName;
        private string helpKeyword;
        private Type displayNodeType;

        private string image;
        private string displayName;
        private string description;

        private static readonly Operation unknown = new Operation(
            String.Empty,
            SR.Keys.Unknown,
            SR.Keys.UnknownDescription,
            "Result_32x.ico");
    }
}
