using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Vbe.Interop;

namespace ConfigToolLibrary2
{
    public class Factory
    {
       
        public static IDictionary<string,object> GetDynamicObject(string SqlLine)
        {
            string[] Property = Utils.StringSplitter(SqlLine);
            if (Property.Length % 2 == 0)
            {
                var myObject = new ExpandoObject() as IDictionary<string, object>;
                for (int i = 0; i < Property.Length; i++)
                {
                    myObject.Add(Property[i+1],Property[i]);
                    i++;
                }

                return myObject;
            }

            return null;
        }

    }
}
