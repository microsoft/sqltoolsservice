//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement.ObjectTypes.Server
{
     /// <summary>
    /// Class to manage affinity for 64 processors in an independent manner for I/O as well as processors
    /// </summary>
    internal sealed class AffinityManager
    {
        public const int MAX64CPU = 64;
        public const int MAX32CPU = 32;
        public const int MAX_IO_CPU_SUPPORTED = 64;

        public BitArray initialIOAffinityArray = new BitArray(64, false);

        private static string[] configFields = new string[]
            {
                "Minimum",
                "Maximum",
                "Dynamic",
                "ConfigValue",
                "RunValue"
            };
        #region class Affinity representing data related to each affinity mask
        /// <summary>
        /// The Affinity structure represents one row of the sys.configuration table for
        /// that particular affinity (affinity,affinty64 for processor or I/O).
        /// It also stores the old value of the affinity for optimizing server query.
        /// </summary>
        private class Affinity
        {
            private int affinityMaskCfg = 0;
            private int affinityMaskRun = 0;
            /// <summary>
            /// Affinity ConfigValue reflecting sys.configurations table
            /// </summary>
            public int AffinityMaskCfg { get { return affinityMaskCfg; } set { affinityMaskCfg = value; } }
            /// <summary>
            /// Affinity RunValue reflecting sys.configurations table
            /// </summary>
            public int AffinityMaskRun { get { return affinityMaskRun; } set { affinityMaskRun = value; } }

            /// Reset the affinities to default values.
            /// </summary>
            public void Clear()
            {
                affinityMaskCfg = 0;
                affinityMaskRun = 0;
            }
        };
        #endregion

        /// <summary>
        /// Generates a mask for masking the N processors
        /// </summary>
        /// <param name="numProcessors"> number of processors to mask</param>
        /// <returns>Mask for numProcessors</returns>
        private int GetMaskAllProcessors(int numProcessors)
        {
            if (numProcessors < 32)
            {
                try
                {
                    return System.Convert.ToInt32(Math.Pow(2, numProcessors) - 1);
                }
                catch (System.OverflowException)
                {
                    return int.MaxValue;
                }
            }
            else
            {
                // Earlier code used int.MaxValue for return which was buggy
                // since in 2's compliment notation -1 is the one with all bits
                // set to 1. [anchals]
                return (int)-1;
            }
        }

        /// <summary>
        /// Lets the caller know if only one CPU is selected. The processor number for the only set CPU is
        /// passed in. The method is optimized this way otherwise it needs to count the number of 1's in the
        /// affinity mask to see if only one CPU is selected.
        /// </summary>
        /// <param name="processorNumber">The CPU number for which affinity is to be set. Range: [0, 63]</param>
        /// <return>true: this is the last CPU which has affinity bit set;
        /// false: some other CPU than processorNumber is having affinity bit set.</return>
        public bool IsThisLastSelectedProcessor(int processorNumber)
        {
            if (processorNumber < 32)
            {
                return (affinity.AffinityMaskCfg == (1 << processorNumber)) && (affinity64.AffinityMaskCfg == 0);
            }
            else
            {
                return (affinity64.AffinityMaskCfg == (1 << (processorNumber - 32))) && (affinity.AffinityMaskCfg == 0);
            }
        }

        /// <summary>
        /// Get affinity masks for first 32 and next 32 processors (total 64 processors) if the
        /// processor masks have been modified after being read from the server.
        /// </summary>
        /// <param name="affinityConfig">returns the affinity for first 32 processors. null if not changed</param>
        /// <param name="affinity64Config">return the affinity for CPUs 33-64. null if not changed.</param>
        public void GetAffinityMasksIfChanged(out int? affinityConfig, out int? affinity64Config)
        {
            affinityConfig = affinity64Config = null;
            affinityConfig = affinity.AffinityMaskCfg;
            affinity64Config = affinity64.AffinityMaskCfg;
        }


        /// <summary>
        /// Checks if automatic affinity is chosen. Happens when mask for all 64 processors is reset/0.
        /// Tells whether the CPU is in auto affinity mode or not. (i.e. all CPU mask bits are 0)
        /// </summary>
        /// <param name="basedOnConfigValue">true: use ConfigValue mask for test
        /// false: use RunValue mask for test.</param>
        /// <returns>true: The Affinity mode is set to "auto".; false: The affinity mode is not auto.</returns>
        public bool IsAutoAffinity(bool basedOnConfigValue)
        {
            return basedOnConfigValue ? (affinity.AffinityMaskCfg == 0 && affinity64.AffinityMaskCfg == 0)
                                      : (affinity.AffinityMaskRun == 0 && affinity64.AffinityMaskRun == 0);

        }

        /// <summary>
        /// Get the affinity for CPU processorNumber as enabled or disabled.
        /// </summary>
        /// <param name="processorNumber">The CPU number for which affinity is queried. Range: [0, 63]</param>
        /// <return>true: affinity is enabled for the CPU; false: affinity disabled
        ///             /// Note: if the function returns false then the SQL Server might be in auto mode also,
        /// as in auto mode all Processor Affinity Mask bits are 0.
        /// </return>
        public bool GetAffinity(int processorNumber, bool showConfigValues)
        {
            int aux = 0, mask = 0;
            if (processorNumber < 32)
            {
                aux = showConfigValues ? affinity.AffinityMaskCfg : affinity.AffinityMaskRun;
                mask = 1 << processorNumber;
            }
            else
            {
                aux = showConfigValues ? affinity64.AffinityMaskCfg : affinity64.AffinityMaskRun;
                mask = 1 << (processorNumber - 32);
            }

            return (aux & mask) != 0;
        }

        /// <summary>
        /// Set the desired CPU's affinity.
        /// </summary>
        /// <param name="processorNumber">the CPU number for which affinity is set. Range: [0, 63] Must be valid.</param>
        /// <param name="affinityEnabled">
        /// true: set affinity bit for the CPU; false: reset CPU bit for the particular processor.
        /// Note: if false is passed then the SQL Server might be set in auto mode also,
        /// as in auto mode all Processor Affinity Mask bits are 0.
        /// </return>
        public void SetAffinity(int processorNumber, bool affinityEnabled)
        {
            int mask = 0;

            if (processorNumber < 32)
            {
                mask = 1 << processorNumber;
                if (affinityEnabled)
                {
                    affinity.AffinityMaskCfg |= mask;
                }
                else
                {
                    affinity.AffinityMaskCfg &= ~mask;
                }
            }
            else
            {
                mask = 1 << (processorNumber - 32);
                if (affinityEnabled)
                {
                    affinity64.AffinityMaskCfg |= mask;
                }
                else
                {
                    affinity64.AffinityMaskCfg &= ~mask;
                }

            }
        }
        private Affinity affinity = new Affinity(); // affinity mask for first 32 processors
        private Affinity affinity64 = new Affinity(); // affinity mask for next 32 (33-64) processors.
    };
}
