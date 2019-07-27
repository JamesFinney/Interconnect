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

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Teramine.Interconnect.Codecs;
using Teramine.Interconnect.Models;

namespace Teramine.Interconnect.Test
{
    [TestFixture]
    public class BsonCodecTests
    {
        [Test]
        public void EncodeDecode_SessionDescriptor()
        {
            SessionDescriptor descriptor = new SessionDescriptor { Name = "testName" };
            var descEncoded = BsonCodec.Encode(descriptor);
            Assert.NotNull(descEncoded);
            var decoded = BsonCodec.Decode<SessionDescriptor>(descEncoded);
            Assert.NotNull(decoded);
            Assert.AreEqual(descriptor.Name, decoded.Name);
        }

        [Test]
        public void EncodeDecode_SessionDescriptor_Concurrent()
        {
            List<SessionDescriptor> descriptors = new List<SessionDescriptor>();
            for(int i = 0; i < 1000; i++)
            {
                descriptors.Add(new SessionDescriptor { Name = i.ToString() });
            }

            var result = Parallel.ForEach(descriptors, new ParallelOptions { MaxDegreeOfParallelism = 10 }, (s) =>
            {
                var encoded = BsonCodec.Encode(s);
                Assert.NotNull(encoded);
                var decoded = BsonCodec.Decode<SessionDescriptor>(encoded);
                Assert.NotNull(decoded);
                Assert.AreEqual(s.Name, decoded.Name);
            });

            Assert.IsTrue(result.IsCompleted);
        }

        [Test]
        public void EncodeDecode_Dictionary_Concurrent()
        {
            List<Dictionary<string,object>> dictionaries = new List<Dictionary<string, object>>();
            for (int i = 0; i < 1000; i++)
            {
                dictionaries.Add(new Dictionary<string, object>
                {
                    { "key", i.ToString() },
                    { "data", new byte[80000] }
                });
            }

            var result = Parallel.ForEach(dictionaries, new ParallelOptions { MaxDegreeOfParallelism = 10 }, (s) =>
            {
                var encoded = BsonCodec.Encode(s);
                Assert.NotNull(encoded);
                var decoded = BsonCodec.Decode<Dictionary<string, object>>(encoded);
                Assert.NotNull(decoded);
                Assert.IsTrue(decoded.ContainsKey("key"));
                Assert.IsTrue(decoded.ContainsKey("data"));
                Assert.AreEqual(80000, (decoded["data"] as byte[]).Length);
            });

            Assert.IsTrue(result.IsCompleted);
        }
    }
}
