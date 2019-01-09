using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Vbe.Interop;

namespace ConfigToolLibrary2
{
    public class Factory
    {

        public static string KeyGenerator(List<string> keys)
        {
            return keys[0] +"X"+ keys[2];
        }

        public static Dictionary<string, IDictionary<string, object>> GetDynamicObjects(List<string> selectStatements)
        {
            Dictionary<string, IDictionary<string, object>> ListOFObjects =
                new Dictionary<string, IDictionary<string, object>>();
            foreach (var Statement in selectStatements)
            {
                List<string> Property = UtilClass.StringSplitter(Statement);
                if (Property.Count % 2 == 1)
                {
                    Property.Insert(0,"PrimaryKey");
                }

                    var key = KeyGenerator(Property);
                    var myObject = new ExpandoObject() as IDictionary<string, object>;
                    for (int i = 0; i < Property.Count; i++)
                    {
                        myObject.Add(Property[i + 1], Property[i]);
                        i++;
                    }

                    ListOFObjects.Add(key,myObject);
                
            }

            var x = ListOFObjects.Last().Value.Last();
            ListOFObjects.Last().Value.Remove(x.Key);
            ListOFObjects.Last().Value.Add(x.Key+" ",x.Value);

            return ListOFObjects;
        }

    }
}
