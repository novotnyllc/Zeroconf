using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
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
        ///     IP Address (alias for IPAddresses.First())
        /// </summary>
        string IPAddress { get; }

        /// <summary>
        ///     IP Addresses
        /// </summary>
        IReadOnlyList<string> IPAddresses { get; }


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
        /// Time-to-live
        /// </summary>
        int Ttl { get; }

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
        readonly Dictionary<string, IService> services = new Dictionary<string, IService>();

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
        ///     IP Address (alias for IPAddresses.First())
        /// </summary>
        public string IPAddress
        {
            get { return IPAddresses?.FirstOrDefault(); }
        }

        /// <summary>
        ///     IP Addresses
        /// </summary>
        public IReadOnlyList<string> IPAddresses { get; set; }

        /// <summary>
        ///     Collection of services provided by the host
        /// </summary>
        public IReadOnlyDictionary<string, IService> Services => services;


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
                var addressesHash = IPAddresses?.Aggregate(0, (current, address) => (current * 397) ^ address.GetHashCode()) ?? 0;
                return ((Id != null ? Id.GetHashCode() : 0)*397) ^ addressesHash;
            }
        }

        /// <summary>
        ///     Diagnostic
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"Id: {Id}, DisplayName: {DisplayName}, IPs: {string.Join(", ", IPAddresses)}, Services: {services.Count}");

            if (services.Any())
            {
                sb.AppendLine();
                foreach (var svc in services)
                {
                    sb.AppendLine(svc.Value.ToString());
                }
            }

            return sb.ToString();
        }

        internal void AddService(IService service)
        {
            services[service.Name] = service ?? throw new ArgumentNullException(nameof(service));
        }
    }

    class Service : IService
    {
        readonly List<IReadOnlyDictionary<string, string>> properties = new List<IReadOnlyDictionary<string, string>>();

        public string Name { get; set; }
        public int Port { get; set; }
        public int Ttl { get; set; }

        public IReadOnlyList<IReadOnlyDictionary<string, string>> Properties => properties;

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append($"Service: {Name} Port: {Port}, TTL: {Ttl}, PropertySets: {properties.Count}");

            if (properties.Any())
            {
                sb.AppendLine();
                for (var i = 0; i < properties.Count; i++)
                {
                    sb.Append($"Begin Property Set #{i}");
                    sb.AppendLine();
                    sb.AppendLine("-------------------");

                    foreach (var kvp in properties[i])
                    {
                        sb.Append($"{kvp.Key} = {kvp.Value}");
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

            properties.Add(set);
        }

    }
}