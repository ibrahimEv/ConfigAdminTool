using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigToolLibrary2
{
    public static class Manipulator
    {
        public static IDictionary<string,object> GetLatestChanges(IDictionary<string,object> Old, IDictionary<string, object> New)
        {
            var latest = new Dictionary<string,object>();
          /*  foreach (var oldObj in Old)
            {
                foreach (var newObj in New)
                {
                    if (newObj.Key == oldObj.Key)
                    {
                        latest.Add(newObj.Key,newObj.Value);
                    }
                    
                }
                
            }*/

            foreach (var newobj in New)
            {
                Old[newobj.Key] = newobj.Value;
            }

            return Old;
        }
    }
}
