using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Dataspace.Common.Announcements;
using Dataspace.Common.ClassesForImplementation;

namespace Dataspace.Common.Data
{
    public class DataRecord
    {
        public DataRecord(Action<Guid, object> poster, object resource)
        {
            Poster = poster;
            Resource = resource;
        }

        public DataRecord(UnactualResourceContent content, object resource)
        {
            Content = content;
            Resource = resource;
        }

        public DataRecord(Action<Guid, object> poster, object resource, UnactualResourceContent content)
            : this(content, resource)
        {
            Poster = poster;
        }

        public DataRecord(Action updateSender, Action<Guid, object> poster, object resource, UnactualResourceContent content)
            : this(poster, resource, content)
        {
            UpdateSender = updateSender;
        }

        public UnactualResourceContent Content { get; internal set; }
        public object Resource { get; internal set; }
        internal Action<Guid, object> Poster { get; private set; }
        internal Action UpdateSender { get; private set; }

        internal object CloneResourceIsPossibleOrRemainSame()
        {
            var formatter = new BinaryFormatter();

            if (Resource != null)
            {
                object res = null;
                if (Resource is ICloneable)
                {
                    res = (Resource as ICloneable).Clone();
                }
                else if (Resource.GetType().IsSerializable)
                {
                    using (var stream = new MemoryStream())
                    {
                        stream.Position = 0;
                        formatter.Serialize(stream, Resource);
                        stream.Position = 0;
                        res = formatter.Deserialize(stream);
                    }
                }
                else
                {
                    res = Resource;
                }
                Debug.Assert(res != null, "res != null");
                return res;
            }
            return null;
        }
    }
}
