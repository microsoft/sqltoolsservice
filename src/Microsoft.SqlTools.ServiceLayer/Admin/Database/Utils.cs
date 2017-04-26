//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

//extern alias VSShell2;
//extern alias VSShell2Iop;
using System;
using System.Reflection;
using System.Globalization;
using Microsoft.SqlServer.Management.Diagnostics;
using SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
//using VSShell2Iop.Microsoft.VisualStudio.Shell.Interop;
//using VSShell2.Microsoft.VisualStudio.Shell;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    /// <summary>
    /// Internal reusable helpers
    /// </summary>
    internal class Utils
    {
        /// <summary>
        /// only static methods
        /// </summary>
        private Utils() { }

        /// <summary>
        /// returns all instances of the given custom attribute on a given object
        /// </summary>
        /// <param name="objectToGetAttributeFrom"></param>
        /// <param name="customAttribute"></param>
        /// <returns></returns>
        public static Attribute GetCustomAttribute(object objectToGetAttributeFrom, Type customAttribute)
        {         
            //first, see if the object implemented this interface to override standard behavior
            System.Reflection.ICustomAttributeProvider attribProvider = objectToGetAttributeFrom as System.Reflection.ICustomAttributeProvider;
            if (attribProvider == null)
            {
                //if not, get it from its type
                attribProvider = (System.Reflection.ICustomAttributeProvider)objectToGetAttributeFrom.GetType();
            }

            object[] attribs = attribProvider.GetCustomAttributes(customAttribute, true);
            if (attribs != null && attribs.Length > 0)
            {
                //NOTE: important that we'll always use the first one in collection.
                //Our implementation of ICustomAttributeProvider knows about that and
                //relies on this behavior
                return attribs[0] as Attribute;
            }

            return null;
        }


        /// <summary>
        /// called to create SqlConnectionInfo out of the given CDataContainer object
        /// </summary>
        /// <param name="dc"></param>
        /// <returns></returns>
        public static SqlConnectionInfo GetSqlConnectionInfoFromDataContainer(CDataContainer dc)
        {
            if (dc != null)
            {
                // we may have been given conneciton information by the object explorer. in which case there is no need
                // to build it ourselves.
                SqlConnectionInfo result = dc.ConnectionInfo as SqlConnectionInfo;
                if (result == null)
                {
                    throw new InvalidOperationException();
                }

                return result;
            }
            else
            {
                return null;
            }
        }

        public static int InitialTreeViewWidth
        {
            get
            {
                return 175;
            }
        }

        /// <summary>
        /// Try to set the CLR thread name. 
        /// Will not throw if the name is already set.
        /// </summary>
        /// <param name="name"></param>
        public static void TrySetThreadName(String name)
        {
            try
            {
                System.Threading.Thread.CurrentThread.Name = name;
            }
            catch (InvalidOperationException)
            { }
        }

        public static bool IsKatmaiOrLater(int version)
        {
            return (10 <= version);
        }

        public static bool IsKjOrLater(ServerVersion version)
        {
            return (version.Major > 10
                    || (version.Major == 10 && version.Minor >= 50));
        }

        public static bool IsSql11OrLater(ServerVersion version)
        {
            return IsSql11OrLater(version.Major);
        }

        public static bool IsSql11OrLater(int versionMajor)
        {
            return (versionMajor >= 11);
        }

        public static bool IsSql12OrLater(ServerVersion version)
        {
            return IsSql12OrLater(version.Major);
        }

        public static bool IsSql12OrLater(int versionMajor)
        {
            return (versionMajor >= 12);
        }

        public static bool IsSql13OrLater(ServerVersion version)
        {
            return IsSql13OrLater(version.Major);
        }

        public static bool IsSql13OrLater(int versionMajor)
        {
            return (versionMajor >= 13);
        }

        public static bool IsSql14OrLater(ServerVersion version)
        {
            return IsSql14OrLater(version.Major);
        }

        public static bool IsSql14OrLater(int versionMajor)
        {
            return (versionMajor >= 14);
        }

        /// <summary>
        /// Check if the version is SQL 2016 SP1 or later.
        /// </summary>
        /// <param name="version"></param>
        /// <returns>true if the version is SQL 2016 SP1 or later, false otherwise</returns>
        public static bool IsSql13SP1OrLater(Version version)
        {
            return (version >= new Version(13, 0, 3510));
        }

        public static bool IsXTPSupportedOnServer(SMO.Server server)
        {
            bool isXTPSupported = false;

            if (server.ConnectionContext.ExecuteScalar("SELECT SERVERPROPERTY('IsXTPSupported')") != DBNull.Value)
            {
                isXTPSupported = server.IsXTPSupported;
            }

            return isXTPSupported;
        }

        /// <summary>
        /// Returns true if given database has memory optimized filegroup on given server.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="dbName"></param>
        /// <returns></returns>
        public static bool HasMemoryOptimizedFileGroup(SMO.Server server, string dbName)
        {
            bool hasMemoryOptimizedFileGroup = false;

            if (server.ServerType != DatabaseEngineType.SqlAzureDatabase)
            {
                string query = string.Format(CultureInfo.InvariantCulture,
                                                            "select top 1 1 from [{0}].sys.filegroups where type = 'FX'",
                                                            CUtils.EscapeString(dbName, ']'));
                if (server.ConnectionContext.ExecuteScalar(query) != null)
                {
                    hasMemoryOptimizedFileGroup = true;
                }
            }

            return hasMemoryOptimizedFileGroup;
        }

        public static bool IsPolybasedInstalledOnServer(SMO.Server server)
        {
            bool isPolybaseInstalled = false;

            if (server.IsSupportedProperty("IsPolyBaseInstalled"))
            {
                isPolybaseInstalled = server.IsPolyBaseInstalled;
            }

            return isPolybaseInstalled;
        }

        /// <summary>
        /// Returns true if current user has given permission on given server.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="permissionName"></param>
        /// <returns></returns>
        public static bool HasPermissionOnServer(SMO.Server server, string permissionName)
        {
            return Convert.ToBoolean(server.ConnectionContext.ExecuteScalar(
                                                    string.Format(CultureInfo.InvariantCulture,
                                                    "SELECT HAS_PERMS_BY_NAME(null, null, '{0}');",
                                                    permissionName)));
        }

        public static bool FilestreamEnabled(SMO.Server svr)
        {
            bool result = false;
            if (svr != null)
            {
                if (IsKatmaiOrLater(svr.Information.Version.Major)     
                    && svr.ServerType != DatabaseEngineType.SqlAzureDatabase) //Azure doesn't support filestream
                {
                    if (svr.Configuration.FilestreamAccessLevel.RunValue != 0)
                    {
                        result = true;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// This function returns true if SSMS is the running application
        /// and if it contains only the core relational packages. 
        /// </summary>
        /// <returns></returns>
        //public static bool IsSsmsMinimalSet()
        //{
        //    IVsShell vsShell = Package.GetGlobalService(typeof(SVsShell)) as IVsShell;
        //    const string guidSqlStudioPkgString = "04401ff3-8b0f-4d2d-85eb-2a3542867a8b";
        //    Guid guidSqlStudioPkg = new Guid(guidSqlStudioPkgString);


        //    //Applications like 'AS Migration wizard' are non-ssms/non-VS shell applications
        //    if (vsShell == null)
        //    {
        //        return false;
        //    }
        //    else
        //    {
        //        IVsPackage ssmsPackage;
        //        vsShell.IsPackageLoaded(ref guidSqlStudioPkg, out ssmsPackage);

        //        return ((ssmsPackage != null) &&
        //                !AreExtendedFeaturesAvailable());
        //    }
        //}

        /// <summary>
        /// This function checks if extended SSMS packages are loaded
        /// 
        /// </summary>
        /// <returns></returns>
        //public static bool AreExtendedFeaturesAvailable()
        //{
        //    return Microsoft.SqlServer.Management.UI.VSIntegration.SsmsInformation.CanShowNonExpressFeatures;
        //}

        /// <summary>
        /// Execute a static method by reflection
        /// </summary>
        /// <param name="assemblyShortName">The short name of the assembly, like Microsoft.SqlServer.Management.RegisteredServersUI.dll</param>
        /// <param name="className">The fully qualified name of the class, like Microsoft.SqlServer.Management.RegisteredServers.Utils</param>
        /// <param name="methodName">THe name of the static method to call, like DoSomething</param>
        /// <param name="parameters">params array of arguments to pass to the method</param>
        /// <returns></returns>
        //public static object ReflectionExecuteStatic(
        //    string assemblyShortName,
        //    string className,
        //    string methodName,
        //    params object[] parameters)
        //{
        //    STrace.Params(
        //        SqlMgmtDiag.TName,
        //        "Utils.ReflectionExecuteStatic(string, string, string, object[])",
        //        "assemblyShortName='{0}', className='{1}', methodName='{2}'",
        //        assemblyShortName,
        //        className,
        //        methodName);

        //    Assembly assembly = Assembly.Load(
        //         Microsoft.SqlServer.Management.SqlMgmt.AssemblyLoadUtil.GetFullAssemblyName(assemblyShortName));

        //    if (assembly == null)
        //    {
        //        STrace.LogExThrow();
        //        throw new ArgumentException("Couldn't load assembly by reflection");
        //    }

        //    Type type = assembly.GetType(className);
        //    if (type == null)
        //    {
        //        STrace.LogExThrow();
        //        throw new ArgumentException("Couldn't find class by reflection");
        //    }

        //    // if we need to call a polymorphic method, use type.GetMethod(string, BindingFlags, null, Type[], null)
        //    MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        //    if (method == null)
        //    {
        //        STrace.LogExThrow();
        //        throw new ArgumentException("Couldn't find method by reflection");
        //    }

        //    return method.Invoke(null, parameters);
        //}

        public static bool IsYukonOrAbove(SMO.Server server)
        {
            return server.Version.Major >= 9;
        }

        public static bool IsBelowYukon(SMO.Server server)
        {
            return server.Version.Major < 9;
        }

        /// <summary>
        /// Some calendars, such as the UmAlQuraCalendar, support an upper date range that is earlier than MaxValue. 
        /// In these cases, trying to access MaxValue in variable assignments or formatting and parsing operations can throw 
        /// an ArgumentOutOfRangeException. Rather than retrieving the value of DateTime.MaxValue, you can retrieve the value 
        /// of the specified culture's latest valid date value from the 
        /// System.Globalization.CultureInfo.DateTimeFormat.Calendar.MaxSupportedDateTime property. 
        /// http://msdn.microsoft.com/en-us/library/system.datetime.maxvalue(v=VS.90).aspx
        /// </summary>
        /// <returns></returns>
        //public static DateTime GetMaxCultureDateTime()
        //{
        //    CultureInfo currentCulture = System.Threading.Thread.CurrentThread.CU;
        //    return currentCulture.DateTimeFormat.Calendar.MaxSupportedDateTime;
        //}

        public static string MakeSqlBracket(string s)
        {
            return "[" + s.Replace("]", "]]") + "]";
        }

        /// <summary>
        /// Displays F1 Help link
        /// </summary>
        /// <param name="serviceProvider">Service provider to display help</param>
        /// <param name="dialogF1Keyword">F1 help link</param>
        //public static void DisplayHelp(IServiceProvider serviceProvider, string dialogF1Keyword)
        //{
        //    if (serviceProvider == null)
        //    {
        //        return;
        //    }
        //    IHelpService helpService = (IHelpService)serviceProvider.GetService(typeof(IHelpService));
        //    if (helpService == null)
        //    {
        //        IHelpProvider helpProvider = (IHelpProvider)serviceProvider.GetService(typeof(IHelpProvider));
        //        if (helpProvider != null)
        //        {
        //            helpProvider.DisplayTopicFromF1Keyword(dialogF1Keyword);
        //        }
        //    }
        //    else
        //    {
        //        helpService.DisplayHelp(dialogF1Keyword);
        //    }
        //}
    }

    /// <summary>
    /// Public reusable helpers
    /// </summary>
    public class SqlMgmtUtils
    {
        /// <summary>
        /// only static methods
        /// </summary>
        private SqlMgmtUtils() { }

        /// <summary>
        /// Returns whether the server is in AS Azure
        /// </summary>
        /// <param name="serverName"></param>
        /// <returns></returns>
        public static bool IsASAzure(string serverName)
        {
            return !string.IsNullOrEmpty(serverName) && serverName.StartsWith("asazure://", StringComparison.OrdinalIgnoreCase);
        }
    }
}
