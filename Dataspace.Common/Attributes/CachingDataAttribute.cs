using System;

namespace Dataspace.Common.Attributes
{

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class CachingDataAttribute:System.Attribute
    {
        public string Name { get; private set; }

		public bool DefaultQuerier { get; set; }

        public bool CollectRareItems = true;

        public CachingDataAttribute(string name)
        {
            Name = name;
        	DefaultQuerier = false;
        }
    }
}
