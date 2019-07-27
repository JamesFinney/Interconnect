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

using System;
using System.Collections.Generic;
using System.Text;
using Force.Crc32;
using System.Linq;
using System.IO;
using Teramine.Interconnect.Models;
using log4net;
using Teramine.Interconnect.Codecs;


namespace Teramine.Interconnect.Framing
{
    public enum FrameType
    {
        ConnectionStart,
        ConnectionAccept,
        ConnectionReject,
        ConnectionOpen,
        ConnectionClose,
        SessionStart,
        SessionAccept,
        SessionReject,
        SessionOpen,
        SessionClose,
        MessageSend,
        MessageAccept,
        MessageReject
    };

    public class Frame
    {
        static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static Version ProtocolVersion { get; } = new Version(1, 0, 0);
        public static int MaxFrameSize = 128 * 1024;

        public uint Length { get; private set; }             // of entire frame
        public FrameType Type { get; private set; }
        public uint SessionID { get; private set; }
        public uint MessageID { get; private set; }
        public byte[] Payload { get; private set; }

        private static byte BREADCRUMB_BYTES = 6;
        private static byte LENGTH_BYTES = 4;
        private static byte TYPE_BYTES = 1;
        private static byte POFF_BYTES = 1;
        private static byte SESSION_ID_BYTES = 4;
        private static byte MESSAGE_ID_BYTES = 4;
        private static byte CRC_BYTES = 4;

        private static Crc32Algorithm _crc32 = new Crc32Algorithm();


        public static byte[] CreateConnectionStartFrame(Dictionary<string,string> data = null)
        {
            var descriptor = new ConnectionDescriptor
            {
                ProtocolVersion = ProtocolVersion,
                MaxFrameSize = MaxFrameSize,
                Data = data
            };

            return _createFrame(FrameType.ConnectionStart, BsonCodec.Encode(descriptor));
        }
        public static byte[] CreateConnectionAcceptFrame()
        {
            var descriptor = new ConnectionDescriptor
            {
                ProtocolVersion = ProtocolVersion,
                MaxFrameSize = MaxFrameSize
            };

            return _createFrame(FrameType.ConnectionAccept, BsonCodec.Encode(descriptor));
        }
        public static byte[] CreateConnectionRejectFrame(string error = null)
        {
            var descriptor = new ConnectionDescriptor
            {
                ProtocolVersion = ProtocolVersion,
                MaxFrameSize = MaxFrameSize,
                Error = error
            };

            return _createFrame(FrameType.ConnectionReject, BsonCodec.Encode(descriptor));
        }
        public static byte[] CreateConnectionOpenFrame()
        {
            return _createFrame(FrameType.ConnectionOpen, null);
        }
        public static byte[] CreateConnectionCloseFrame()
        {
            return _createFrame(FrameType.ConnectionClose, null);
        }

        public static byte[] CreateSessionStartFrame(uint sesionId, byte[] data)
        {
            return _createFrame(FrameType.SessionStart, data, sesionId);
        }
        public static byte[] CreateSessionAcceptFrame(uint sesionId, byte[] data)
        {
            return _createFrame(FrameType.SessionAccept, data, sesionId);
        }
        public static byte[] CreateSessionRejectFrame(uint sesionId, byte[] data)
        {
            return _createFrame(FrameType.SessionReject, data, sesionId);
        }
        public static byte[] CreateSessionOpenFrame(uint sesionId, byte[] data)
        {
            return _createFrame(FrameType.SessionOpen, data, sesionId);
        }
        public static byte[] CreateSessionCloseFrame(uint sesionId)
        {
            return _createFrame(FrameType.SessionClose, null, sesionId);
        }

        public static byte[] CreateMessageSendFrame(uint sesionId, uint messageId, byte[] data)
        {
            return _createFrame(FrameType.MessageSend, data, sesionId, messageId);
        }
        public static byte[] CreateMessageAcceptFrame(uint sesionId, uint messageId, byte[] data)
        {
            return _createFrame(FrameType.MessageAccept, data, sesionId, messageId);
        }
        public static byte[] CreateMessageRejectFrame(uint sesionId, uint messageId, byte[] data)
        {
            return _createFrame(FrameType.MessageReject, data, sesionId, messageId);
        }

        public static List<Frame> DecodeFrameBuffer(byte[] buffer, int offset, int available, out int newOffset, out int newAvailable)
        {
            if (buffer == null) throw new ArgumentNullException();

            var frames = new List<Frame>();
            newOffset = offset;
            newAvailable = available;

            // do we have the minimum - empty payload no extended
            var minBytes = BREADCRUMB_BYTES + LENGTH_BYTES + TYPE_BYTES + POFF_BYTES + CRC_BYTES;
            if (available < minBytes)
            {
                return frames;
            }

            // while there's still data available in the buffer
            while (available > 0)
            {
                // breadcrumb
                if (buffer[offset] != (byte)'I' || 
                    buffer[offset + 1] != (byte)'N' ||
                    buffer[offset + 2] != (byte)'T' ||
                    buffer[offset + 3] != (byte)'E' ||
                    buffer[offset + 4] != (byte)'R' ||
                    buffer[offset + 5] != (byte)'C')
                {
                    available--;
                    offset++;
                    continue;
                }

                // length
                var length = BitConverter.ToUInt32(buffer, offset + BREADCRUMB_BYTES);
                if (available < length)
                {
                    // exit
                    break;
                }

                // CRC (end) - validate data not corrupt FIRST
                var frameCrc = new byte[CRC_BYTES];
                Array.Copy(buffer, offset + length - 4, frameCrc, 0, frameCrc.Length);
                var calculatedCrc = _crc32.ComputeHash(buffer, offset, (int)length - CRC_BYTES);
                if (!frameCrc.SequenceEqual(calculatedCrc))
                {
                    // going to re-search for next packet - if corrupt don't know how/where it's been corrupted (nor can we trust length)
                    available--;
                    offset++;
                    continue;
                }

                // type
                var type = (FrameType)buffer[offset + BREADCRUMB_BYTES + LENGTH_BYTES];

                // POFF
                var extheaderLength = buffer[offset + BREADCRUMB_BYTES + LENGTH_BYTES + TYPE_BYTES] * 4;
                uint sessionId = 0;
                uint messageId = 0;

                if (extheaderLength > 0)
                {
                    // sessionId
                    sessionId = BitConverter.ToUInt32(buffer, offset + BREADCRUMB_BYTES + LENGTH_BYTES + TYPE_BYTES + POFF_BYTES);

                    // messageId
                    messageId = BitConverter.ToUInt32(buffer, offset + BREADCRUMB_BYTES + LENGTH_BYTES + TYPE_BYTES + POFF_BYTES + SESSION_ID_BYTES);
                }

                // payload
                var payloadLength = length - (BREADCRUMB_BYTES + LENGTH_BYTES + TYPE_BYTES + POFF_BYTES + extheaderLength + CRC_BYTES);
                var payloadBytes = new byte[payloadLength];
                Array.Copy(buffer, offset + BREADCRUMB_BYTES + LENGTH_BYTES + TYPE_BYTES + POFF_BYTES + extheaderLength, payloadBytes, 0, payloadLength);

                // create and add the frame
                var frame = new Frame
                {
                    Length = length,
                    Type = type,
                    SessionID = sessionId,
                    MessageID = messageId,
                    Payload = payloadBytes
                };

                frames.Add(frame);

                // move pointers
                available -= (int)length;
                offset += (int)length;
            }

            if (available == 0)
            {
                // move pointer to beginning of buffer
                offset = 0;
            }

            newOffset = offset;
            newAvailable = available;

            return frames;
        }

        private static byte[] _createFrame(FrameType type, byte[] payload, uint sessionId = 0, uint messageId = 0)
        {
            int payloadBytes = payload == null ? 0 : payload.Length;

            // are we using the extended header
            bool useExtendedHeader =
                type == FrameType.SessionStart |
                type == FrameType.SessionAccept |
                type == FrameType.SessionReject |
                type == FrameType.SessionOpen |
                type == FrameType.SessionClose |
                type == FrameType.MessageSend | 
                type == FrameType.MessageAccept | 
                type == FrameType.MessageReject;

            // if so, how many bytes is it
            var extHeaderBytes = useExtendedHeader ? SESSION_ID_BYTES + MESSAGE_ID_BYTES : 0;

            // create the frame buffer
            var frameBuffer = new byte[BREADCRUMB_BYTES + LENGTH_BYTES + TYPE_BYTES + POFF_BYTES + extHeaderBytes + payloadBytes + CRC_BYTES];

            // breadcrumb
            frameBuffer[0] = (byte)'I';
            frameBuffer[1] = (byte)'N';
            frameBuffer[2] = (byte)'T';
            frameBuffer[3] = (byte)'E';
            frameBuffer[4] = (byte)'R';
            frameBuffer[5] = (byte)'C';

            // length
            var lengthBytes = BitConverter.GetBytes(frameBuffer.Length);
            Array.Copy(lengthBytes, 0, frameBuffer, BREADCRUMB_BYTES, lengthBytes.Length);

            // type
            frameBuffer[BREADCRUMB_BYTES + LENGTH_BYTES] = (byte)type;

            // POFF + extended header
            if(useExtendedHeader)
            {
                // set POFF
                frameBuffer[BREADCRUMB_BYTES + LENGTH_BYTES + TYPE_BYTES] = (byte)((SESSION_ID_BYTES + MESSAGE_ID_BYTES) / 4);

                // SessionID 
                var sessionIdBytes = BitConverter.GetBytes(sessionId);
                Array.Copy(sessionIdBytes, 0, frameBuffer, BREADCRUMB_BYTES + LENGTH_BYTES + TYPE_BYTES + POFF_BYTES, sessionIdBytes.Length);

                // MessageID 
                var messageIdBytes = BitConverter.GetBytes(messageId);
                Array.Copy(messageIdBytes, 0, frameBuffer, BREADCRUMB_BYTES + LENGTH_BYTES + TYPE_BYTES + POFF_BYTES + SESSION_ID_BYTES, messageIdBytes.Length);
            }

            // payload
            if(payloadBytes > 0)
                Array.Copy(payload, 0, frameBuffer, BREADCRUMB_BYTES + LENGTH_BYTES + TYPE_BYTES + POFF_BYTES + extHeaderBytes, payloadBytes);

            // CRC
            var crcBytes = _crc32.ComputeHash(frameBuffer, 0, frameBuffer.Length - 4);
            Array.Copy(crcBytes, 0, frameBuffer, BREADCRUMB_BYTES + LENGTH_BYTES + TYPE_BYTES + POFF_BYTES + extHeaderBytes + payloadBytes, crcBytes.Length);

            // end
            return frameBuffer;
        }
    }
}
