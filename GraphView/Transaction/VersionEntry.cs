﻿using System.Runtime.CompilerServices;

namespace GraphView.Transaction
{
    using System.Runtime.Serialization;
    using System;
    using System.Collections.Generic;

    public class VersionEntry : IComparable<VersionEntry>
    {
        /// <summary>
        /// The offsets of fields in serialized binary data
        /// </summary>
        private static readonly int BEGIN_TIMESTAMP_OFFSET = 0;
        private static readonly int END_TIMESTAMP_OFFSET = 1 * 8;
        private static readonly int TXID_OFFSET = 2 * 8;
        private static readonly int MAX_COMMIT_TS_OFFSET = 3 * 8;
        private static readonly int RECORD_OFFSET = 4 * 8;

        /// <summary>
        /// Default values of the version entry
        /// </summary>
        public static readonly long DEFAULT_BEGIN_TIMESTAMP = -1L;
        public static readonly long DEFAULT_END_TIMESTAMP = -1L;
        public static readonly long DEFAULT_MAX_COMMIT_TS = 0L;

        public static readonly object EMPTY_RECORD = "";
        public static readonly long EMPTY_TXID = -1L;

        public static readonly long VERSION_KEY_START_INDEX = 0L;

        // The following three properties are readonly
        internal object RecordKey { get; set; }
        internal long VersionKey;
        internal object Record { get; set; }

        // The following properties may be changed during the lifetime
        // of a tx, after a version entry is created
        internal long BeginTimestamp;
        internal long EndTimestamp;
        internal long TxId { get; set; }
        internal long MaxCommitTs { get; set; }

        internal int latch = 0;

        public VersionEntry()
        {
            this.Reset();
        }

        public VersionEntry(
            object recordKey,
            long versionKey,
            long beginTimestamp,
            long endTimestamp,
            object record,
            long txId,
            long maxCommitTs)
        {
            this.RecordKey = recordKey;
            this.VersionKey = versionKey;
            this.BeginTimestamp = beginTimestamp;
            this.EndTimestamp = endTimestamp;
            this.Record = record;
            this.TxId = txId;
            this.MaxCommitTs = maxCommitTs;
        }

        public VersionEntry(
            object recordKey,
            long versionKey,
            object record,
            long txId)
        {
            this.RecordKey = recordKey;
            this.VersionKey = versionKey;
            this.BeginTimestamp = VersionEntry.DEFAULT_BEGIN_TIMESTAMP;
            this.EndTimestamp = VersionEntry.DEFAULT_END_TIMESTAMP;
            this.Record = record;
            this.TxId = txId;
            this.MaxCommitTs = VersionEntry.DEFAULT_MAX_COMMIT_TS;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + this.VersionKey.GetHashCode();
            hash = hash * 23 + this.RecordKey.GetHashCode();

            return hash;
        }

        public static void CopyValue(VersionEntry src, VersionEntry dst)
        {
            dst.RecordKey = src.RecordKey;
            dst.VersionKey = src.VersionKey;
            dst.BeginTimestamp = src.BeginTimestamp;
            dst.EndTimestamp = src.EndTimestamp;
            dst.TxId = src.TxId;
            dst.Record = src.Record;
            dst.MaxCommitTs = src.MaxCommitTs;
        }

        public void Set(
            object recordKey,
            long versionKey,
            long beginTimestamp,
            long endTimestamp,
            object record,
            long txId,
            long maxCommitTs)
        {
            this.RecordKey = recordKey;
            this.VersionKey = versionKey;
            this.BeginTimestamp = beginTimestamp;
            this.EndTimestamp = endTimestamp;
            this.Record = record;
            this.TxId = txId;
            this.MaxCommitTs = maxCommitTs;
        }

        public void Reset()
        {
            this.RecordKey = null;
            this.VersionKey = VersionEntry.VERSION_KEY_START_INDEX;
            this.BeginTimestamp = VersionEntry.DEFAULT_BEGIN_TIMESTAMP;
            this.EndTimestamp = VersionEntry.DEFAULT_END_TIMESTAMP;
            this.Record = null;
            this.TxId = VersionEntry.EMPTY_TXID;
            this.MaxCommitTs = VersionEntry.DEFAULT_MAX_COMMIT_TS;
        }

        public override bool Equals(object obj)
        {
            VersionEntry ventry = obj as VersionEntry;
            if (ventry == null)
            {
                return false;
            }

            return this.VersionKey == ventry.VersionKey &&
                this.RecordKey == ventry.RecordKey;
        }

        /// <summary>
        /// Serialize essential properties in version entry to bytes array
        /// </summary>
        /// 
        /// The format of bytes stream is like:
        /// ------- 8 bytes------ ------- 8 bytes---- --- 8 bytes------ 8 bytes----- --X bytes----
        /// [beginTimestamp bytes][endTimestamp bytes][txId bytes][maxCommitTs bytes][record bytes]
        /// 
        /// <returns>a byte array</returns>
        public static byte[] Serialize(VersionEntry versionEntry)
        {
            List<byte> byteList = new List<byte>();

            byteList.AddRange(BitConverter.GetBytes(versionEntry.BeginTimestamp));
            byteList.AddRange(BitConverter.GetBytes(versionEntry.EndTimestamp));
            byteList.AddRange(BitConverter.GetBytes(versionEntry.TxId));
            byteList.AddRange(BitConverter.GetBytes(versionEntry.MaxCommitTs));
            byteList.AddRange(BytesSerializer.Serialize(versionEntry.Record));

            return byteList.ToArray();
        }

        /// <summary>
        /// Deserialize a version entry by the given recordKey, versionKey and content bytes
        /// </summary>
        /// <param name="recordKey"></param>
        /// <param name="versionKey"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static VersionEntry Deserialize(object recordKey, long versionKey, byte[] bytes, VersionEntry versionEntry = null)
        {
            long beginTimestamp = BitConverter.ToInt64(bytes, VersionEntry.BEGIN_TIMESTAMP_OFFSET);
            long endTimestamp = BitConverter.ToInt64(bytes, VersionEntry.END_TIMESTAMP_OFFSET);
            long txId = BitConverter.ToInt64(bytes, VersionEntry.TXID_OFFSET);
            long maxCommitTs = BitConverter.ToInt64(bytes, VersionEntry.MAX_COMMIT_TS_OFFSET);

            byte[] recordBytes = new byte[bytes.Length - VersionEntry.RECORD_OFFSET];
            Buffer.BlockCopy(bytes, VersionEntry.RECORD_OFFSET, recordBytes, 0, recordBytes.Length);
            object record = BytesSerializer.Deserialize(recordBytes);

            if (versionEntry == null)
            {
                return new VersionEntry(recordKey, versionKey, beginTimestamp, endTimestamp,
                    record, txId, maxCommitTs);
            }
            else
            {
                versionEntry.Set(recordKey, versionKey, beginTimestamp, endTimestamp,
                    record, txId, maxCommitTs);
                return versionEntry;
            }
        }

        public static VersionEntry InitEmptyVersionEntry(object recordKey, VersionEntry version = null)
        {
            if (version == null)
            {
                return new VersionEntry(recordKey, VersionEntry.VERSION_KEY_START_INDEX,
                    VersionEntry.EMPTY_RECORD, VersionEntry.EMPTY_TXID);
            }
            else
            {
                version.Reset();
                version.RecordKey = recordKey;
                return version;
            }
        }
            

        public static VersionEntry InitFirstVersionEntry(object recordKey, object payload, VersionEntry version = null)
        {
            version = version == null ? new VersionEntry() : version;
            version.Reset();

            version.RecordKey = recordKey;
            version.VersionKey = 0L;
            version.BeginTimestamp = 0L;
            version.EndTimestamp = long.MaxValue;
            version.Record = payload;

            return version;
        }

        public int CompareTo(VersionEntry other)
        {
            // Two version entries are only comparable if they belong to the same record
            if (this.RecordKey != other.RecordKey)
            {
                return -1;
            }
            else
            {
                if (this.VersionKey < other.VersionKey)
                {
                    return -1;
                }
                else if (this.VersionKey == other.VersionKey)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
        }
    }
}
