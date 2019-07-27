#region License
// Copyright (c) 2019 Teramine Ltd
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using log4net;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Teramine.DSC.Interconnect.Codecs
{
    public class BsonCodec : Codec
    {
        static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public static byte[] Encode(object data)
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new BsonDataWriter(ms))
                {
                    var serializer = new Newtonsoft.Json.JsonSerializer();
                    serializer.Serialize(writer, data);
                }

                return ms.ToArray();
            }
        }

        public static T Decode<T>(byte[] data)
        {
            try
            {
                using (var stream = new MemoryStream(data))
                {
                    using (var reader = new BsonDataReader(stream))
                    {
                        var serializer = new Newtonsoft.Json.JsonSerializer();
                        var deserialised = serializer.Deserialize<T>(reader);
                        return (T)deserialised;
                    }
                }

            }
            catch (ArgumentOutOfRangeException ex) when (ex.ParamName == "type")
            {
                return default;
            }
            catch (Exception e)
            {
                Log.ErrorFormat("Failed to decode binary content to type [{0}]: [{1}]", typeof(T).ToString(), e.Message);
                return default;
            }
        }
    }
}
