using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ConfigToolLibrary2
{
    public static class ObjectExtension
    {

        public static T CopyObject<T>(this object objSource)
        {

            using (MemoryStream stream = new MemoryStream())

            {

                BinaryFormatter formatter = new BinaryFormatter();

                formatter.Serialize(stream, objSource);

                stream.Position = 0;

                return (T)formatter.Deserialize(stream);

            }

        }

    }
}
