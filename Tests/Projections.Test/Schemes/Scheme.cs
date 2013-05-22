
using System.Xml.Schema;
using System.IO;
using System.ComponentModel.Composition;
using Dataspace.Common.Interfaces;

namespace Server.Modules.ProdStructureModule
{
  [Export(typeof(ISchemeProvider))]
  internal class BaseScheme:ISchemeProvider
  {
   
     private const string ReadScheme = "<?xml version=\"1.0\" encoding=\"utf-8\"?><xs:schema id=\"BaseScheme\"    targetNamespace=\"http://tempuri.org/BaseScheme\"    elementFormDefault=\"qualified\"    xmlns=\"\"    xmlns:b=\"http://metaspace.org/DataSchema\"    xmlns:mstns=\"http://tempuri.org/BaseScheme\"    xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">  <xs:import namespace =\"http://metaspace.org/DataSchema\"/>  <xs:element name=\"Element\">    <xs:complexType>      <xs:complexContent>        <xs:extension base=\"b:Element\">          <xs:sequence>            <xs:element name=\"Attribute\">              <xs:complexType>                <xs:complexContent>                  <xs:extension base=\"b:Attribute\">                    <xs:sequence>                      <xs:element name =\"Code\">                        <xs:complexType>                                                 <xs:attribute name =\"Value\"/>                        </xs:complexType>                      </xs:element>                      <xs:element name=\"Value\">                        <xs:complexType>                          <xs:complexContent>                            <xs:extension base=\"b:Value\">                              <xs:attribute name=\"Name\" />                            </xs:extension>                          </xs:complexContent>                        </xs:complexType>                      </xs:element>                    </xs:sequence>                    <xs:attribute name=\"Name\" />                  </xs:extension>                </xs:complexContent>              </xs:complexType>            </xs:element>          </xs:sequence>          <xs:attribute name=\"Name\" />        </xs:extension>      </xs:complexContent>    </xs:complexType>  </xs:element></xs:schema>";	    


     public XmlSchema GetReadScheme()
	 {	  
        return XmlSchema.Read(new StringReader(ReadScheme),(o,e)=> { });	  
	 }    
  }

  [Export(typeof(ISchemeProvider))]
  internal class FilterScheme:ISchemeProvider
  {
   
     private const string ReadScheme = "<?xml version=\"1.0\" encoding=\"utf-8\"?><xs:schema id=\"NamefilterScheme\"    targetNamespace=\"http://tempuri.org/Namefilter\"    elementFormDefault=\"qualified\"    xmlns=\"\"    xmlns:b=\"http://metaspace.org/DataSchema\"    xmlns:mstns=\"http://tempuri.org/Namefilter\"    xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">  <xs:import namespace =\"http://metaspace.org/DataSchema\"/>  <xs:element name=\"Element\">    <xs:complexType>      <xs:complexContent>        <xs:extension base=\"b:Element\">          <xs:sequence>            <xs:element name=\"Value\" type=\"mstns:Value\"/>          </xs:sequence>          <xs:attribute name=\"Name\" />                </xs:extension>      </xs:complexContent>    </xs:complexType>  </xs:element>  <xs:element name=\"Value\" type=\"mstns:Value\"/>    <xs:complexType name=\"Value\">    <xs:complexContent>      <xs:extension base=\"b:Value\">        <xs:sequence>          <xs:element name=\"Element\">            <xs:complexType>              <xs:complexContent>                <xs:extension base=\"b:Element\">                  <xs:sequence>                    <xs:element name=\"Attribute\">                      <xs:complexType>                        <xs:complexContent>                          <xs:extension base=\"b:Attribute\">                            <xs:attribute name=\"Name\" />                                                    </xs:extension>                        </xs:complexContent>                      </xs:complexType>                    </xs:element>                  </xs:sequence>                  <xs:attribute name=\"Name\" />                                </xs:extension>              </xs:complexContent>            </xs:complexType>          </xs:element>        </xs:sequence>        <xs:attribute name=\"Name\" />              </xs:extension>    </xs:complexContent>  </xs:complexType>  </xs:schema>";	    


     public XmlSchema GetReadScheme()
	 {
	  
        return XmlSchema.Read(new StringReader(ReadScheme),(o,e)=> { });
	  
	 }


    

  }
  
  
  [Export(typeof(ISchemeProvider))]
  internal class GroupScheme:ISchemeProvider
  {
   
     private const string ReadScheme = "<?xml version=\"1.0\" encoding=\"utf-8\"?><xs:schema id=\"GroupTest\"    targetNamespace=\"http://tempuri.org/GroupScheme\"    elementFormDefault=\"qualified\"    xmlns=\"\"    xmlns:b=\"http://metaspace.org/DataSchema\"    xmlns:mstns=\"http://tempuri.org/GroupScheme\"    xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">  <xs:import namespace =\"http://metaspace.org/DataSchema\"/>  <xs:element name=\"ElementGroup\">    <xs:complexType>      <xs:sequence>        <xs:element name=\"Element\">          <xs:complexType>                          <xs:complexContent>                <xs:extension base=\"b:Element\">                  <xs:sequence>                    <xs:element name=\"AttributeGroup\">                      <xs:complexType>                        <xs:sequence>                          <xs:element name=\"AttributeSubGroup\">                            <xs:complexType>                              <xs:sequence>                                <xs:element name=\"Attribute\">                                  <xs:complexType>                                    <xs:complexContent>                                      <xs:extension base=\"b:Attribute\">                                        <xs:attribute name=\"Name\"/>                                      </xs:extension>                                    </xs:complexContent>                                  </xs:complexType>                                </xs:element>                              </xs:sequence>                              <xs:attribute name=\"Name\"/>                            </xs:complexType>                          </xs:element>                        </xs:sequence>                        <xs:attribute name=\"Name\"/>                      </xs:complexType>                    </xs:element>                  </xs:sequence>                  <xs:attribute name=\"Name\"/>                </xs:extension>              </xs:complexContent>          </xs:complexType>                </xs:element>      </xs:sequence>      <xs:attribute name=\"Name\"/>      <xs:attribute name=\"Id\" type=\"b:Guid\"/>    </xs:complexType>  </xs:element>             </xs:schema>";	    


     public XmlSchema GetReadScheme()
	 {
	  
        return XmlSchema.Read(new StringReader(ReadScheme),(o,e)=> { });
	  
	 }    
  }

  [Export(typeof(ISchemeProvider))]
  internal class NameGroupsScheme:ISchemeProvider
  {
   
     private const string ReadScheme = "<?xml version=\"1.0\" encoding=\"utf-8\"?><xs:schema id=\"NamefilterScheme\"    targetNamespace=\"http://tempuri.org/NamefilterWithGroups\"    elementFormDefault=\"qualified\"    xmlns:b=\"http://metaspace.org/DataSchema\"    xmlns=\"http://tempuri.org/NamefilterWithGroups\"      xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">  <xs:import namespace =\"http://metaspace.org/DataSchema\"/>  <xs:element name=\"Element\">    <xs:complexType>      <xs:complexContent>        <xs:extension base=\"b:Element\">          <xs:sequence>            <xs:element name=\"Value\" type=\"Value\"/>          </xs:sequence>          <xs:attribute name=\"Name\" />        </xs:extension>      </xs:complexContent>    </xs:complexType>  </xs:element>  <xs:element name=\"Value\" type=\"Value\"/>    <xs:complexType name=\"Value\">    <xs:complexContent>      <xs:extension base=\"b:Value\">        <xs:sequence>          <xs:element name=\"Element\">            <xs:complexType>              <xs:complexContent>                <xs:extension base=\"b:Element\">                  <xs:sequence>                    <xs:element name=\"Attribute\">                      <xs:complexType>                        <xs:complexContent>                          <xs:extension base=\"b:Attribute\">                            <xs:attribute name=\"Name\" />                          </xs:extension>                        </xs:complexContent>                      </xs:complexType>                    </xs:element>                  </xs:sequence>                  <xs:attribute name=\"Name\" />                </xs:extension>                           </xs:complexContent>                                    </xs:complexType>          </xs:element>        </xs:sequence>        <xs:attribute name=\"Name\" />      </xs:extension>    </xs:complexContent>  </xs:complexType>  </xs:schema>";	    


     public XmlSchema GetReadScheme()
	 {
	  
        return XmlSchema.Read(new StringReader(ReadScheme),(o,e)=> { });
	  
	 }    
  }
  
}