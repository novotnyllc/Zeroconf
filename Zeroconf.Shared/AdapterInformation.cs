namespace Zeroconf
{
    public class AdapterInformation
    {
        private readonly string m_address;
        private readonly string m_name;

        public AdapterInformation(string address, string name)
        {
            m_address = address;
            m_name = name;
        }

        public string Address
        {
            get { return m_address; }
        }

        public string Name
        {
            get { return m_name; }
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Name, Address);
        }
    }
}
