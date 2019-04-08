using System.Configuration;

namespace ConfigToolWPF
{
    public class RepositoryConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsRequired = true, IsDefaultCollection = true)]
        public RepositoryConfigInstanceCollection Instances
        {
            get { return (RepositoryConfigInstanceCollection)this[""]; }
            set { this[""] = value; }
        }
    }
    public class RepositoryConfigInstanceCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new RepositoryConfigInstanceElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            //set to whatever Element Property you want to use for a key
            return ((RepositoryConfigInstanceElement)element).Name;
        }
    }

    public class RepositoryConfigInstanceElement : ConfigurationElement
    {
        //Make sure to set IsKey=true for property exposed as the GetElementKey above
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get { return (string)base["name"]; }
            set { base["name"] = value; }
        }

        [ConfigurationProperty("displayName", IsKey = true, IsRequired = true)]
        public string DisplayName
        {
            get { return (string)base["displayName"]; }
            set { base["displayName"] = value; }
        }

        [ConfigurationProperty("id", IsRequired = true)]
        public int Id
        {
            get { return (int)base["id"]; }
            set { base["id"] = value; }
        }
    }

    public class RepositoryDetail
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
    }

}


