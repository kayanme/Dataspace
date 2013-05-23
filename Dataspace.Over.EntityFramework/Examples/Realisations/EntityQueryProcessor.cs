using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Over.EntityFramework.Examples;


namespace Dataspace.Over.EntityFramework.Realisations
{
    public sealed class EntityQueryProcessor:QueryProcessor
    {
        public override IEnumerable<T> GetItems<T>(Expression<Func<T, bool>> predicate)
        {
            using(var model = new ResourcesModelContainer())
            {
                return model.Set<T>().Where(predicate);
            }

        }
    }
}
