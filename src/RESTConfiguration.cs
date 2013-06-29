using System;
using System.Collections.Generic;
using System.Text;
using PayPal.Manager;
using PayPal.Exception;
using System.Management;
using System.Runtime.InteropServices;

namespace PayPal
{
    public class RESTConfiguration
    {
        /// <summary>
        /// string Authorization Token
        /// </summary>
        private string authorizeToken;

        /// <summary>
        /// Idempotency Request Id
        /// </summary>
        private string reqId;

        /// <summary>
        /// Dynamic configuration map
        /// </summary>
        private Dictionary<string, string> config;

        /// <summary>
        ///  Gets and sets the Authorization Token
        /// </summary>
        public string AuthorizationToken
        {
            get
            {
                return this.authorizeToken;
            }
            set
            {
                this.authorizeToken = value;
            }
        }

        /// <summary>
        /// Gets and sets the Idempotency Request Id
        /// </summary>
        public string RequestId
        {
            private get
            {
                return reqId;
            }
            set
            {
                reqId = value;
            }
        }

        /// <summary>
        /// Optional headers map
        /// </summary>
        private Dictionary<string, string> headersMap;

        public RESTConfiguration(Dictionary<string, string> config)
        {
            this.config = ConfigManager.GetConfigWithDefaults(config);
        }

        public RESTConfiguration(Dictionary<string, string> config, Dictionary<string, string> headersMap)
        {
            this.config = ConfigManager.GetConfigWithDefaults(config);
            this.headersMap = (headersMap == null) ? new Dictionary<string, string>() : headersMap;
        }

        public Dictionary<string, string> GetHeaders()
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(AuthorizationToken))
            {
                headers.Add("Authorization", AuthorizationToken);
            }
            else if (!string.IsNullOrEmpty(GetClientId()) && !string.IsNullOrEmpty(GetClientSecret()))
            {
                headers.Add("Authorization", "Basic " + EncodeToBase64(GetClientId(), GetClientSecret()));
            }
            headers.Add("User-Agent", FormUserAgentHeader());
            if (!string.IsNullOrEmpty(RequestId))
            {
                headers.Add("PayPal-Request-Id", RequestId);
            }
            return headers;
        }

        private string GetClientId()
        {
            return this.config.ContainsKey(BaseConstants.ClientId) ? this.config[BaseConstants.ClientId] : null;
        }

        private string GetClientSecret()
        {
            return this.config.ContainsKey(BaseConstants.ClientSecret) ? this.config[BaseConstants.ClientSecret] : null;
        }

        private string EncodeToBase64(string clientId, string clientSecret)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(clientId + ":" + clientSecret);
                string base64ClientId = Convert.ToBase64String(bytes);
                return base64ClientId;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new PayPalException(ex.Message, ex);
            }
            catch (ArgumentException ex)
            {
                throw new PayPalException(ex.Message, ex);
            }
            catch (NotSupportedException ex)
            {
                throw new PayPalException(ex.Message, ex);
            }
            catch (System.Exception ex)
            {
                throw new PayPalException(ex.Message, ex);
            }
        }

        public static string FormUserAgentHeader()
        {
            string header = null;
            StringBuilder stringBuilder = new StringBuilder("PayPalSDK/"
                    + PayPalResource.SDKId + " " + PayPalResource.SDKVersion
                    + " ");
            string dotNETVersion = DotNetVersionHeader;
            stringBuilder.Append(";").Append(dotNETVersion);
            string osVersion = GetOSHeader();
            if (osVersion.Length > 0)
            {
                stringBuilder.Append(";").Append(osVersion);
            }
            header = stringBuilder.ToString();
            return header;
        }

        private static string OperatingSystemFriendlyName
        {
            get
            {
                string result = string.Empty;
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
                foreach (ManagementObject os in searcher.Get())
                {
                    result = os["Caption"].ToString();
                }
                return result;
            }
        }

#if NET_2_0 || NET_3_5
        private static bool Is64BitProcess
        {
            get { return IntPtr.Size == 8; }
        }

        private static bool Is64BitOperatingSystem
        {
            get
            {
                if (Is64BitProcess)
                {
                    return true;
                }
                bool isWow64;
                return ModuleContainsFunction("kernel32.dll", "IsWow64Process") && IsWow64Process(GetCurrentProcess(), out isWow64) && isWow64;
            }
        }

        private static bool ModuleContainsFunction(string moduleName, string methodName)
        {
            IntPtr hModule = GetModuleHandle(moduleName);
            if (hModule != IntPtr.Zero)
            {
                return GetProcAddress(hModule, methodName) != IntPtr.Zero;
            }
            return false;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        extern static bool IsWow64Process(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] out bool isWow64);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        extern static IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        extern static IntPtr GetModuleHandle(string moduleName);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        extern static IntPtr GetProcAddress(IntPtr hModule, string methodName);
#endif
        private static string GetOSHeader()
        {
            string osHeader = string.Empty;

#if NET_2_0 || NET_3_5
            if (Is64BitOperatingSystem)
            {
                osHeader += "bit=" + 64 + ";";
            }
            else 
            {
                osHeader += "bit=" + 32 + ";";
            }
#elif NET_4_0
            if (Environment.Is64BitOperatingSystem)
            {
                osHeader += "bit=" + 64 + ";";
            }
            else 
            {
                osHeader += "bit=" + 32 + ";";
            }
#endif
            osHeader += "os=" + OperatingSystemFriendlyName + " " + Environment.OSVersion.Version + ";";
            return osHeader;
        }

        private static string DotNetVersionHeader
        {
            get
            {
                string DotNetVersionHeader = "lang=" + "DOTNET;" + "v=" + Environment.Version.ToString().Trim();
                return DotNetVersionHeader;
            }
        }
    }
}
