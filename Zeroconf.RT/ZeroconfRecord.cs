using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zeroconf
{
    public class ZeroconfRecord
    {
        internal void AddProperty(string key, string value)
        {
            _properties[key] = value;
        }

        private readonly Dictionary<string, string> _properties = new Dictionary<string, string>();

        public string Name { get; set; }
        public string IPAddress { get; set; }
        public string Host { get; set; }
        public string Port { get; set; }
        
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

        public IReadOnlyDictionary<string, string> Properties
        {
            get
            {
                return _properties;
            }
        }
    }
}