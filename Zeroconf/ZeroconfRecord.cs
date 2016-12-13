using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeroconf
{
    /// <summary>
    ///     A ZeroConf record response
    /// </summary>
    public interface IZeroconfHost
    {
        /// <summary>
        ///     Name
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        ///     Id, possibly different than the Name
        /// </summary>
        string Id { get; }

        /// <summary>
        ///     IP Address
        /// </summary>
        string IPAddress { get; }


        /// <summary>
        ///     Services offered by this host (based on services queried for)
        /// </summary>
        IReadOnlyDictionary<string, IService> Services { get; }
    }

    /// <summary>
    ///     Represents a service provided by a host
    /// </summary>
    public interface IService
    {
        /// <summary>
        ///     Name
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Port
        /// </summary>
        int Port { get; }

        /// <summary>
        ///     Properties of the object. Most services have a single set of properties, but some services
        ///     may return multiple sets of properties
        /// </summary>
        IReadOnlyList<IReadOnlyDictionary<string, string>> Properties { get; }
    }

    /// <summary>
    ///     A ZeroConf record response
    /// </summary>
    class ZeroconfHost : IZeroconfHost, IEquatable<ZeroconfHost>, IEquatable<IZeroconfHost>
    {
        readonly Dictionary<string, IService> _services = new Dictionary<string, IService>();

        public bool Equals(IZeroconfHost other)
        {
            return Equals(other as ZeroconfHost);
        }

        public bool Equals(ZeroconfHost other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Id, other.Id) && string.Equals(IPAddress, other.IPAddress);
        }

        /// <summary>
        ///     Id, possibly different than the display name
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     IP Address
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        ///     Collection of services provided by the host
        /// </summary>
        public IReadOnlyDictionary<string, IService> Services
        {
            get { return _services; }
        }


        /// <summary>
        ///     Display Name
        /// </summary>
        public string DisplayName { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ZeroconfHost)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Id != null ? Id.GetHashCode() : 0)*397) ^ (IPAddress != null ? IPAddress.GetHashCode() : 0);
            }
        }

        /// <summary>
        ///     Diagnostic
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Id: {0}, DisplayName: {1}, IP: {2}, Services: {3}", Id, DisplayName, IPAddress, _services.Count);

            if (_services.Any())
            {
                sb.AppendLine();
                foreach (var svc in _services)
                {
                    sb.AppendLine(svc.Value.ToString());
                }
            }

            return sb.ToString();
        }

        internal void AddService(IService service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            _services[service.Name] = service;
        }
    }

    class Service : IService
    {
        readonly List<IReadOnlyDictionary<string, string>> _properties = new List<IReadOnlyDictionary<string, string>>();

        public string Name { get; set; }
        public int Port { get; set; }

        public IReadOnlyList<IReadOnlyDictionary<string, string>> Properties
        {
            get { return _properties; }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendFormat("Service: {0} Port: {1}, PropertySets: {2}", Name, Port, _properties.Count);

            if (_properties.Any())
            {
                sb.AppendLine();
                for (var i = 0; i < _properties.Count; i++)
                {
                    sb.AppendFormat("Begin Property Set #{0}", i);
                    sb.AppendLine();
                    sb.AppendLine("-------------------");

                    foreach (var kvp in _properties[i])
                    {
                        sb.AppendFormat("{0} = {1}", kvp.Key, kvp.Value);
                        sb.AppendLine();
                    }
                    sb.AppendLine("-------------------");
                }
            }

            return sb.ToString();
        }

        internal void AddPropertySet(IReadOnlyDictionary<string, string> set)
        {
            if (set == null)
                throw new ArgumentNullException(nameof(set));

            _properties.Add(set);
        }
    }
}