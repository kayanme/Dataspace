using System.Xml.Schema;

namespace Dataspace.Common.Interfaces
{
    public interface ISchemeProvider
    {
        XmlSchema GetReadScheme();
	 
    }
}
