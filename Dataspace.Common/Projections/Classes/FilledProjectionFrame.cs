using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Dataspace.Common.Projections.Storages;
using Dataspace.Common.ClassesForImplementation;

namespace Dataspace.Common.Projections.Classes
{
    internal class FilledProjectionFrame
    {

        public const string IdName = "Id";

        public string Name{get { return _matchedElement.Name; }}

        public string[] Keys { get { return _matchedElement.FillingInfo.PropertyNames; } }

        private Lazy<object> _resource;

        private ProjectionElement _matchedElement;

        private Guid _key;

        public Guid Key { get { return _key; } }

       

        private object ValueExtractor(object resource, string name)
        {
            var type = resource.GetType();
            var property =
                type.GetProperty(name);

            if (property != null && property.CanRead)
                return property.GetGetMethod().Invoke(resource, null);

            var field = type.GetField(name);

            if (field != null)
                return field.GetValue(resource);

            throw new InvalidOperationException(string.Format("Нет свойства {1} для ресурса {0}", type.Name, name));
        }

        public object this[string key]
        {
            get
            {
                if (key == IdName)
                    return _key;
                if (key == RegistrationStorage.DefinitlyNoChildren)
                    return !_matchedElement.DownRelations.Any();
                if (_resource != null)
                {
                   var value = _resource.Value;
                }
                if (_matchedElement.FillingInfo.GetFillType(key) == FillingInfo.FillType.Native)
                {
                    Debug.Assert(_resource != null);
                    if (_resource.Value != null)
                        return ValueExtractor(_resource.Value, key);
                    else                    
                        return null;
                    
                }
                else if (_matchedElement.FillingInfo.GetFillType(key) == FillingInfo.FillType.ByFiller)
                {
                    if (_matchedElement.PropertyFiller == null)
                        throw new InvalidOperationException(string.Format("Для получения поля {0} элемента {1} требуется явное описание получения (либо элемент не представляет ресурс, либо в нем отсуствует указанное поле)",key,_matchedElement.Name));
                    return _matchedElement.PropertyFiller(_key, key);
                }
                else
                {
                    Debug.Fail("нет такого типа заполнения");
                    throw new ArgumentException("нет такого типа заполнения");
                }
            }
        }

        public FilledProjectionFrame[] ChildFrames;

        public FilledProjectionFrame(ProjectionElement element,Guid key)
        {         
            _key = key;
            _matchedElement = element;
            Debug.Assert(_matchedElement.FillingInfo.PropertyNames.All(k => _matchedElement.FillingInfo.GetFillType(k) == FillingInfo.FillType.ByFiller));
          
        }

        public FilledProjectionFrame(ProjectionElement element, Guid key,Lazy<object> resourceFiller)
        {
            _key = key;
            _matchedElement = element;
            _resource = resourceFiller;
          
        }
    }
}
