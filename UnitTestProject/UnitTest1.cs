using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var str = "asdasdasdasdasdas";
            var num = 32;
            var ls = new List<byte>();
            ls.AddRange(BitConverter.GetBytes(32));
            ls.AddRange(Encoding.UTF8.GetBytes(str));

            using (var ms = new MemoryStream(ls.ToArray()))
            using (var binaryreader = new BinaryReader(ms, Encoding.UTF8))
            {
                var numread = binaryreader.ReadInt32();
                Assert.AreEqual(32, numread);



                var strread = binaryreader.ReadString();
                Assert.AreEqual(strread, "asdasdasdasdasdas");
            }
        }
        [TestMethod]
        public void TestMethod11()
        {
            var str = "按劳动法的法律和的话法轮大法好啦";
            var num = 32;

            byte[] ls = null;
            using (var ms = new MemoryStream())
            using (var binarywriter = new BinaryWriter(ms, Encoding.UTF8))
            {
                binarywriter.Write(str);
                binarywriter.Write(num);
                ls = ms.ToArray();
            }

            using (var ms = new MemoryStream(ls))
            using (var binaryreader = new BinaryReader(ms, Encoding.UTF8))
            {



                var strread = binaryreader.ReadString();
                Assert.AreEqual(strread, str);
                var numread = binaryreader.ReadInt32();
                Assert.AreEqual(32, numread);
            }
        }

        [TestMethod]
        public void TestMethod2()
        {
            var str = "asdasdasdasdasdas";
            var ls = new List<byte>();
            ls.Add(17);
            ls.AddRange(Encoding.UTF8.GetBytes(str));

            using (var ms = new MemoryStream(ls.ToArray()))
            using (var binaryreader = new BinaryReader(ms, Encoding.UTF8))
            {

                var strread = binaryreader.ReadString();
                Assert.AreEqual(strread, "asdasdasdasdasdas");
            }
        }

        [TestMethod]
        public void TestMethod3()
        {
            double max = long.MaxValue;
            var m=max / 1024 / 1024 / 1024;

        }
    }
}
