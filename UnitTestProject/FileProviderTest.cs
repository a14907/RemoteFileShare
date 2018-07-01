using Microsoft.Extensions.FileProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject
{
    [TestClass]
    public class FileProviderTest
    {
        [TestMethod]
        public void Test()
        {
            PhysicalFileProvider physicalFileProvider = new PhysicalFileProvider("C:\\Users\\a14907_admin\\Downloads\\bt");

            var rootdir = physicalFileProvider.GetDirectoryContents("/");
            foreach (var item in rootdir)
            {

            }

        }
    }
}
