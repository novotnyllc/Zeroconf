using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;

namespace Zeroconf
{
    /// <summary>
    /// A ZeroConf record response
    /// </summary>
    public interface IZeroconfRecord
    {
        /// <summary>
        /// Name of the record
        /// </summary>
        string Name { get; }

        /// <summary>
        /// IP Address
        /// </summary>
#if NETFX_CORE
        Windows.Networking.HostName
#else
        System.Net.IPAddress 
#endif
            
            IPAddress { get; }

        /// <summary>
        /// Host name
        /// </summary>
        string Host { get; }

        /// <summary>
        /// Port
        /// </summary>
        int Port { get; }

        /// <summary>
        /// Properties of the object
        /// </summary>
        IReadOnlyDictionary<string, string> Properties { get; }
    }

    /// <summary>
    /// A ZeroConf record response
    /// </summary>
    internal class ZeroconfRecord : IZeroconfRecord
    {
        internal void AddProperty(string key, string value)
        {
            _properties[key] = value;
        }

        private readonly Dictionary<string, string> _properties = new Dictionary<string, string>();

        /// <summary>
        /// Name of the record
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// IP Address
        /// </summary>
        public
#if NETFX_CORE
 Windows.Networking.HostName
#else
        System.Net.IPAddress 
#endif
            IPAddress { get; set; }
        
        
        /// <summary>
        /// Host name
        /// </summary>
        public string Host { get; set; }
        
        /// <summary>
        /// Port
        /// </summary>
        public int Port { get; set; }
        
        /// <summary>
        /// Diagnostic
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Name:{0} IP:{1} Host:{2} Port:{3}", Name, IPAddress, Host, Port);

            if (_properties.Any())
            {
                sb.AppendLine();
                foreach (var kvp in _properties)
                {
                    sb.AppendFormat("{0} = {1}", kvp.Key, kvp.Value);
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Properties of the object
        /// </summary>
        public IReadOnlyDictionary<string, string> Properties
        {
            get
            {
                return _properties;
            }
        }
    }
}