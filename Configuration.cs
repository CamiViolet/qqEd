using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace qqEd
{
    public class Configuration
    {
        private static Configuration instance;   // 'Singleton pattern'
        private Dictionary<string, string> dict;

        XmlSerializer serializer = new XmlSerializer(typeof(item[]),
                             new XmlRootAttribute() { ElementName = "items" });

        public static Configuration Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Configuration();
                    instance.LoadFromFile();
                }
                return instance;
            }
        }

        public void LoadFromFile()
        {
            if (File.Exists("./Configuration.xml"))
            {
                using (var fileStream = new FileStream("./Configuration.xml", FileMode.Open))
                {
                    dict = ((item[])serializer.Deserialize(fileStream)).ToDictionary(i => i.id, i => i.value);
                    return;
                }
            }
            dict = new Dictionary<string, string>();
        }

        public void Load(System.Windows.Window form, System.Windows.Controls.TextBox text)
        {
            text.Text = dict[form.GetType().Name + "/" + text.Name];
        }

        public void Save(System.Windows.Window form, System.Windows.Controls.TextBox text)
        {
            dict[form.GetType().Name + "/" + text.Name] = text.Text;
        }

        public void SaveToFile()
        {
            using (var fileStream = new FileStream("./Configuration.xml", FileMode.Create))
            {
                serializer.Serialize(fileStream,
                    dict.Select(kv => new item() { id = kv.Key, value = kv.Value }).ToArray());
            }
        }
    }
    public class item
    {
        [XmlAttribute]
        public string id;
        [XmlAttribute]
        public string value;
    }
}