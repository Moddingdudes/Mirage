using System;
using System.Collections.Generic;
using System.IO;
using Mirage.Serialization;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
// The following resolves a CS0104 error because apparently there's the same class
// in System.Collections.Generic with the same name, and Unity complains about it.
// First sighted on 2021.3.3f1. May affect all 2021 versions...? Also thanks to
// Hertzole on Discord for pointing out "UNITY_2021" might not exist in 2022.
#if UNITY_2021_1_OR_NEWER
using CollectionExtensions = Mirage.Serialization.CollectionExtensions;
#endif


namespace Mirage.Tests.Runtime.Serialization
{
    [TestFixture]
    public class NetworkWriterTest : TestBase
    {
        private readonly NetworkWriter writer = new NetworkWriter(1300);
        private readonly MirageNetworkReader reader = new MirageNetworkReader();

        [SetUp]
        public void Setup()
        {
            // set new ObjectLocator
            reader.ObjectLocator = Substitute.For<IObjectLocator>();
        }
        [TearDown]
        public void TearDown()
        {
            reader.Dispose();
            writer.Reset();
            TearDownTestObjects();
        }


        [Test]
        public void TestWritingBytesSegment()
        {
            byte[] data = { 1, 2, 3 };
            writer.WriteBytes(data, 0, data.Length);

            reader.Reset(writer.ToArraySegment());
            var deserialized = reader.ReadBytesSegment(data.Length);
            Assert.That(deserialized.Count, Is.EqualTo(data.Length));
            for (var i = 0; i < data.Length; ++i)
                Assert.That(deserialized.Array[deserialized.Offset + i], Is.EqualTo(data[i]));
        }

        // write byte[], read segment
        [Test]
        public void TestWritingBytesAndReadingSegment()
        {
            byte[] data = { 1, 2, 3 };
            writer.WriteBytesAndSize(data);

            reader.Reset(writer.ToArraySegment());
            var deserialized = reader.ReadBytesAndSizeSegment();
            Assert.That(deserialized.Count, Is.EqualTo(data.Length));
            for (var i = 0; i < data.Length; ++i)
                Assert.That(deserialized.Array[deserialized.Offset + i], Is.EqualTo(data[i]));
        }

        // write segment, read segment
        [Test]
        public void TestWritingSegmentAndReadingSegment()
        {
            byte[] data = { 1, 2, 3, 4 };
            // [2, 3]
            var segment = new ArraySegment<byte>(data, 1, 1);
            writer.WriteBytesAndSizeSegment(segment);

            reader.Reset(writer.ToArraySegment());
            var deserialized = reader.ReadBytesAndSizeSegment();
            Assert.That(deserialized.Count, Is.EqualTo(segment.Count));
            for (var i = 0; i < segment.Count; ++i)
                Assert.That(deserialized.Array[deserialized.Offset + i], Is.EqualTo(segment.Array[segment.Offset + i]));
        }

        [Test]
        public void TestMaxStringWrite()
        {
            Assert.Throws<DataMisalignedException>(() =>
            {
                writer.WriteString(new string('*', StringExtensions.MaxStringLength));
            });
        }

        [Test]
        public void TestResetSetsPotionAndLength()
        {
            writer.WriteString("I saw");
            writer.WriteInt64(0xA_FADED_DEAD_EEL);
            writer.WriteString("and ate it");
            writer.Reset();

            Assert.That(writer.BitPosition, Is.EqualTo(0));
            Assert.That(writer.ByteLength, Is.EqualTo(0));

            var data = writer.ToArray();
            Assert.That(data, Is.Empty);
        }

        [Test]
        public void TestReadingLengthWrapAround()
        {
            // This is 1.5x int.MaxValue, in the negative range of int.
            writer.WritePackedUInt32(3221225472);
            reader.Reset(writer.ToArraySegment());
            Assert.Throws<OverflowException>(() => reader.ReadBytesAndSize());
        }

        [Test]
        public void TestReading0LengthBytesAndSize()
        {
            writer.WriteBytesAndSize(new byte[] { });
            reader.Reset(writer.ToArraySegment());
            Assert.That(reader.ReadBytesAndSize().Length, Is.EqualTo(0));
        }

        [Test]
        public void TestReading0LengthBytes()
        {
            writer.WriteBytes(new byte[] { }, 0, 0);
            reader.Reset(writer.ToArraySegment());
            Assert.That(reader.ReadBytes(0).Length, Is.EqualTo(0));
        }

        [Test]
        public void TestWritingNegativeBytesAndSizeFailure()
        {
            Assert.Throws<OverflowException>(() => writer.WriteBytesAndSize(new byte[0], 0, -1));
            Assert.That(writer.ByteLength, Is.EqualTo(0));
        }

        [Test]
        public void TestReadingTooMuch()
        {
            void EnsureThrows(Action<NetworkReader> read)
            {
                Assert.Throws<System.IO.EndOfStreamException>(() =>
                {
                    reader.Reset(new byte[0]);
                    read.Invoke(reader);
                });
            }
            // Try reading more than there is data to be read from
            // This should throw EndOfStreamException always
            EnsureThrows(r => r.ReadByte());
            EnsureThrows(r => r.ReadSByte());
            EnsureThrows(r => r.ReadChar());
            EnsureThrows(r => r.ReadBoolean());
            EnsureThrows(r => r.ReadInt16());
            EnsureThrows(r => r.ReadUInt16());
            EnsureThrows(r => r.ReadInt32());
            EnsureThrows(r => r.ReadUInt32());
            EnsureThrows(r => r.ReadInt64());
            EnsureThrows(r => r.ReadUInt64());
            EnsureThrows(r => r.ReadUInt64());
            EnsureThrows(r => r.ReadDecimalConverter());
            EnsureThrows(r => r.ReadSingle());
            EnsureThrows(r => r.ReadDouble());
            EnsureThrows(r => r.ReadString());
            EnsureThrows(r => r.ReadBytes(1));
            EnsureThrows(r => r.ReadBytes(2));
            EnsureThrows(r => r.ReadBytes(3));
            EnsureThrows(r => r.ReadBytes(4));
            EnsureThrows(r => r.ReadBytes(8));
            EnsureThrows(r => r.ReadBytes(16));
            EnsureThrows(r => r.ReadBytes(32));
            EnsureThrows(r => r.ReadBytes(100));
            EnsureThrows(r => r.ReadBytes(1000));
            EnsureThrows(r => r.ReadBytes(10000));
            EnsureThrows(r => r.ReadBytes(1000000));
            EnsureThrows(r => r.ReadBytes(10000000));
            EnsureThrows(r => r.ReadBytesAndSize());
            EnsureThrows(r => r.ReadPackedInt32());
            EnsureThrows(r => r.ReadPackedUInt32());
            EnsureThrows(r => r.ReadPackedInt64());
            EnsureThrows(r => r.ReadPackedUInt64());
            EnsureThrows(r => r.ReadVector2());
            EnsureThrows(r => r.ReadVector3());
            EnsureThrows(r => r.ReadVector4());
            EnsureThrows(r => r.ReadVector2Int());
            EnsureThrows(r => r.ReadVector3Int());
            EnsureThrows(r => r.ReadColor());
            EnsureThrows(r => r.ReadColor32());
            EnsureThrows(r => r.ReadQuaternion());
            EnsureThrows(r => r.ReadRect());
            EnsureThrows(r => r.ReadPlane());
            EnsureThrows(r => r.ReadRay());
            EnsureThrows(r => r.ReadMatrix4x4());
            EnsureThrows(r => r.ReadGuid());
        }

        private static readonly Vector2[] vector2s = {
            Vector2.right,
                Vector2.up,
                Vector2.zero,
                Vector2.one,
                Vector2.positiveInfinity,
                new Vector2(0.1f,3.1f)
        };

        [Test, TestCaseSource(nameof(vector2s))]
        public void TestVector2(Vector2 vector)
        {
            writer.WriteVector2(vector);
            reader.Reset(writer.ToArraySegment());
            var output = reader.ReadVector2();
            Assert.That(output, Is.EqualTo(vector));
        }

        private static readonly Vector3[] vector3s = {
                Vector3.right,
                Vector3.up,
                Vector3.zero,
                Vector3.one,
                Vector3.positiveInfinity,
                Vector3.forward,
                new Vector3(0.1f,3.1f,1.4f)
        };

        [Test, TestCaseSource(nameof(vector3s))]
        public void TestVector3(Vector3 input)
        {
            writer.WriteVector3(input);
            reader.Reset(writer.ToArraySegment());
            var output = reader.ReadVector3();
            Assert.That(output, Is.EqualTo(input));
        }

        private static readonly Vector4[] vector4s = {
                Vector3.right,
                Vector3.up,
                Vector4.zero,
                Vector4.one,
                Vector4.positiveInfinity,
                new Vector4(0.1f,3.1f,1.4f,4.9f)
        };

        [Test, TestCaseSource(nameof(vector4s))]
        public void TestVector4(Vector4 input)
        {
            writer.WriteVector4(input);
            reader.Reset(writer.ToArraySegment());
            var output = reader.ReadVector4();
            Assert.That(output, Is.EqualTo(input));
        }

        private static readonly Vector2Int[] vector2Ints = {
                Vector2Int.down,
                Vector2Int.up,
                Vector2Int.left,
                Vector2Int.zero,
                new Vector2Int(-1023,-999999),
                new Vector2Int(257,12345),
                new Vector2Int(0x7fffffff,-12345)
        };

        [Test, TestCaseSource(nameof(vector2Ints))]
        public void TestVector2Int(Vector2Int input)
        {
            writer.WriteVector2Int(input);
            reader.Reset(writer.ToArraySegment());
            var output = reader.ReadVector2Int();
            Assert.That(output, Is.EqualTo(input));
        }

        private static readonly Vector3Int[] vector3Ints = {
                Vector3Int.down,
                Vector3Int.up,
                Vector3Int.left,
                Vector3Int.one,
                Vector3Int.zero,
                new Vector3Int(-1023,-999999,1392),
                new Vector3Int(257,12345,-6132),
                new Vector3Int(0x7fffffff,-12345,-1)
        };

        [Test, TestCaseSource(nameof(vector3Ints))]
        public void TestVector3Int(Vector3Int input)
        {
            writer.WriteVector3Int(input);
            reader.Reset(writer.ToArraySegment());
            var output = reader.ReadVector3Int();
            Assert.That(output, Is.EqualTo(input));
        }

        private static readonly Color[] colors = {
                Color.black,
                Color.blue,
                Color.cyan,
                Color.yellow,
                Color.magenta,
                Color.white,
                new Color(0.401f,0.2f,1.0f,0.123f)
        };

        [Test, TestCaseSource(nameof(colors))]
        public void TestColor(Color input)
        {
            writer.WriteColor(input);
            reader.Reset(writer.ToArraySegment());
            var output = reader.ReadColor();
            Assert.That(output, Is.EqualTo(input));
        }

        private static readonly Color32[] color32s = {
                Color.black,
                Color.blue,
                Color.cyan,
                Color.yellow,
                Color.magenta,
                Color.white,
                new Color32(0xab,0xcd,0xef,0x12),
                new Color32(125,126,0,255)
        };

        [Test, TestCaseSource(nameof(color32s))]
        public void TestColor32(Color32 input)
        {
            writer.WriteColor32(input);
            reader.Reset(writer.ToArraySegment());
            var output = reader.ReadColor32();
            Assert.That(output, Is.EqualTo(input));
        }

        private static readonly Rect[] rects = {
                Rect.zero,
                new Rect(1004.1f,2.001f,4636,400f),
                new Rect(-100.622f,-200f,300f,975.6f),
                new Rect(-100f,435,-30.04f,400f),
                new Rect(55,-200f,-44,-123)
        };

        [Test, TestCaseSource(nameof(rects))]
        public void TestRect(Rect input)
        {
            writer.WriteRect(input);
            reader.Reset(writer.ToArraySegment());
            var output = reader.ReadRect();
            Assert.That(output, Is.EqualTo(input));
        }

        private static readonly Plane[] planes = {
                new Plane(new Vector3(-0.24f,0.34f,0.2f), 120.2f),
                new Plane(new Vector3(0.133f,0.34f,0.122f), -10.135f),
                new Plane(new Vector3(0.133f,-0.0f,float.MaxValue), -13.3f),
                new Plane(new Vector3(0.1f,-0.2f,0.3f), 14.5f)
        };

        [Test, TestCaseSource(nameof(planes))]
        public void TestPlane(Plane input)
        {
            writer.WritePlane(input);
            reader.Reset(writer.ToArraySegment());
            var output = reader.ReadPlane();
            // note: Plane constructor does math internally, resulting in
            // floating point precision loss that causes exact comparison
            // to fail the test. So we test that the difference is small.
            Assert.That((output.normal - input.normal).magnitude, Is.LessThan(1e-6f));
            Assert.That(output.distance, Is.EqualTo(input.distance));
        }

        private static readonly Ray[] rays = {
                new Ray(Vector3.up,Vector3.down),
                new Ray(new Vector3(0.1f,0.2f,0.3f), new Vector3(0.4f,0.5f,0.6f)),
                new Ray(new Vector3(-0.3f,0.5f,0.999f), new Vector3(1f,100.1f,20f))
        };

        [Test, TestCaseSource(nameof(rays))]
        public void TestRay(Ray input)
        {
            writer.WriteRay(input);
            reader.Reset(writer.ToArraySegment());
            var output = reader.ReadRay();
            Assert.That((output.direction - input.direction).magnitude, Is.LessThan(1e-6f));
            Assert.That(output.origin, Is.EqualTo(input.origin));
        }

        private static readonly Matrix4x4[] matrix4X4s = {
                Matrix4x4.identity,
                Matrix4x4.zero,
                Matrix4x4.Scale(Vector3.one * 0.12345f),
                Matrix4x4.LookAt(Vector2.up,Vector3.right,Vector3.forward),
                Matrix4x4.Rotate(Quaternion.LookRotation(Vector3.one))
        };

        [Test, TestCaseSource(nameof(matrix4X4s))]
        public void TestMatrix(Matrix4x4 input)
        {
            writer.WriteMatrix4X4(input);
            reader.Reset(writer.ToArraySegment());
            var output = reader.ReadMatrix4x4();
            Assert.That(output, Is.EqualTo(input));
        }

        // These are all bytes which never show up in valid UTF8 encodings.
        // NetworkReader should gracefully handle maliciously crafted input.
        private static readonly byte[] invalidUTF8bytes = {
                0xC0, 0xC1, 0xF5, 0xF6,
                0xF7, 0xF8, 0xF9, 0xFA,
                0xFB, 0xFC, 0xFD, 0xFE,
                0xFF
            };

        [Test, TestCaseSource(nameof(invalidUTF8bytes))]
        public void TestReadingInvalidString(byte invalid)
        {
            writer.WriteString("an uncorrupted string");
            var data = writer.ToArray();
            data[10] = invalid;
            reader.Reset(data);
            Assert.Throws<System.Text.DecoderFallbackException>(() => reader.ReadString());
        }

        [Test]
        public void TestReadingTruncatedString()
        {
            const string str = "a string longer than 10 bytes";
            writer.WriteString(str);
            // change length value to longer than string
            // +2 because length is already +1 to handle null
            writer.WriteAtPosition((ushort)(str.Length + 2), 16, 0);

            reader.Reset(writer.ToArraySegment());
            Assert.Throws<System.IO.EndOfStreamException>(() => reader.ReadString());
        }

        [Test]
        public void WriteAtPositionTest()
        {
            // write 2 bytes
            writer.WriteByte(1);
            writer.WriteByte(2);

            // .ToArray() length is 2?
            Assert.That(writer.ToArray().Length, Is.EqualTo(2));

            // set position back by one
            writer.WriteAtPosition(2, 8, 8);

            // Changing the position should not alter the size of the data
            Assert.That(writer.ToArray().Length, Is.EqualTo(2));
        }

        [Test]
        public void WriteAtBytePositionTest()
        {
            // write 2 bytes
            writer.WriteByte(1);
            writer.WriteByte(2);

            // .ToArray() length is 2?
            Assert.That(writer.ToArray().Length, Is.EqualTo(2));

            // set position back by one
            writer.WriteAtBytePosition(2, 8, 1);

            // Changing the position should not alter the size of the data
            Assert.That(writer.ToArray().Length, Is.EqualTo(2));
        }

        [Test]
        public void TestToArraySegment()
        {
            writer.WriteString("hello");
            writer.WriteString("world");

            reader.Reset(writer.ToArraySegment());
            Assert.That(reader.ReadString(), Is.EqualTo("hello"));
            Assert.That(reader.ReadString(), Is.EqualTo("world"));
        }

        [Test]
        public void TestChar()
        {
            var a = 'a';
            var u = 'ⓤ';

            writer.WriteChar(a);
            writer.WriteChar(u);
            reader.Reset(writer.ToArraySegment());
            var a2 = reader.ReadChar();
            Assert.That(a2, Is.EqualTo(a));
            var u2 = reader.ReadChar();
            Assert.That(u2, Is.EqualTo(u));
        }

        private static readonly string[] weirdUnicode = {
                "𝔲𝔫𝔦𝔠𝔬𝔡𝔢 𝔱𝔢𝔰𝔱",
                "𝖚𝖓𝖎𝖈𝖔𝖉𝖊 𝖙𝖊𝖘𝖙",
                "𝐮𝐧𝐢𝐜𝐨𝐝𝐞 𝐭𝐞𝐬𝐭",
                "𝘶𝘯𝘪𝘤𝘰𝘥𝘦 𝘵𝘦𝘴𝘵",
                "𝙪𝙣𝙞𝙘𝙤𝙙𝙚 𝙩𝙚𝙨𝙩",
                "𝚞𝚗𝚒𝚌𝚘𝚍𝚎 𝚝𝚎𝚜𝚝",
                "𝓊𝓃𝒾𝒸𝑜𝒹𝑒 𝓉𝑒𝓈𝓉",
                "𝓾𝓷𝓲𝓬𝓸𝓭𝓮 𝓽𝓮𝓼𝓽",
                "𝕦𝕟𝕚𝕔𝕠𝕕𝕖 𝕥𝕖𝕤𝕥",
                "ЦПIᄃӨDΣ ƬΣƧƬ",
                "ㄩ几丨匚ㄖᗪ乇 ㄒ乇丂ㄒ",
                "ひ刀ﾉᄃのり乇 ｲ乇丂ｲ",
                "Ʉ₦ł₵ØĐɆ ₮Ɇ₴₮",
                "ｕｎｉｃｏｄｅ ｔｅｓｔ",
                "ᴜɴɪᴄᴏᴅᴇ ᴛᴇꜱᴛ",
                "ʇsǝʇ ǝpoɔıun",
                "ยภเς๏๔є ՇєรՇ",
                "ᑘᘉᓰᑢᓍᕲᘿ ᖶᘿSᖶ",
                "υɳιƈσԃҽ ƚҽʂƚ",
                "ʊռɨƈօɖɛ ȶɛֆȶ",
                "🆄🅽🅸🅲🅾🅳🅴 🆃🅴🆂🆃",
                "ⓤⓝⓘⓒⓞⓓⓔ ⓣⓔⓢⓣ",
                "̶̝̳̥͈͖̝͌̈͛̽͊̏̚͠",
                // test control codes
                "\r\n", "\n", "\r", "\t",
                "\\", "\"", "\'",
                "\u0000\u0001\u0002\u0003",
                "\u0004\u0005\u0006\u0007",
                "\u0008\u0009\u000A\u000B",
                "\u000C\u000D\u000E\u000F",
                // test invalid bytes as characters
                "\u00C0\u00C1\u00F5\u00F6",
                "\u00F7\u00F8\u00F9\u00FA",
                "\u00FB\u00FC\u00FD\u00FE",
                "\u00FF"
        };

        [Test, TestCaseSource(nameof(weirdUnicode))]
        public void TestUnicodeString(string weird)
        {
            writer.WriteString(weird);
            reader.Reset(writer.ToArraySegment());
            var str = reader.ReadString();
            Assert.That(str, Is.EqualTo(weird));
        }

        private static readonly uint[] uint32s =
        {
            0,
            234,
            2284,
            67821,
            16777210,
            16777219,
            uint.MaxValue
        };

        [Test, TestCaseSource(nameof(uint32s))]
        public void TestPackedUInt32(uint value)
        {
            writer.WritePackedUInt32(value);
            reader.Reset(writer.ToArraySegment());
            Assert.That(reader.ReadPackedUInt32(), Is.EqualTo(value));
        }

        [Test, TestCaseSource(nameof(uint32s))]
        public void TestUInt32(uint value)
        {
            writer.WriteUInt32(value);
            reader.Reset(writer.ToArraySegment());
            Assert.That(reader.ReadUInt32(), Is.EqualTo(value));
        }

        private static readonly long[] int32Fail =
        {
            1099511627775,
            281474976710655,
            72057594037927935
        };

        [Test, TestCaseSource(nameof(int32Fail))]
        public void TestPackedUInt32Failure(long data)
        {
            Assert.Throws<OverflowException>(() =>
            {
                writer.WritePackedUInt64((ulong)data);
                reader.Reset(writer.ToArraySegment());
                reader.ReadPackedUInt32();
            });
        }

        private static readonly int[] int32s =
        {
            0,
            234,
            2284,
            67821,
            16777210,
            16777219,
            int.MaxValue,
            -1,
            -234,
            -2284,
            -67821,
            -16777210,
            -16777219,
            int.MinValue,
        };

        [Test, TestCaseSource(nameof(int32s))]
        public void TestPackedInt32(int data)
        {
            writer.WritePackedInt32(data);
            reader.Reset(writer.ToArraySegment());
            Assert.That(reader.ReadPackedInt32(), Is.EqualTo(data));
        }

        [Test, TestCaseSource(nameof(int32s))]
        public void TestInt32(int data)
        {
            writer.WriteInt32(data);
            reader.Reset(writer.ToArraySegment());
            Assert.That(reader.ReadInt32(), Is.EqualTo(data));
        }

        [Test, TestCaseSource(nameof(int32Fail))]
        public void TestPackedInt32Failure(long data)
        {
            Assert.Throws<OverflowException>(() =>
            {
                writer.WritePackedInt64(data);
                reader.Reset(writer.ToArraySegment());
                reader.ReadPackedInt32();
            });
        }

        private static readonly ulong[] uint64s =
        {
            0,
            234,
            2284,
            67821,
            16777210,
            16777219,
            4294967295,
            1099511627775,
            281474976710655,
            72057594037927935,
            ulong.MaxValue,
        };

        [Test, TestCaseSource(nameof(uint64s))]
        public void TestPackedUInt64(ulong data)
        {
            writer.WritePackedUInt64(data);
            reader.Reset(writer.ToArraySegment());
            Assert.That(reader.ReadPackedUInt64(), Is.EqualTo(data));
        }

        [Test, TestCaseSource(nameof(uint64s))]
        public void TestUInt64(ulong data)
        {
            writer.WriteUInt64(data);
            reader.Reset(writer.ToArraySegment());
            Assert.That(reader.ReadUInt64(), Is.EqualTo(data));
        }

        private static readonly long[] int64s =
        {
            0,
            234,
            2284,
            67821,
            16777210,
            16777219,
            4294967295,
            1099511627775,
            281474976710655,
            72057594037927935,
            long.MaxValue,
            -1,
            -234,
            -2284,
            -67821,
            -16777210,
            -16777219,
            -4294967295,
            -1099511627775,
            -281474976710655,
            -72057594037927935,
            long.MinValue,
        };

        [Test, TestCaseSource(nameof(int64s))]
        public void TestPackedInt64(long data)
        {
            writer.WritePackedInt64(data);
            reader.Reset(writer.ToArraySegment());
            Assert.That(reader.ReadPackedInt64(), Is.EqualTo(data));
        }

        [Test, TestCaseSource(nameof(int64s))]
        public void TestInt64(long data)
        {
            writer.WriteInt64(data);
            reader.Reset(writer.ToArraySegment());
            Assert.That(reader.ReadInt64(), Is.EqualTo(data));
        }

        [Test]
        public void TestGuid()
        {
            var originalGuid = new Guid("0123456789abcdef9876543210fedcba");
            writer.WriteGuid(originalGuid);

            reader.Reset(writer.ToArraySegment());
            var readGuid = reader.ReadGuid();
            Assert.That(readGuid, Is.EqualTo(originalGuid));
        }

        private static readonly float[] weirdFloats =
        {
            0f,
            -0f,
            float.Epsilon,
            -float.Epsilon,
            float.MaxValue,
            float.MinValue,
            float.NaN,
            -float.NaN,
            float.PositiveInfinity,
            float.NegativeInfinity,
            (float) double.MaxValue,
            (float) double.MinValue,
            (float) decimal.MaxValue,
            (float) decimal.MinValue,
            (float) Math.PI,
            (float) Math.E
        };

        [Test, TestCaseSource(nameof(weirdFloats))]
        public void TestFloats(float weird)
        {
            writer.WriteSingle(weird);
            reader.Reset(writer.ToArraySegment());
            var readFloat = reader.ReadSingle();
            Assert.That(readFloat, Is.EqualTo(weird));
        }

        private static readonly double[] weirdDoubles =
        {
            0d,
            -0d,
            double.Epsilon,
            -double.Epsilon,
            double.MaxValue,
            double.MinValue,
            double.NaN,
            -double.NaN,
            double.PositiveInfinity,
            double.NegativeInfinity,
            float.MaxValue,
            float.MinValue,
            (double) decimal.MaxValue,
            (double) decimal.MinValue,
            Math.PI,
            Math.E
        };

        [Test, TestCaseSource(nameof(weirdDoubles))]
        public void TestDoubles(double weird)
        {
            writer.WriteDouble(weird);
            reader.Reset(writer.ToArraySegment());
            var readDouble = reader.ReadDouble();
            Assert.That(readDouble, Is.EqualTo(weird));
        }

        private static readonly decimal[] weirdDecimals =
        {
            decimal.Zero,
            -decimal.Zero,
            decimal.MaxValue,
            decimal.MinValue,
            (decimal) Math.PI,
            (decimal) Math.E
        };

        [Test, TestCaseSource(nameof(weirdDecimals))]
        public void TestDecimals(decimal weird)
        {
            writer.WriteDecimalConverter(weird);
            reader.Reset(writer.ToArraySegment());
            var readDecimal = reader.ReadDecimalConverter();
            Assert.That(readDecimal, Is.EqualTo(weird));
        }

        [Test]
        public void TestFloatBinaryCompatibility()
        {
            float[] inputFloats = {
                ((float) Math.PI) / 3.0f,
                ((float) Math.E) / 3.0f
            };
            byte[] expected = {
                146, 10,134, 63,
                197,245,103, 63
            };
            foreach (var weird in inputFloats)
            {
                writer.WriteSingle(weird);
            }
            Assert.That(writer.ToArray(), Is.EqualTo(expected));
        }

        [Test]
        public void TestDoubleBinaryCompatibility()
        {
            double[] inputDoubles = {
                Math.PI / 3.0d,
                Math.E / 3.0d
            };
            byte[] expected = {
                101,115, 45, 56, 82,193,240, 63,
                140,116,112,185,184,254,236, 63
            };
            foreach (var weird in inputDoubles)
            {
                writer.WriteDouble(weird);
            }
            Assert.That(writer.ToArray(), Is.EqualTo(expected));
        }

        [Test]
        public void TestDecimalBinaryCompatibility()
        {
            decimal[] inputDecimals = {
                ((decimal) Math.PI) / 3.0m,
                ((decimal) Math.E) / 3.0m
            };
            byte[] expected = {
                0x00, 0x00, 0x1C, 0x00, 0x12, 0x37, 0xD6, 0x21, 0xAB, 0xEA,
                0x84, 0x0A, 0x5B, 0x5E, 0xB1, 0x03, 0x00, 0x00, 0x0E, 0x00,
                0x00, 0x00, 0x00, 0x00, 0xF0, 0x6D, 0xC2, 0xA4, 0x68, 0x52,
                0x00, 0x00
            };
            foreach (var weird in inputDecimals)
            {
                writer.WriteDecimalConverter(weird);
            }
            Assert.That(writer.ToArray(), Is.EqualTo(expected));
        }

        [Test]
        public void TestByteEndianness(
            [Values(0x12, 0x43, 0x00, 0xff, 0xab, 0x02, 0x20)] byte value)
        {
            writer.WriteByte(value);
            Assert.That(writer.ToArray(), Is.EqualTo(new[] { value }));
        }

        [Test]
        public void TestUShortEndianness()
        {
            writer.WriteUInt16(0x1234);
            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0x34, 0x12 }));
        }

        [Test]
        public void TestUIntEndianness()
        {
            writer.WriteUInt32(0x12345678);
            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0x78, 0x56, 0x34, 0x12 }));
        }

        [Test]
        public void TestULongEndianness()
        {
            writer.WriteUInt64(0x0123456789abcdef);
            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0xef, 0xcd, 0xab, 0x89, 0x67, 0x45, 0x23, 0x01 }));
        }

        [Test]
        public void TestSbyteEndianness()
        {
            byte[] values = { 0x12, 0x43, 0x00, 0xff, 0xab, 0x02, 0x20 };
            byte[] expected = { 0x12, 0x43, 0x00, 0xff, 0xab, 0x02, 0x20 };
            foreach (var value in values)
            {
                writer.WriteSByte((sbyte)value);
            }
            Assert.That(writer.ToArray(), Is.EqualTo(expected));
        }

        [Test]
        public void TestShortEndianness()
        {
            writer.WriteInt16(0x1234);
            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0x34, 0x12 }));
        }

        [Test]
        public void TestIntEndianness()
        {
            writer.WriteInt32(0x12345678);
            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0x78, 0x56, 0x34, 0x12 }));
        }

        [Test]
        public void TestLongEndianness()
        {
            writer.WriteInt64(0x0123456789abcdef);
            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0xef, 0xcd, 0xab, 0x89, 0x67, 0x45, 0x23, 0x01 }));
        }

        [Test]
        public void TestWritingAndReading()
        {
            writer.WriteChar((char)1);
            writer.WriteByte(2);
            writer.WriteSByte(3);
            writer.WriteBoolean(true);
            writer.WriteInt16(4);
            writer.WriteUInt16(5);
            writer.WriteInt32(6);
            writer.WriteUInt32(7U);
            writer.WriteInt64(8L);
            writer.WriteUInt64(9UL);
            writer.WriteSingle(10.0F);
            writer.WriteDouble(11.0D);
            writer.WriteDecimalConverter(12);
            writer.WriteString(null);
            writer.WriteString("");
            writer.WriteString("13");
            // just the byte array, no size info etc.
            writer.WriteBytes(new byte[] { 14, 15 }, 0, 2);
            // [SyncVar] struct values can have uninitialized byte arrays, null needs to be supported
            writer.WriteBytesAndSize(null);

            // buffer, no-offset, count
            writer.WriteBytesAndSize(new byte[] { 17, 18 }, 0, 2);
            // buffer, offset, count
            writer.WriteBytesAndSize(new byte[] { 19, 20, 21 }, 1, 2);
            // size, buffer
            writer.WriteBytesAndSize(new byte[] { 22, 23 }, 0, 2);

            // read them
            reader.Reset(writer.ToArraySegment());

            Assert.That(reader.ReadChar(), Is.EqualTo(1));
            Assert.That(reader.ReadByte(), Is.EqualTo(2));
            Assert.That(reader.ReadSByte(), Is.EqualTo(3));
            Assert.That(reader.ReadBoolean(), Is.True);
            Assert.That(reader.ReadInt16(), Is.EqualTo(4));
            Assert.That(reader.ReadUInt16(), Is.EqualTo(5));
            Assert.That(reader.ReadInt32(), Is.EqualTo(6));
            Assert.That(reader.ReadUInt32(), Is.EqualTo(7));
            Assert.That(reader.ReadInt64(), Is.EqualTo(8));
            Assert.That(reader.ReadUInt64(), Is.EqualTo(9));
            Assert.That(reader.ReadSingle(), Is.EqualTo(10));
            Assert.That(reader.ReadDouble(), Is.EqualTo(11));
            Assert.That(reader.ReadDecimalConverter(), Is.EqualTo(12));
            // writing null string should write null in Mirage ("" in original HLAPI)
            Assert.That(reader.ReadString(), Is.Null);
            Assert.That(reader.ReadString(), Is.EqualTo(""));
            Assert.That(reader.ReadString(), Is.EqualTo("13"));

            Assert.That(reader.ReadBytes(2), Is.EqualTo(new byte[] { 14, 15 }));

            Assert.That(reader.ReadBytesAndSize(), Is.Null);

            Assert.That(reader.ReadBytesAndSize(), Is.EqualTo(new byte[] { 17, 18 }));

            Assert.That(reader.ReadBytesAndSize(), Is.EqualTo(new byte[] { 20, 21 }));

            Assert.That(reader.ReadBytesAndSize(), Is.EqualTo(new byte[] { 22, 23 }));
        }

        [Test]
        public void TestList()
        {
            var original = new List<int> { 1, 2, 3, 4, 5 };
            writer.Write(original);

            reader.Reset(writer.ToArraySegment());
            var readList = reader.Read<List<int>>();
            Assert.That(readList, Is.EqualTo(original));
        }

        [Test]
        public void TestNullList()
        {
            writer.Write<List<int>>(null);

            reader.Reset(writer.ToArraySegment());
            var readList = reader.Read<List<int>>();
            Assert.That(readList, Is.Null);
        }

        [Test]
        public void WriteNetworkBehaviorNull()
        {
            writer.WriteNetworkBehaviour(null);

            reader.Reset(writer.ToArraySegment());
            var behaviour = reader.ReadNetworkBehaviour();

            Assert.That(behaviour, Is.Null);

            Assert.That(writer.ByteLength, Is.EqualTo(reader.BytePosition));
        }

        [Test]
        public void WriteNetworkBehaviorNotNull()
        {
            var mock = CreateBehaviour<MockRpcComponent>();
            // init lazy props
            _ = mock.Identity.NetworkBehaviours;
            mock.Identity.NetId = 1;
            // returns found id
            reader.ObjectLocator.TryGetIdentity(1, out var _).Returns(x => { x[1] = mock.Identity; return true; });

            writer.WriteNetworkBehaviour(mock);

            reader.Reset(writer.ToArraySegment());
            var behaviour = reader.ReadNetworkBehaviour<MockRpcComponent>();

            Assert.That(behaviour == mock);
            Assert.That(writer.ByteLength, Is.EqualTo(reader.BytePosition));
        }

        // make weaver generate writers for MockComponent[]
        [NetworkMessage]
        private struct _BehaviourArrayWriter
        {
            public MockRpcComponent[] mockComponents;
        }
        [Test]
        public void WriteNetworkBehaviorArray()
        {
            var mock = CreateBehaviour<MockRpcComponent>();
            // init lazy props
            _ = mock.Identity.NetworkBehaviours;
            mock.Identity.NetId = 1;
            // returns found id
            reader.ObjectLocator.TryGetIdentity(1, out var _).Returns(x => { x[1] = mock.Identity; return true; });

            var mockArray = new MockRpcComponent[] { mock };
            writer.Write(mockArray);

            reader.Reset(writer.ToArraySegment());
            var readArray = reader.Read<MockRpcComponent[]>();

            Assert.That(mockArray.Length == mockArray.Length);
            Assert.That(mockArray[0] == mockArray[0]);
            Assert.That(writer.ByteLength, Is.EqualTo(reader.BytePosition));
        }

        [Test]
        public void WriteNetworkBehaviorDestroyed()
        {
            // setup
            var mock = CreateBehaviour<MockRpcComponent>();
            // init lazy props
            _ = mock.Identity.NetworkBehaviours;
            mock.Identity.NetId = 1;
            // return not found
            reader.ObjectLocator.TryGetIdentity(1, out var _).Returns(x => { x[1] = null; return false; });

            writer.WriteNetworkBehaviour(mock);

            reader.Reset(writer.ToArraySegment());
            var behaviour = reader.ReadNetworkBehaviour<MockRpcComponent>();

            Assert.That(behaviour, Is.Null);
            // make sure read same as written (including compIndex for non-0 netid
            Assert.That(writer.ByteLength, Is.EqualTo(reader.BytePosition));
        }

        [Test]
        public void TestWriteGameObject()
        {
            writer.WriteGameObject(null);

            reader.Reset(writer.ToArraySegment());
            var obj = reader.ReadGameObject();

            Assert.That(obj, Is.Null);

            Assert.That(writer.ByteLength, Is.EqualTo(reader.BytePosition));
        }

        // use networkmessage to make sure writer is generated
        [NetworkMessage]
        public struct NullableIntMessage
        {
            public int? value1;
            public bool? value2;
            public ulong? value3;
        }

        [Test]
        [TestCase(null)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(1234)]
        public void NullableInt(int? value)
        {
            writer.Write(value);
            reader.Reset(writer.ToArraySegment());
            var unpacked = reader.Read<int?>();

            Assert.That(unpacked, Is.EqualTo(value));
        }

        [Test]
        [TestCase(null)]
        [TestCase(true)]
        [TestCase(false)]
        public void NullableBool(bool? value)
        {
            writer.Write(value);
            reader.Reset(writer.ToArraySegment());
            var unpacked = reader.Read<bool?>();

            Assert.That(unpacked, Is.EqualTo(value));
        }

        [Test]
        [TestCase(null)]
        [TestCase(0ul)]
        [TestCase(20202020ul)]
        public void NullableUlong(ulong? value)
        {
            writer.Write(value);
            reader.Reset(writer.ToArraySegment());
            var unpacked = reader.Read<ulong?>();

            Assert.That(unpacked, Is.EqualTo(value));
        }

        [Test]
        public void SByteLength()
        {
            writer.WriteSByte(14);
            Assert.That(writer.BitPosition, Is.EqualTo(8));
        }
        [Test]
        public void Int16Length()
        {
            writer.WriteInt16(50);
            Assert.That(writer.BitPosition, Is.EqualTo(16));
        }
        [Test]
        public void Int32Length()
        {
            writer.WriteInt32(20);
            Assert.That(writer.BitPosition, Is.EqualTo(32));
        }
        [Test]
        public void Int64Length()
        {
            writer.WriteInt64(21);
            Assert.That(writer.BitPosition, Is.EqualTo(64));
        }


        [Test]
        public void ByteLength()
        {
            writer.WriteByte(20);
            Assert.That(writer.BitPosition, Is.EqualTo(8));
        }
        [Test]
        public void UInt16Length()
        {
            writer.WriteUInt16(263);
            Assert.That(writer.BitPosition, Is.EqualTo(16));
        }
        [Test]
        public void UInt32Length()
        {
            writer.WriteUInt32(65);
            Assert.That(writer.BitPosition, Is.EqualTo(32));
        }
        [Test]
        public void UInt64Length()
        {
            writer.WriteUInt64(22);
            Assert.That(writer.BitPosition, Is.EqualTo(64));
        }

        [Test]
        public void NetworkWriterOtherAsmDefTest()
        {
            // reset flags
            MessageWithCustomWriterExtesions.WriterCalled = 0;
            MessageWithCustomWriterExtesions.ReaderCalled = 0;

            var inValue = new MessageWithCustomWriter()
            {
                type = 3,
                value = 1.4f
            };

            // use generic writer
            writer.Write(inValue);
            Assert.That(writer.BitPosition, Is.EqualTo(MessageWithCustomWriterExtesions.WriteSize));
            reader.Reset(writer.ToArraySegment());

            var outValue = reader.Read<MessageWithCustomWriter>();
            Assert.That(reader.BitPosition, Is.EqualTo(MessageWithCustomWriterExtesions.WriteSize));
            Assert.That(outValue.type, Is.EqualTo(inValue.type));
            Assert.That(outValue.value, Is.EqualTo(inValue.value).Within(0.01f));

            // methods should be called once
            Assert.That(MessageWithCustomWriterExtesions.WriterCalled, Is.EqualTo(1));
            Assert.That(MessageWithCustomWriterExtesions.ReaderCalled, Is.EqualTo(1));
        }

        [Test]
        [TestCase(null)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(10)]
        public void WritePlusOne(int? inCount)
        {
            CollectionExtensions.WriteCountPlusOne(writer, inCount);
            reader.Reset(writer.ToArraySegment());
            var hasValue = CollectionExtensions.ReadCountPlusOne(reader, out var outCount);

            Assert.That(hasValue, Is.EqualTo(inCount.HasValue));
            if (hasValue)
            {
                Assert.That(outCount, Is.EqualTo(inCount.Value));
            }
        }

        [Test]
        [Description("valids size from reader with 80 bits")]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(50)]
        [TestCase(79)]
        [TestCase(80)]
        public void ValidateSizeNoThrow(int count)
        {
            // 10 bytes = 80 bits
            reader.Reset(new byte[10]);
            Assert.DoesNotThrow(() =>
            {
                CollectionExtensions.ValidateSize(reader, count);
            });
        }

        [Test]
        [Description("valids size from reader with 80 bits")]
        [TestCase(81)]
        [TestCase(100)]
        [TestCase(50000)]
        [TestCase(int.MaxValue)]
        public void ValidateSizeThrows(int count)
        {
            // 10 bytes = 80 bits
            reader.Reset(new byte[10]);
            var e = Assert.Throws<EndOfStreamException>(() =>
            {
                CollectionExtensions.ValidateSize(reader, count);
            });
            Assert.That(e.Message, Is.EqualTo($"Can't read {count} elements because it would read past the end of the stream."));
        }


        [Test]
        public void DoesNotWriteProtectedField()
        {
            var inValue = new _ClassWithProtected()
            {
                Field1 = 10,
                Field2 = 20,
                Field3 = 30,
                Field4 = 40,
            };

            Assert.DoesNotThrow(() =>
            {
                MessagePacker.Pack(new _MessageWithProtected { Field = inValue }, writer);
            });
            writer.Reset();

            writer.Write(new _MessageWithProtected { Field = inValue });
            // use generic writer
            //writer.Write();
            reader.Reset(writer.ToArraySegment());
            var outValue = reader.Read<_MessageWithProtected>().Field;

            Assert.That(outValue, Is.Not.Null);
            Assert.That(outValue.Field1, Is.EqualTo(10));
            Assert.That(outValue.Field2, Is.EqualTo(default(int)));
            Assert.That(outValue.Field3, Is.EqualTo(default(int)));
            Assert.That(outValue.Field4, Is.EqualTo(default(int)));
        }
    }

    [NetworkMessage]
    public class _ClassWithProtected
    {
        // should serialize
        public int Field1;

        // should NOT serialize
        protected int _field2;
        private int _field3;
        internal int Field4;

        // accessors for test
        public int Field2 { get => _field2; set => _field2 = value; }
        public int Field3 { get => _field3; set => _field3 = value; }
    }

    public struct _MessageWithProtected
    {
        public _ClassWithProtected Field;
    }
}
