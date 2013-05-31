using System.Dynamic;
using System.Linq;
using System.Xml.Linq;

namespace Dataspace.Common.Utility
{
    /// <summary>
    /// Класс для быстрого построения Xml без сложных потребностей.
    /// Синтаксис вида tag(attr1:attrValue,attr2:attrValue, elemTag1(...),elemTag2(...),
    /// где tag,elemTag1,elemTag2 - экземпляры данного класса, "attr1","attr2" - имена атрибутов соответствующего тэга.
    /// attrValue - значение атрибута.
    /// На выходе выполнения метода - XDocument.
    /// </summary>
    /// <example>
    /// dynamic tag1 = new XmlBuilder("tag1","");
    /// dynamic tag2 = new XmlBuilder("tag2","");
    /// 
    /// XDocument doc = tag1(name:"Tag1",
    ///                      t21: tag2(name:"Tag21"),
    ///                      t22: tag2(name:"Tag22"));
    /// 
    /// На выходе:
    /// <Tag1 name="Tag1">
    ///    <Tag2 name="Tag21/>
    ///    <Tag2 name="Tag22/>
    /// </Tag1>
    /// 
    /// Имена t21 и t22 даны только потому, что C# запрещает ставить именованные параметры раньше неименованных, 
    /// а атрибуты в начале нагляднее.
    /// </example>
    public sealed class XmlBuilder:DynamicObject
    {      
        private string _nmspace;
        private string _name;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlBuilder"/> class.
        /// </summary>
        /// <param name="name">Имя тэга.</param>
        /// <param name="nmspace">Неймспейс.</param>
        public XmlBuilder(string name,string nmspace)
        {
            _nmspace = nmspace;
            _name = name;
        }

        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            var document = new XDocument();
            document.Add(new XElement(XName.Get(_name, _nmspace)));
          
            var pairs = binder.CallInfo.ArgumentNames.Zip(args, (a, b) => new {name = a, value = b});
            foreach (var pair in pairs)
            {
                if (pair.value is XDocument)
                {
                    var newElement = (pair.value as XDocument).Root;                    
                    document.Root.Add(newElement);
                }
                else
                {
                    document.Root.Add(new XAttribute(XName.Get(pair.name,_nmspace),pair.value??"{x:Null}"));
                }
            }
            result = document;
            return true;
        }
    }
}
