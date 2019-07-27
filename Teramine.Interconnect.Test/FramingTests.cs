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
using Teramine.Interconnect.Codecs;
using Teramine.Interconnect.Framing;
using Teramine.Interconnect.Models;

namespace Teramine.Interconnect.Test
{
    [TestFixture]
    public class FramingTests
    {
        [Test]
        public void Decode_NullBuffer()
        {
            Assert.Throws<ArgumentNullException>(() => Frame.DecodeFrameBuffer(null, 0, 10, out var newOffset, out var newAvailable));
        }

        [Test]
        public void Decode_EmptyBuffer()
        {
            var frames = Frame.DecodeFrameBuffer(new byte[0], 0, 0, out var newOffset, out var newAvailable);
            Assert.AreEqual(0, frames.Count);
        }

        [Test]
        public void Decode_RandomBuffer()
        {
            var rand = new Random(Guid.NewGuid().GetHashCode());
            var buffer = new byte[1000];
            rand.NextBytes(buffer);

            var frames = Frame.DecodeFrameBuffer(buffer, 0, buffer.Length, out var newOffset, out var newAvailable);
            Assert.AreEqual(0, frames.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
        }

        [Test]
        public void EncodeDecode_SingleFrame()
        {
            var encoded = Frame.CreateConnectionStartFrame();
            var decoded = Frame.DecodeFrameBuffer(encoded, 0, encoded.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(1, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
            Assert.AreEqual(FrameType.ConnectionStart, decoded[0].Type);
        }

        [Test]
        public void EncodeDecode_SingleFrame_BadCrc()
        {
            var encoded = Frame.CreateConnectionStartFrame();
            encoded[encoded.Length - 2] = 0x00;

            var decoded = Frame.DecodeFrameBuffer(encoded, 0, encoded.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(0, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
        }

        [Test]
        public void EncodeDecode_SingleFrame_CorruptContent()
        {
            var encoded = Frame.CreateConnectionStartFrame();

            encoded[10] = 0xFF;
            encoded[11] = 0xFF;
            encoded[12] = 0xFF;

            var decoded = Frame.DecodeFrameBuffer(encoded, 0, encoded.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(0, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
        }

        [Test]
        public void EncodeDecode_FrameOffsetFromStart_ToEnd()
        {
            var encoded = Frame.CreateConnectionStartFrame();
            var modified = new byte[20 + encoded.Length];
            Array.Copy(encoded, 0, modified, 20, encoded.Length);

            var decoded = Frame.DecodeFrameBuffer(modified, 0, modified.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(1, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
        }

        [Test]
        public void EncodeDecode_FrameOffsetFromStart_EmptySpaceEnd()
        {
            var encoded = Frame.CreateConnectionStartFrame();
            var modified = new byte[50 + encoded.Length + 20];
            Array.Copy(encoded, 0, modified, 50, encoded.Length);

            var decoded = Frame.DecodeFrameBuffer(modified, 0, modified.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(1, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
        }

        [Test]
        public void EncodeDecode_StartAligned_PartSecondFrame()
        {
            var encoded0 = Frame.CreateConnectionStartFrame();
            var encoded1 = Frame.CreateConnectionStartFrame();

            var combined = new byte[encoded0.Length + encoded1.Length / 2];
            Array.Copy(encoded0, 0, combined, 0, encoded0.Length);
            Array.Copy(encoded1, 0, combined, encoded0.Length, encoded1.Length / 2);

            var decoded = Frame.DecodeFrameBuffer(combined, 0, combined.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(1, decoded.Count);
            Assert.AreEqual(encoded0.Length, newOffset);
            Assert.AreEqual(combined.Length - encoded0.Length, newAvailable);
        }

        [Test]
        public void EncodeDecode_Multiple_Same_Aligned()
        {
            var encoded0 = Frame.CreateConnectionStartFrame();
            var encoded1 = Frame.CreateConnectionStartFrame();
            var encoded2 = Frame.CreateConnectionStartFrame();

            var combined = new byte[encoded0.Length * 3];
            Array.Copy(encoded0, 0, combined, 0, encoded0.Length);
            Array.Copy(encoded1, 0, combined, encoded0.Length, encoded1.Length);
            Array.Copy(encoded2, 0, combined, encoded0.Length + encoded1.Length, encoded2.Length);

            var decoded = Frame.DecodeFrameBuffer(combined, 0, combined.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(3, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
        }

        [Test]
        public void EncodeDecode_Multiple_Different_Aligned()
        {
            var encoded0 = Frame.CreateConnectionStartFrame();
            var encoded1 = Frame.CreateConnectionOpenFrame();
            var encoded2 = Frame.CreateConnectionCloseFrame();

            var combined = new byte[encoded0.Length * 3];
            Array.Copy(encoded0, 0, combined, 0, encoded0.Length);
            Array.Copy(encoded1, 0, combined, encoded0.Length, encoded1.Length);
            Array.Copy(encoded2, 0, combined, encoded0.Length + encoded1.Length, encoded2.Length);

            var decoded = Frame.DecodeFrameBuffer(combined, 0, combined.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(3, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);

            Assert.AreEqual(FrameType.ConnectionStart, decoded[0].Type);
            Assert.AreEqual(FrameType.ConnectionOpen, decoded[1].Type);
            Assert.AreEqual(FrameType.ConnectionClose, decoded[2].Type);
        }

        [Test]
        public void EncodeDecode_SessionStart_Single_NoData()
        {
            var encoded = Frame.CreateSessionStartFrame(10, null);
            var decoded = Frame.DecodeFrameBuffer(encoded, 0, encoded.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(1, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
            Assert.AreEqual(FrameType.SessionStart, decoded[0].Type);
            Assert.AreEqual(10, decoded[0].SessionID);
        }

        [Test]
        public void EncodeDecode_SessionStart_Single_WithDictionaryData()
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "keyString", "string" },
                { "keyInteger", 36 },
                { "keyArray", new byte[10] },
            };

            var encoded = Frame.CreateSessionStartFrame(0, BsonCodec.Encode(data));
            var decoded = Frame.DecodeFrameBuffer(encoded, 0, encoded.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(1, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
            Assert.AreEqual(FrameType.SessionStart, decoded[0].Type);
            Assert.AreEqual(0, decoded[0].SessionID);

            // validate data
            Assert.NotNull(decoded[0].Payload);
            var dataDecoded = BsonCodec.Decode<Dictionary<string, object>>(decoded[0].Payload);
            Assert.NotNull(dataDecoded);

            // keys and values present
            Assert.IsTrue(dataDecoded.ContainsKey("keyString"));
            Assert.AreEqual("string", (string)dataDecoded["keyString"]);

            Assert.IsTrue(dataDecoded.ContainsKey("keyInteger"));
            Assert.AreEqual(36, (int)(long)dataDecoded["keyInteger"]);      // need to first unbox

            Assert.IsTrue(dataDecoded.ContainsKey("keyArray"));
            Assert.AreEqual(10, ((byte[])dataDecoded["keyArray"]).Length);
        }

        [Test]
        public void EncodeDecode_SessionStart_Multiple_WithData()
        {
            var descriptor0 = new SessionDescriptor { Name = "0" };
            var encoded0 = Frame.CreateSessionStartFrame(10, BsonCodec.Encode(descriptor0));

            var descriptor1 = new SessionDescriptor { Name = "1" };
            var encoded1 = Frame.CreateSessionStartFrame(11, BsonCodec.Encode(descriptor1));

            var descriptor2 = new SessionDescriptor { Name = "2" };
            var encoded2 = Frame.CreateSessionStartFrame(12, BsonCodec.Encode(descriptor2));

            var descriptor3 = new SessionDescriptor { Name = "3" };
            var encoded3 = Frame.CreateSessionStartFrame(13, BsonCodec.Encode(descriptor3));

            var descriptor4 = new SessionDescriptor { Name = "4" };
            var encoded4 = Frame.CreateSessionStartFrame(14, BsonCodec.Encode(descriptor4));

            var combined = new byte[encoded0.Length + encoded1.Length + encoded2.Length + encoded3.Length + encoded4.Length];
            Array.Copy(encoded0, 0, combined, 0, encoded0.Length);
            Array.Copy(encoded1, 0, combined, encoded0.Length, encoded1.Length);
            Array.Copy(encoded2, 0, combined, encoded0.Length + encoded1.Length, encoded2.Length);
            Array.Copy(encoded3, 0, combined, encoded0.Length + encoded1.Length + encoded2.Length, encoded3.Length);
            Array.Copy(encoded4, 0, combined, encoded0.Length + encoded1.Length + encoded2.Length + encoded3.Length, encoded4.Length);

            var decoded = Frame.DecodeFrameBuffer(combined, 0, combined.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(5, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);

            Assert.AreEqual(FrameType.SessionStart, decoded[0].Type);
            Assert.AreEqual(10, decoded[0].SessionID);
            Assert.NotNull(decoded[0].Payload);
            var decodedDescriptor0 = BsonCodec.Decode<SessionDescriptor>(decoded[0].Payload);
            Assert.AreEqual("0", decodedDescriptor0.Name);

            Assert.AreEqual(FrameType.SessionStart, decoded[1].Type);
            Assert.AreEqual(11, decoded[1].SessionID);
            Assert.NotNull(decoded[1].Payload);
            var decodedDescriptor1 = BsonCodec.Decode<SessionDescriptor>(decoded[1].Payload);
            Assert.AreEqual("1", decodedDescriptor1.Name);

            Assert.AreEqual(FrameType.SessionStart, decoded[2].Type);
            Assert.AreEqual(12, decoded[2].SessionID);
            Assert.NotNull(decoded[2].Payload);
            var decodedDescriptor2 = BsonCodec.Decode<SessionDescriptor>(decoded[2].Payload);
            Assert.AreEqual("2", decodedDescriptor2.Name);

            Assert.AreEqual(FrameType.SessionStart, decoded[3].Type);
            Assert.AreEqual(13, decoded[3].SessionID);
            Assert.NotNull(decoded[3].Payload);
            var decodedDescriptor3 = BsonCodec.Decode<SessionDescriptor>(decoded[3].Payload);
            Assert.AreEqual("3", decodedDescriptor3.Name);

            Assert.AreEqual(FrameType.SessionStart, decoded[4].Type);
            Assert.AreEqual(14, decoded[4].SessionID);
            Assert.NotNull(decoded[4].Payload);
            var decodedDescriptor4 = BsonCodec.Decode<SessionDescriptor>(decoded[4].Payload);
            Assert.AreEqual("4", decodedDescriptor4.Name);
        }

        #region Frame Type Tests

        [Test]
        public void EncodeDecode_ConnectionAccept()
        {
            var encoded = Frame.CreateConnectionAcceptFrame();
            var decoded = Frame.DecodeFrameBuffer(encoded, 0, encoded.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(1, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
            Assert.AreEqual(FrameType.ConnectionAccept, decoded[0].Type);
        }

        [Test]
        public void EncodeDecode_ConnectionReject()
        {
            var encoded = Frame.CreateConnectionRejectFrame("ErrorMessage");
            var decoded = Frame.DecodeFrameBuffer(encoded, 0, encoded.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(1, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
            Assert.AreEqual(FrameType.ConnectionReject, decoded[0].Type);
        }

        [Test]
        public void ConnectionOpen()
        {
            var encoded = Frame.CreateConnectionOpenFrame();
            var decoded = Frame.DecodeFrameBuffer(encoded, 0, encoded.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(1, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
            Assert.AreEqual(FrameType.ConnectionOpen, decoded[0].Type);
        }

        [Test]
        public void ConnectionClose()
        {
            var encoded = Frame.CreateConnectionCloseFrame();
            var decoded = Frame.DecodeFrameBuffer(encoded, 0, encoded.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(1, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
            Assert.AreEqual(FrameType.ConnectionClose, decoded[0].Type);
        }

        [Test]
        public void SessionStart()
        {
            var data = new Dictionary<string, object>
            {
                { "data", "Frame.CreateSessionStartFrame" }
            };

            var encoded = Frame.CreateSessionStartFrame(99, BsonCodec.Encode(data));
            var decoded = Frame.DecodeFrameBuffer(encoded, 0, encoded.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(1, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
            Assert.AreEqual(FrameType.SessionStart, decoded[0].Type);
            Assert.AreEqual(99, decoded[0].SessionID);
            Assert.NotNull(decoded[0].Payload);
            var decodedData = BsonCodec.Decode<Dictionary<string, object>>(decoded[0].Payload);
            Assert.NotNull(decodedData);
            Assert.IsTrue(decodedData.ContainsKey("data"));
            Assert.AreEqual("Frame.CreateSessionStartFrame", (string)decodedData["data"]);
        }

        [Test]
        public void SessionAccept()
        {
            var data = new Dictionary<string, object>
            {
                { "data", "Frame.CreateSessionAcceptFrame" }
            };

            var encoded = Frame.CreateSessionAcceptFrame(99, BsonCodec.Encode(data));
            var decoded = Frame.DecodeFrameBuffer(encoded, 0, encoded.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(1, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
            Assert.AreEqual(FrameType.SessionAccept, decoded[0].Type);
            Assert.AreEqual(99, decoded[0].SessionID);
            Assert.NotNull(decoded[0].Payload);
            var decodedData = BsonCodec.Decode<Dictionary<string, object>>(decoded[0].Payload);
            Assert.NotNull(decodedData);
            Assert.IsTrue(decodedData.ContainsKey("data"));
            Assert.AreEqual("Frame.CreateSessionAcceptFrame", (string)decodedData["data"]);
        }

        [Test]
        public void SessionReject()
        {
            var data = new Dictionary<string, object>
            {
                { "data", "Frame.CreateSessionRejectFrame" }
            };

            var encoded = Frame.CreateSessionRejectFrame(99, BsonCodec.Encode(data));
            var decoded = Frame.DecodeFrameBuffer(encoded, 0, encoded.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(1, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
            Assert.AreEqual(FrameType.SessionReject, decoded[0].Type);
            Assert.AreEqual(99, decoded[0].SessionID);
            Assert.NotNull(decoded[0].Payload);
            var decodedData = BsonCodec.Decode<Dictionary<string, object>>(decoded[0].Payload);
            Assert.NotNull(decodedData);
            Assert.IsTrue(decodedData.ContainsKey("data"));
            Assert.AreEqual("Frame.CreateSessionRejectFrame", (string)decodedData["data"]);
        }

        [Test]
        public void SessionOpen()
        {
            var data = new Dictionary<string, object>
            {
                { "data", "Frame.CreateSessionOpenFrame" }
            };

            var encoded = Frame.CreateSessionOpenFrame(99, BsonCodec.Encode(data));
            var decoded = Frame.DecodeFrameBuffer(encoded, 0, encoded.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(1, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
            Assert.AreEqual(FrameType.SessionOpen, decoded[0].Type);
            Assert.AreEqual(99, decoded[0].SessionID);
            Assert.NotNull(decoded[0].Payload);
            var decodedData = BsonCodec.Decode<Dictionary<string, object>>(decoded[0].Payload);
            Assert.NotNull(decodedData);
            Assert.IsTrue(decodedData.ContainsKey("data"));
            Assert.AreEqual("Frame.CreateSessionOpenFrame", (string)decodedData["data"]);
        }

        [Test]
        public void SessionClose()
        {
            var encoded = Frame.CreateSessionCloseFrame(99);
            var decoded = Frame.DecodeFrameBuffer(encoded, 0, encoded.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(1, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
            Assert.AreEqual(FrameType.SessionClose, decoded[0].Type);
            Assert.AreEqual(99, decoded[0].SessionID);
            Assert.NotNull(decoded[0].Payload);
        }

        [Test]
        public void MessageSend()
        {
            var data = new Dictionary<string, object>
            {
                { "data", "Frame.CreateMessageSendFrame" }
            };

            var encoded = Frame.CreateMessageSendFrame(99, 36, BsonCodec.Encode(data));
            var decoded = Frame.DecodeFrameBuffer(encoded, 0, encoded.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(1, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
            Assert.AreEqual(FrameType.MessageSend, decoded[0].Type);
            Assert.AreEqual(99, decoded[0].SessionID);
            Assert.AreEqual(36, decoded[0].MessageID);
            Assert.NotNull(decoded[0].Payload);
            var decodedData = BsonCodec.Decode<Dictionary<string, object>>(decoded[0].Payload);
            Assert.NotNull(decodedData);
            Assert.IsTrue(decodedData.ContainsKey("data"));
            Assert.AreEqual("Frame.CreateMessageSendFrame", (string)decodedData["data"]);
        }

        [Test]
        public void MessageAccept()
        {
            var data = new Dictionary<string, object>
            {
                { "data", "Frame.CreateMessageAcceptFrame" }
            };

            var encoded = Frame.CreateMessageAcceptFrame(99, 36, BsonCodec.Encode(data));
            var decoded = Frame.DecodeFrameBuffer(encoded, 0, encoded.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(1, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
            Assert.AreEqual(FrameType.MessageAccept, decoded[0].Type);
            Assert.AreEqual(99, decoded[0].SessionID);
            Assert.AreEqual(36, decoded[0].MessageID);
            Assert.NotNull(decoded[0].Payload);
            var decodedData = BsonCodec.Decode<Dictionary<string, object>>(decoded[0].Payload);
            Assert.NotNull(decodedData);
            Assert.IsTrue(decodedData.ContainsKey("data"));
            Assert.AreEqual("Frame.CreateMessageAcceptFrame", (string)decodedData["data"]);
        }

        [Test]
        public void MessageReject()
        {
            var data = new Dictionary<string, object>
            {
                { "data", "Frame.CreateMessageRejectFrame" }
            };

            var encoded = Frame.CreateMessageRejectFrame(99, 36, BsonCodec.Encode(data));
            var decoded = Frame.DecodeFrameBuffer(encoded, 0, encoded.Length, out var newOffset, out var newAvailable);

            Assert.AreEqual(1, decoded.Count);
            Assert.AreEqual(0, newOffset);
            Assert.AreEqual(0, newAvailable);
            Assert.AreEqual(FrameType.MessageReject, decoded[0].Type);
            Assert.AreEqual(99, decoded[0].SessionID);
            Assert.AreEqual(36, decoded[0].MessageID);
            Assert.NotNull(decoded[0].Payload);
            var decodedData = BsonCodec.Decode<Dictionary<string, object>>(decoded[0].Payload);
            Assert.NotNull(decodedData);
            Assert.IsTrue(decodedData.ContainsKey("data"));
            Assert.AreEqual("Frame.CreateMessageRejectFrame", (string)decodedData["data"]);
        }

        #endregion
    }
}
