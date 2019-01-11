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
        public bool Flag=false;
        public int Cnt=0;

        public static string KeyGenerator(List<string> keys,bool flag01)
        {
            if (flag01 == false)
            {
                return keys[0];
            }
            else
            {
                return keys[0] + "X" + keys[2];
            }
            
        }

        public  Dictionary<string, IDictionary<string, object>> GetDynamicObjects(List<string> selectStatements, bool flag01 = false)
        {
            Dictionary<string, IDictionary<string, object>> ListOFObjects =
                new Dictionary<string, IDictionary<string, object>>();
            foreach (var Statement in selectStatements)
            {
                Cnt++;
                List<string> Property = UtilClass.StringSplitter(Statement);
                if (Property.Count % 2 == 1)
                {
                    Property.Insert(0,"PrimaryKey"+Cnt);
                }

                    var key = KeyGenerator(Property, flag01);
                    var myObject = new ExpandoObject() as IDictionary<string, object>;
                    for (int i = 0; i < Property.Count; i++)
                    {
                        myObject.Add(Property[i + 1], Property[i]);
                        i++;
                    }

                try
                {
                    ListOFObjects.Add(key, myObject);
                }
                catch
                {
                    this.Flag = true;
                    ListOFObjects.Clear();
                    return GetDynamicObjects(selectStatements,this.Flag);
                  
                }
                    
                
            }

            var x = ListOFObjects.Last().Value.Last();
            ListOFObjects.Last().Value.Remove(x.Key);
            ListOFObjects.Last().Value.Add(x.Key+" ",x.Value);

            return ListOFObjects;
        }

    }
}
