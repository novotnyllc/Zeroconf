using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zeroconf
{
    /// <summary>
    /// A ZeroConf record response
    /// </summary>
    public class ZeroconfRecord
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
        public string IPAddress { get; set; }
        
        /// <summary>
        /// Host name
        /// </summary>
        public string Host { get; set; }
        
        /// <summary>
        /// Port
        /// </summary>
        public string Port { get; set; }
        
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