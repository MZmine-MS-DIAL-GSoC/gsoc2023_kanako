﻿using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace CompMs.Common.MessagePack.Tests
{
    [TestClass()]
    public class LargeListMessagePackTests
    {
        [TestMethod()]
        [DataRow(100)]
        [DataRow(13421775)] // 1073741823 / 80 = 13421772.8
        [DataRow(20000000)]
        // [DataRow(1073741823)]
        // [DataRow(1073741825)]
        public void SaveAndLoadLargeSampleTest(int size) {
            var datas = new LargeSample[size];
            var memory = new MemoryStream();
            LargeListMessagePack.Serialize(memory, datas);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = LargeListMessagePack.Deserialize<LargeSample>(memory);
            Assert.AreEqual(datas.Length, actual.Count);
        }

        [TestMethod()]
        [DataRow(100)]
        [DataRow(13421775)] // 1073741823 / 80 = 13421772.8
        [DataRow(20000000)]
        // [DataRow(1073741823)]
        // [DataRow(1073741825)]
        public void SaveAndLoadSmallSampleTest(int size) {
            var datas = new SmallSample[size];
            var memory = new MemoryStream();
            LargeListMessagePack.Serialize(memory, datas);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = LargeListMessagePack.Deserialize<SmallSample>(memory);
            Assert.AreEqual(datas.Length, actual.Count);
        }

        [TestMethod()]
        [DataRow(100, 100)]
        [DataRow(1000000, 100)]
        [DataRow(10000000, 100)]
        [DataRow(100, 10500000)]
        // [DataRow(10737420, 100)] // 1073741823 / 100 = 10737418.23
        // [DataRow(20000000, 100)]
        public void SaveAndLoadRandomSampleTest(int size, int dataSize) {
            var datas = new RandomSample[size];
            for (int i = 0; i < datas.Length; i++) {
                datas[i] = new RandomSample(dataSize);
            }
            var memory = new MemoryStream();
            LargeListMessagePack.Serialize(memory, datas);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = LargeListMessagePack.Deserialize<RandomSample>(memory);
            Assert.AreEqual(datas.Length, actual.Count);
        }

        [TestMethod()]
        public void SaveAndLoadLargeAndLessSampleTest() {
            var datas = new FixedSample[]
            {
                new FixedSample(500000000L, 500000000L),
                new FixedSample(500000000L, 630000000L),
            };
            var memory = new MemoryStream();
            LargeListMessagePack.Serialize(memory, datas);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = LargeListMessagePack.Deserialize<FixedSample>(memory);
            Assert.AreEqual(datas.Length, actual.Count);
        }

        [MessagePackObject]
        public class SmallSample {

        }

        [MessagePackObject]
        public class LargeSample {
            // 8 bytes x 10 + header = 80 bytes + header
            [Key(0)]
            public long[] Xs { get; set; } = new long[10];
            // [Key(0)]
            // public long X0 { get; set; }
            // [Key(1)]
            // public long X1 { get; set; }
            // [Key(2)]
            // public long X2 { get; set; }
            // [Key(3)]
            // public long X3 { get; set; }
            // [Key(4)]
            // public long X4 { get; set; }
            // [Key(5)]
            // public long X5 { get; set; }
            // [Key(6)]
            // public long X6 { get; set; }
            // [Key(7)]
            // public long X7 { get; set; }
            // [Key(8)]
            // public long X8 { get; set; }
            // [Key(9)]
            // public long X9 { get; set; }
        }

        [MessagePackObject]
        public class RandomSample {
            private readonly static Random random = new Random();
            public RandomSample(int size) {
                Xs = new byte[size];
                random.NextBytes(Xs);               
            }

            public RandomSample() {

            }

            [Key(0)]
            public byte[] Xs { get; set; }
        }

        [MessagePackObject]
        public class FixedSample {
            public FixedSample(long size1, long size2) {
                Xs = new byte[size1];
                Ys = new byte[size2];
            }

            public FixedSample() {

            }

            [Key(0)]
            public byte[] Xs { get; set; }

            [Key(1)]
            public byte[] Ys { get; set; }
        }
    }
}