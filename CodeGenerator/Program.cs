using System;
using System.IO;
using System.Xml;

namespace PuzzleXamlGenerator
{
    class Program
    {
        const int error_success = 0;
        const int error_fail = 1;


        static int Main(string[] args)
        {
            try
            {
                App app = new App(args);
                return app.Run();
            }
            catch
            {
            }

            return error_fail;
        }



        private sealed class App
        {
            private string TemplateFilePath { get; }

            private string DestinationFilePath { get; }


            public App(string[] args)
            {
                if (args.Length != 2)
                    throw new ArgumentException();

                TemplateFilePath = args[0];
                DestinationFilePath = args[1];

                if (!File.Exists(TemplateFilePath))
                    throw new FileNotFoundException();
            }


            public int Run()
            {
                XmlDocument doc = new XmlDocument();

                doc.Load(TemplateFilePath);

                XmlNode templateNode = FindNode(doc.DocumentElement, "Template");

                if (templateNode is null)
                    return error_fail;

                XmlNode templateData = templateNode.FirstChild;

                if (templateData is null)
                    return error_fail;

                XmlNode parent = templateNode.ParentNode;

                // delete the template node
                parent.RemoveChild(templateNode);

                // insert new nodes based on the template
                for (int index = 0; index < 81; index++)
                {
                    XmlNode newNode = doc.CreateElement(templateData.Name, templateData.NamespaceURI);

                    foreach (XmlAttribute atribute in templateData.Attributes)
                    {
                        XmlAttribute newAttribute = (XmlAttribute)atribute.CloneNode(false);
                        newAttribute.InnerText = atribute.InnerText.Replace("{0}", index.ToString());
                        newNode.Attributes.Append(newAttribute);
                    }

                    parent.AppendChild(newNode);
                }

                XmlWriterSettings settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "    ",
                    OmitXmlDeclaration = true
                };

                using XmlWriter writer = XmlWriter.Create(DestinationFilePath, settings);

                doc.Save(writer);

                return error_success;
            }




            private XmlNode FindNode(XmlNode node, string nodeName)
            {
                if (node.Name == nodeName)
                    return node;

                if (node.HasChildNodes)
                {
                    foreach (XmlNode childNode in node.ChildNodes)
                    {
                        XmlNode foundNode = FindNode(childNode, nodeName);

                        if (foundNode != null)
                            return foundNode;
                    }
                }

                return null;
            }
        }
    }
}
