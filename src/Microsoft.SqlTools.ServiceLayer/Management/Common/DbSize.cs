//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlTools.ServiceLayer.Management
{
    /// <summary>
    /// Helper class to describe the size of an Azure database
    /// </summary>
    public class DbSize
    {
        public enum SizeUnits
        {
            MB,
            GB,
            TB
        }

        #region Member Vars

        private readonly int size;
        private readonly SizeUnits sizeUnit;

        #endregion 


        public DbSize(int size, SizeUnits sizeUnit)
        {
            this.size = size;
            this.sizeUnit = sizeUnit;
        }

        /// <summary>
        /// Copy constructor
        /// </summary> 
        public DbSize(DbSize copy)
        {
            this.size = copy.Size;
            this.sizeUnit = copy.SizeUnit;
        }
        
        /// <summary>
        /// Size of the DB
        /// </summary>
        public int Size
        {
            get
            {
                return size;
            }
        }

        /// <summary>
        /// Units that the size is measured in 
        /// </summary>
        public SizeUnits SizeUnit
        {
            get
            {
                return sizeUnit;
            }
        }

        /// <summary>
        /// Returns the number of bytes represented by the DbSize
        /// </summary>
        public double SizeInBytes
        {
            get
            {
                var sizeBase = Convert.ToDouble(this.size);
                switch (SizeUnit)
                {
                    case SizeUnits.MB:
                        return sizeBase*1024.0*1024.0;
                    case SizeUnits.GB:
                        return sizeBase*1024.0*1024.0*1024.0;
                    case SizeUnits.TB:
                        return sizeBase*1024.0*1024.0*1024.0*1024.0;
                }
                throw new InvalidOperationException(SR.UnknownSizeUnit(SizeUnit.ToString()));
            }
        }
        #region Object Overrides 
        /// <summary>
        /// Displays the size in the format ####UU (e.g. 100GB)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return size + sizeUnit.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return this == (DbSize)obj;
        }

        public override int GetHashCode()
        {
            return this.size.GetHashCode() ^ this.sizeUnit.GetHashCode();
        }

        #endregion Object Overrides

        /// <summary>
        /// Parses a string in the format ####UU into a DbSize object. The number of
        /// numeric characters must be parseable into an int and the last two characters
        /// mapped to one of the SizeUnits enum values. 
        /// </summary>
        /// <param name="dbSizeString"></param>
        /// <returns></returns>
        public static DbSize ParseDbSize(string dbSizeString)
        {
            if (dbSizeString == null || dbSizeString.Length < 3)
            { //Sanity check
                throw new ArgumentException("DbSize string must be at least 3 characters (#UU)");
            }
            int size;
            //Try and parse all but the last two characters into the size
            if (int.TryParse(dbSizeString.Substring(0, dbSizeString.Length - 2), out size))
            {
                //Get the unit portion (last two characters)
                string unitPortion = dbSizeString.Substring(dbSizeString.Length - 2 );
                SizeUnits unit;
                if (Enum.TryParse(unitPortion, true, out unit))
                {
                    return new DbSize(size, unit);
                }
                else
                {
                    throw new ArgumentException("DbSize string does not contain a valid unit portion");
                }
            }
            else
            {
                throw new ArgumentException("DbSize string does not contain a valid numeric portion");
            }
        }

        public static bool operator ==(DbSize x, DbSize y)
        {
            if(ReferenceEquals(x, y))
            { //Both null or both same instance, are equal
                return true;
            }

            if((object)x == null || (object)y == null)
            { //Only one null, never equal (cast to object to avoid infinite loop of ==)
                return false;
            }

            return x.size == y.size && x.sizeUnit == y.sizeUnit;
        }

        public static bool operator !=(DbSize x, DbSize y)
        {
            return !(x == y);
        }
    }
}
