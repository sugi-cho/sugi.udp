using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace ilda.digital
{
    [Serializable]
    public class IDNData
    {
        #region Defines

        // UDP protocol
        public const int IDNVAL_HELLO_UDP_PORT = 7255;

        // Packet commands
        public enum IDNCMD
        {
            VOID = 0x00,
            PING_REQUEST = 0x08,    // Can be used for round trip measurements
            PING_RESPONSE = 0x09,    // Request payload data copied into response.

            GROUP_REQUEST = 0x0C,    // Client group mask retrieval/modification
            GROUP_RESPONSE = 0x0D,    // Result and current client group mask

            SCAN_REQUEST = 0x10,    // Network scan for units
            SCAN_RESPONSE = 0x11,    // Unit identification and status
            SERVICEMAP_REQUEST = 0x12,    // Request for unit services
            SERVICEMAP_RESPONSE = 0x13,    // Map of supported services

            SERVICE_PARAMETERS_REQUEST = 0x20,
            SERVICE_PARAMETERS_RESPONSE = 0x21,
            UNIT_PARAMETERS_REQUEST = 0x22,
            UNIT_PARAMETERS_RESPONSE = 0x23,
            LINK_PARAMETERS_REQUEST = 0x28,
            LINK_PARAMETERS_RESPONSE = 0x29,

            CNLMSG = 0x40,    // Realtime channel message (empty: keepalive)
            CNLMSG_ACKREQ = 0x41,    // Realtime channel message + request acknowledge
            CNLMSG_CLOSE = 0x44,    // Gracefully close (if msg: process, then close)
            CNLMSG_CLOSE_ACKREQ = 0x45,    // Gracefully close + request acknowledge
            ACKNOWLEDGE = 0x47,    // Acknowledgement response
        }

        // Packet flags masks
        const byte IDNMSK_PKTFLAGS_GROUP = 0x0F;    // The lower 4 bits are the client group

        // Client group operation codes
        public enum IDNVAL
        {
            GROUPOP_SUCCESS = 0x00,    // Successful operation
            GROUPOP_GETMASK = 0x01,    // Get the group mask
            GROUPOP_SETMASK = 0x02,    // Set the group mask
            GROUPOP_ERR_AUTH = 0xFD,    // Authentication error
            GROUPOP_ERR_OPERATION = 0xFE,    // Invalid operation
            GROUPOP_ERR_REQUEST = 0xFF,    // Invalid request
        }

        // SCAN response status flags
        public enum IDNFLG
        {
            VOID = 0,
            SCAN_STATUS_MALFUNCTION = 0x80,    // The unit has a permanent malfunction
            SCAN_STATUS_OFFLINE = 0x40,    // Currently unavailable (bootup, overheat, eStop)
            SCAN_STATUS_EXCLUDED = 0x20,    // The client group is excluded from streaming
            SCAN_STATUS_OCCUPIED = 0x10,    // All sessions are occupied by clients
            SCAN_STATUS_REALTIME = 0x01,    // Offers realtime streaming through IDN-Hello
        }

        // Realtime command acknowledgement
        const byte IDNVAL_RTACK_SUCCESS = 0x00;    // Successfully accepted
        const byte IDNVAL_RTACK_ERR_NOT_CONNECTED = 0xEB;    // Empty close without connection
        const byte IDNVAL_RTACK_ERR_OCCUPIED = 0xEC;    // All sessions are occupied by clients
        const byte IDNVAL_RTACK_ERR_EXCLUDED = 0xED;    // The client group is excluded from streaming
        const byte IDNVAL_RTACK_ERR_PAYLOAD = 0xEE;    // Invalid payload
        const byte IDNVAL_RTACK_ERR_GENERIC = 0xEF;    // Any other processing error

        const short IDNVAL_RTACK_EVFLG_NEW = 0x0001;  // New connection
        const short IDNVAL_RTACK_EVFLG_SEQERR_LVL1 = 0x0010;  // Sequence error (not strictly increased by 1)
        const short IDNMSK_RTACK_EVFLG_SEQERR = 0x00F0;  // Any sequence error

        // Service types(0x00..0x3F): Interfaces
        public enum IDNVAL_STYPE
        {
            VOID = 0x00,        // Unknown
            UART = 0x04,        // Generic UART interface
            DMX512 = 0x05,        // Generic DMX512 interface
            MIDI = 0x08,        // Generic MIDI interface

            // Service types(0x80..0xBF / 2 LSB): Media types
            LAPRO = 0x80,        // Standard laser projector
            AUDIO = 0x84,        // Standard audio processing
        }

        // Channel message content IDs
        public enum IDNFLG_CONTENTID
        {
            VOUD = 0,
            CHANNELMSG = 0x8000,      // Channel message flag (specific bit assignments)
            CONFIG_LSTFRG = 0x4000,      // Set for config header or last fragment
            CHANNELID = 0x3F00,      // Channel ID bit mask
            CNKTYPE = 0x00FF,      // Data chunk type bit mask
        }

        // Data chunk types - singe message / first fragment
        public enum IDNVAL_CNKTYPE
        {
            VOID = 0x00,        // Empty chunk (no data)
            LPGRF_WAVE = 0x01,        // Sample data array
            LPGRF_FRAME = 0x02,        // Sample data array (entirely)
            LPGRF_FRAME_FIRST = 0x03,        // Sample data array (first fragment)
            OCTET_SEGMENT = 0x10,        // Delimited sequence of octets (can be multiple chunks)
            OCTET_STRING = 0x11,        // Discrete sequence of octets (single chunk)
            DIMMER_LEVELS = 0x18,        // Dimmer levels for DMX512 packets
            AUDIO_WAVE = 0x20,        // Sample data array
        }

        // Data chunk types - fragment sequel
        const byte IDNVAL_CNKTYPE_LPGRF_FRAME_SEQUEL = 0xC0;        // Sample data array (sequel fragment)

        // Channel configuration: Flags
        public enum IDNMSK_CHNCFG
        {
            DATA_MATCH = 0x30,        // Data/Configuration crosscheck
            ROUTING = 0x01,        // Verify/Route/Open channel before message processing
            CLOSE = 0x02,        // Close channel after message processing
        }

        // Channel configuration: Service modes; Note: Must be unique! Used to map "any service" routings
        public enum IDNVAL_SMOD
        {
            VOID = 0x00,        // No function, no lookup
            LPGRF_CONTINUOUS = 0x01,        // Laser graphic: Stream of waveform segments
            LPGRF_DISCRETE = 0x02,        // Laser graphic: Stream of individual frames
            LPEFX_CONTINUOUS = 0x03,        // Transparent, octet segments only (includes start code)
            LPEFX_DISCRETE = 0x04,        // Buffered, frames of effect data, may mix with strings
            DMX512_CONTINUOUS = 0x05,        // Transparent, octet segments only (includes start code)
            DMX512_DISCRETE = 0x06,        // Buffered, frames of effect data, may mix with strings
            AUDIO_CONTINUOUS = 0x0C,        // Audio: Stream of waveform segments
        }

        // Chunk header flags
        const byte IDNMSK_CNKHDR_CONFIG_MATCH = 0x30;        // Data/Configuration crosscheck
        const byte IDNFLG_OCTET_SEGMENT_DELIMITER = 0x01;        // Segment contains octet sequence delimiter
        const byte IDNFLG_GRAPHIC_FRAME_ONCE = 0x01;        // Frame is only scanned once

        // ----------------------------------------------------------------------------
        const int IDNVAL_CHANNEL_COUNT = 64;
        #endregion

        public IDNHeader idnHeader;
        public ChannelMessageHeader channelMessageHeader;
        public ChannelConfigurationHeader channelConfigurationHeader;
        public DescriptorTag[] dictionary;
        public FrameSampleChunkHeader frameSampleChunkHeader;
        public Sample[] samples;

        public static IDNData Parse(byte[] data)
        {
            var idn = new IDNData();
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                idn.idnHeader = new IDNHeader(reader);
                if (idn.Command == IDNCMD.CNLMSG)
                {
                    idn.channelMessageHeader = new ChannelMessageHeader(reader);
                    if (idn.HasChannelConfiguration)
                    {
                        idn.channelConfigurationHeader = new ChannelConfigurationHeader(reader);
                        idn.dictionary = new DescriptorTag[8];//idn‚Ì’†g‚ªidtf -> idn‚Ìê‡‚ÉŒˆ‚ß‘Å‚¿‚µ‚Ä‚éI
                        for (var i = 0; i < 8; i++)
                        {
                            idn.dictionary[i] = (DescriptorTag)reader.ReadUInt16BigEdian();
                        }
                    }

                    idn.frameSampleChunkHeader = new FrameSampleChunkHeader(reader);
                    var numSamples = (
                            idn.TotalSize
                            - (idn.HasChannelConfiguration ? ChannelConfigurationHeader.Size + idn.dictionary.Length * 2 : 0)
                            - FrameSampleChunkHeader.Size
                        ) / Sample.Size;
                    idn.samples = new Sample[numSamples];
                    for (var i = 0; i < numSamples; i++)
                        idn.samples[i] = new Sample(reader);
                }
            }
            return idn;
        }

        public IDNCMD Command => idnHeader.command;
        public int TotalSize => channelMessageHeader.totalSize;
        public bool HasChannelConfiguration => channelMessageHeader.cnl.cclf;
        public int ChannelId => channelMessageHeader.cnl.channelId;
        public Sample[] FrameSamples => samples;

        [Serializable]
        public struct IDNHeader
        {
            public static int size => 4;
            public IDNHeader(BinaryReader reader)
            {
                command = (IDNCMD)reader.ReadByte();
                flags = (IDNFLG)reader.ReadByte();
                sequence = reader.ReadUInt16BigEdian();
            }

            public IDNCMD command;
            public IDNFLG flags;
            public ushort sequence;
        }

        [Serializable]
        public struct ChannelMessageHeader
        {
            public static int Size => 8;
            public ChannelMessageHeader(BinaryReader reader)
            {
                totalSize = reader.ReadUInt16BigEdian();
                cnl = new CNL(reader.ReadByte());
                chunkType = (IDNVAL_CNKTYPE)reader.ReadByte();
                timestamp = reader.ReadUInt32BigEdian();
            }

            public ushort totalSize;
            public CNL cnl;
            public IDNVAL_CNKTYPE chunkType;
            public uint timestamp;

            [Serializable]
            public struct CNL
            {
                public CNL(byte data)
                {
                    cclf = 0 < (data & 0b01000000); //channel configuration flag
                    channelId = (byte)(data & 0b00111111);
                }
                public bool cclf;
                public byte channelId;
            }
        }

        [Serializable]
        public struct ChannelConfigurationHeader
        {
            public static int Size => 4;
            public ChannelConfigurationHeader(BinaryReader reader)
            {
                scwc = reader.ReadByte();
                flags = new Flags(reader.ReadByte());
                serviceId = reader.ReadByte();
                serviceMode = (ServiceMode)reader.ReadByte();

            }

            public int scwc;
            public Flags flags;
            public int serviceId;
            public ServiceMode serviceMode;

            [Serializable]
            public struct Flags
            {
                public Flags(byte data)
                {
                    sdm = (data >> 4) & 0b0011;
                    close = 0 < (data & 0b0010);
                    routing = 0 < (data & 0b0001);
                }
                public int sdm;
                public bool close;
                public bool routing;
            }
            public enum ServiceMode
            {
                Void = 0x00,
                LaserProjectorGraphic_Continuous = 0x01,
                LaserProjectorGraphic_Discrete = 0x02,
                LaserProjectorEffects_Continuous = 0x03,
                LaserProjectorEffects_Discrete = 0x04,
                DMX512_Continuous = 0x05,
                DMX512_Discrete = 0x06,
            }
        }
        public enum DescriptorTag
        {
            VOID = 0,
            Precision = 0x4010,
            X = 0x4200,
            Y = 0x4210,
            Blue460 = 0x51CC,
            Green532 = 0x5214,
            Red638 = 0x527E,
        }

        [Serializable]
        public struct FrameSampleChunkHeader
        {
            public static int Size => 4;
            public FrameSampleChunkHeader(BinaryReader reader)
            {
                var data = reader.ReadBytes(Size);
                flags = new Flags(data[0]);
                Array.Reverse(data);
                duration = BitConverter.ToUInt32(data, 0) & 0X00FFFFFF;
            }

            public Flags flags;
            public uint duration;   //microseconds

            [Serializable]
            public struct Flags
            {
                public Flags(byte data)
                {
                    scm = (data >> 4) & 0b0011;
                    once = 0 < (data & 0b0001);
                }
                public int scm;
                public bool once;
            }
        }
        [Serializable]
        public struct Sample
        {
            public static int Size => 7;
            public Sample(BinaryReader reader)
            {
                position = new Vector2(reader.ReadInt16BigEdian(), reader.ReadInt16BigEdian());
                color = new Color32(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), byte.MaxValue);
            }
            public Vector2 position;
            public Color32 color;
        }
    }
    public static class ReadBigEndian
    {
        public static uint ReadUInt32BigEdian(this BinaryReader reader)
        {
            var array = reader.ReadBytes(4);
            Array.Reverse(array);
            return BitConverter.ToUInt32(array, 0);
        }
        public static ushort ReadUInt16BigEdian(this BinaryReader reader)
        {
            var array = reader.ReadBytes(2);
            Array.Reverse(array);
            return BitConverter.ToUInt16(array, 0);
        }
        public static short ReadInt16BigEdian(this BinaryReader reader)
        {
            var array = reader.ReadBytes(2);
            Array.Reverse(array);
            return BitConverter.ToInt16(array, 0);
        }
    }
}