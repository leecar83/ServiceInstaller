using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;

/// <summary>
/// Based on example from http://www.c-sharpcorner.com/article/installing-a-service-programmatically/
/// </summary>
namespace SvcInstaller
{
    class ServiceInstaller
    {
        #region DLLImports
        [DllImport("advapi32.dll")]
        public static extern IntPtr OpenSCManager(string lpMachineName, string lpSCDB, int scParameter);
        [DllImport("Advapi32.dll")]
        public static extern IntPtr CreateService(IntPtr SC_HANDLE, string lpSvcName, string lpDisplayName,
        int dwDesiredAccess, int dwServiceType, int dwStartType, int dwErrorControl, string lpPathName,
        string lpLoadOrderGroup, int lpdwTagId, string lpDependencies, string lpServiceStartName, string lpPassword);
        [DllImport("advapi32.dll")]
        public static extern void CloseServiceHandle(IntPtr SCHANDLE);
        [DllImport("advapi32.dll")]
        public static extern int StartService(IntPtr SVHANDLE, int dwNumServiceArgs, string lpServiceArgVectors);
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern IntPtr OpenService(IntPtr SCHANDLE, string lpSvcName, int dwNumServiceArgs);
        [DllImport("advapi32.dll")]
        public static extern int DeleteService(IntPtr SVHANDLE);
        [DllImport("kernel32.dll")]
        public static extern int GetLastError();
        [DllImport("kernel32.dll", SetLastError = true)]

        [return: MarshalAs(UnmanagedType.Bool)]

        private static extern bool ChangeServiceConfig(SafeHandle hService, uint dwServiceType, uint dwStartType,
            uint dwErrorControl, string lpBinaryPathName, string lpLoadOrderGroup, out uint lpdwTagId,
            string lpDependencies, string lpServiceStartName, string lpPassword, string lpDisplayName);



        #endregion DLLImport

        #region Constants
        private const uint SERVICE_NO_CHANGE = 0xFFFFFFFF;
        private const uint SERVICE_BOOT_START = 0;
        private const uint SERVICE_SYSTEM_START = 1;
        #endregion

        /// <summary>
        /// Main method
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            string svcPath;
            string svcName;
            string svcDispName;

            ServiceInstaller c = new ServiceInstaller();
            if (args.Length == 3 || args.Length == 2)
            {
                svcPath = args[2];
                svcName = args[1];
                svcDispName = args[1];

                if (String.Equals(args[0], "-install", StringComparison.CurrentCultureIgnoreCase))
                {
                    c.InstallService(svcPath, svcName, svcDispName);
                    Console.WriteLine("Attempted to install " + svcName);
                }
                else if (String.Equals(args[0], "-uninstall", StringComparison.CurrentCultureIgnoreCase))
                {
                    c.UnInstallService(svcName);
                    Console.WriteLine("Attempted to uninstall " + svcName);
                }
                else
                {
                    Console.WriteLine("Use command line arguments -Install or -Uninstall, <ServiceName>, <Path>\n" +
                    "Example: SvcInstaller.exe -Install ServiceDisplayName \"C:\\ServicePath\\Service.exe\"\n" +
                    "         SvcInstaller.exe - Uninstall ServiceDisplayName");
                    Console.ReadLine();
                }
            }
            else
            {
                Console.WriteLine("Use command line arguments -Install or -Uninstall, <Path>, <DisplayName>\n" +
                                   "Example: SvcInstaller.exe -Install \"C:\\ServicePath\\Service.exe\" ServiceDisplayName");
                Console.ReadLine();
            }
        }

        /// <summary>
        /// Installs the service in the service control manager
        /// </summary>
        /// <param name="svcPath">The complete path of the service.</param>
        /// <param name="svcName">Name of the service.</param>
        /// <param name="svcDispName">Display name of the service.</param>
        /// <returns>True if the processed successfully. False if there was any error.</returns>
        private bool InstallService(string svcPath, string svcName, string svcDispName)
        {
            #region Constants declaration.
            int SC_MANAGER_CREATE_SERVICE = 0x0002;
            int SERVICE_WIN32_OWN_PROCESS = 0x00000010;
            //int SERVICE_DEMAND_START = 0x00000003;
            int SERVICE_ERROR_NORMAL = 0x00000001;
            int STANDARD_RIGHTS_REQUIRED = 0xF0000;
            int SERVICE_QUERY_CONFIG = 0x0001;
            int SERVICE_CHANGE_CONFIG = 0x0002;
            int SERVICE_QUERY_STATUS = 0x0004;
            int SERVICE_ENUMERATE_DEPENDENTS = 0x0008;
            int SERVICE_START = 0x0010;
            int SERVICE_STOP = 0x0020;
            int SERVICE_PAUSE_CONTINUE = 0x0040;
            int SERVICE_INTERROGATE = 0x0080;
            int SERVICE_USER_DEFINED_CONTROL = 0x0100;
            int SERVICE_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED |
            SERVICE_QUERY_CONFIG |
            SERVICE_CHANGE_CONFIG |
            SERVICE_QUERY_STATUS |
            SERVICE_ENUMERATE_DEPENDENTS |
            SERVICE_START |
            SERVICE_STOP |
            SERVICE_PAUSE_CONTINUE |
            SERVICE_INTERROGATE |
            SERVICE_USER_DEFINED_CONTROL);
            int SERVICE_AUTO_START = 0x00000002;
            #endregion Constants declaration

            try
            {
                IntPtr sc_handle = OpenSCManager(null, null, SC_MANAGER_CREATE_SERVICE);
                if (sc_handle.ToInt32() != 0)
                {
                    IntPtr sv_handle = CreateService(sc_handle, svcName, svcDispName, SERVICE_ALL_ACCESS, SERVICE_WIN32_OWN_PROCESS, SERVICE_AUTO_START, SERVICE_ERROR_NORMAL, svcPath, null, 0, null, null, null);
                    if (sv_handle.ToInt32() == 0)
                    {
                        CloseServiceHandle(sc_handle);
                        return false;
                    }
                    else
                    {
                        var svc = new ServiceController(svcName);
                        ServiceHelper.ChangeStartMode(svc, ServiceStartMode.Manual);
                        //now trying to start the service
                        //int i = StartService(sv_handle, 0, null);
                        // If the value i is zero, then there was an error starting the service.
                        // note: error may arise if the service is already running or some other problem.
                        //if (i == 0)
                        //{
                            //return false;
                        //}
                        CloseServiceHandle(sc_handle);

                        return true;
                    }
                }
                else
                    return false;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Uninstalls the service from the service control manager
        /// </summary>
        /// <param name="svcName">Name of the service to uninstall.</param>
        private bool UnInstallService(string svcName)
        {
            int GENERIC_WRITE = 0x40000000;
            IntPtr sc_hndl = OpenSCManager(null, null, GENERIC_WRITE);
            if (sc_hndl.ToInt32() != 0)
            {
                int DELETE = 0x10000;
                IntPtr svc_hndl = OpenService(sc_hndl, svcName, DELETE);

                if (svc_hndl.ToInt32() != 0)
                {
                    int i = DeleteService(svc_hndl);
                    if (i != 0)
                    {
                        CloseServiceHandle(sc_hndl);
                        return true;
                    }
                    else
                    {
                        CloseServiceHandle(sc_hndl);
                        return false;
                    }
                }
                else
                    return false;
            }
            else
                return false;
        }

        /// <summary>
        /// ServiceHelper Class to Change Service Start Mode
        /// </summary>
        private static class ServiceHelper
        {
            #region DLL Imports
            [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern Boolean ChangeServiceConfig(
                IntPtr hService,
                UInt32 nServiceType,
                UInt32 nStartType,
                UInt32 nErrorControl,
                String lpBinaryPathName,
                String lpLoadOrderGroup,
                IntPtr lpdwTagId,
                [In] char[] lpDependencies,
                String lpServiceStartName,
                String lpPassword,
                String lpDisplayName);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            static extern IntPtr OpenService(
                IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);

            [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr OpenSCManager(
                string machineName, string databaseName, uint dwAccess);

            [DllImport("advapi32.dll", EntryPoint = "CloseServiceHandle")]
            public static extern int CloseServiceHandle(IntPtr hSCObject);
            #endregion

            #region Constants
            private const uint SERVICE_NO_CHANGE = 0xFFFFFFFF;
            private const uint SERVICE_QUERY_CONFIG = 0x00000001;
            private const uint SERVICE_CHANGE_CONFIG = 0x00000002;
            private const uint SC_MANAGER_ALL_ACCESS = 0x000F003F;
            #endregion

            /// <summary>
            /// Changes Start Mode of the Service
            /// </summary>
            /// <param name="svc">Name of Service</param>
            /// <param name="mode">Start Mode</param>
            public static void ChangeStartMode(ServiceController svc, ServiceStartMode mode)
            {
                var scManagerHandle = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
                if (scManagerHandle == IntPtr.Zero)
                {
                    throw new ExternalException("Open Service Manager Error");
                }

                var serviceHandle = OpenService(
                    scManagerHandle,
                    svc.ServiceName,
                    SERVICE_QUERY_CONFIG | SERVICE_CHANGE_CONFIG);

                if (serviceHandle == IntPtr.Zero)
                {
                    throw new ExternalException("Open Service Error");
                }

                var result = ChangeServiceConfig(
                    serviceHandle,
                    SERVICE_NO_CHANGE,
                    (uint)mode,
                    SERVICE_NO_CHANGE,
                    null,
                    null,
                    IntPtr.Zero,
                    null,
                    null,
                    null,
                    null);

                if (result == false)
                {
                    int nError = Marshal.GetLastWin32Error();
                    var win32Exception = new Win32Exception(nError);
                    throw new ExternalException("Could not change service start type: "
                        + win32Exception.Message);
                }

                CloseServiceHandle(serviceHandle);
                CloseServiceHandle(scManagerHandle);
            }
        }
    }
}
