using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Attributes;
using Dataspace.Common.ClassesForImplementation;

namespace Projections.Advanced.Test.Objects
{
    [Resource("Model")]
    [Serializable]
    public sealed class Model
    {
        public string Name { get; set; }
    }
    [Export(typeof(ResourceGetter))]
    public class ModelGetter:ResourceGetter<Model>
    {
        protected override Model GetItemTyped(Guid id)
        {
            throw new NotImplementedException();
        }
    }

    [Export(typeof(ResourcePoster))]
    public class ModelPoster : ResourcePoster<Model>
    {
        protected override void WriteResourceTyped(Guid key, Model resource)
        {
            throw new NotImplementedException();
        }

        protected override void DeleteResourceTyped(Guid key)
        {
            throw new NotImplementedException();
        }
    }

    [Export(typeof(ResourceQuerier))]
    public class ModelQuerier : ResourceQuerier<Model>
    {
      
        [IsQuery]
        public IEnumerable<Guid> Query1(Guid element)
        {
            return DefaultValue;
        }

        [IsQuery]
        public IEnumerable<Guid> Query1(string name)
        {
            return DefaultValue;
        }

        [IsQuery]
        public IEnumerable<Guid> Query1(bool isRoot, Guid element)
        {
            return DefaultValue;
        }

        [IsQuery]
        public IEnumerable<Guid> Query1(string name,Guid element)
        {
            return DefaultValue;
        }

        [IsQuery]
        public IEnumerable<Guid> Query1()
        {
            return DefaultValue;
        }

    }

    [Export(typeof(ResourceRegistrator))]
    public class Registrator:ResourceRegistrator
    {
        protected override Type[] ResourceTypes
        {
            get { return new[] {typeof (Model)}; }
        }
    }
}
