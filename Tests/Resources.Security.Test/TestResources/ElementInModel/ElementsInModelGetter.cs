using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;

using Resources.Test.TestResources.ElementInModel;

namespace Resources.Test.TestResources
{
    [Export(typeof(ResourceGetter))]
    public class ElementsInModelGetter : ResourceGetter<ElementsInModel>
    {

        #region Imports

#pragma warning disable 0649
        [Import]
        private ResourcePool _pool;
#pragma warning restore 0649

        #endregion

        public static bool WasChanged = false;

        protected override ElementsInModel GetItemTyped(Guid id)
        {
            try
            {
                WasChanged = true;
                var elements = TypedPool.Get<Element>("model=" + id);
                return new ElementsInModel {Elements = elements};
            }
            catch (KeyNotFoundException)
            {

                return null;
                
            }
           
        }

        protected override IEnumerable<KeyValuePair<Guid, ElementsInModel>> GetItemsTyped(IEnumerable<Guid> id)
        {
            WasChanged = true;
            return id.ToDictionary(k => k, GetItemTyped);
        }

      
    }
}
