using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Zeroconf;

namespace ZeroconfTests
{
    [TestClass]
    public class DnsMathTests
    {
        [TestMethod]
        public void AuthoritativeAnswerIsAResponse()
        {
            Assert.IsTrue(DnsMath.IsResponse(0x8400));
        }

        [TestMethod]
        public void QuestionIsNotAResponse()
        {
            Assert.IsFalse(DnsMath.IsResponse(0));
        }

        [TestMethod]
        public void B11000000IsAPointer()
        {
            Assert.IsTrue(DnsMath.IsPointer(0xC0));
        }

        [TestMethod]
        public void B11000001IsAPointer()
        {
            Assert.IsTrue(DnsMath.IsPointer(0xC1));
        }

        [TestMethod]
        public void B00000001IsNotAPointer()
        {
            Assert.IsFalse(DnsMath.IsPointer(12));
        }

        [TestMethod]
        public void SmallPointerIsCorrect()
        {
            Assert.IsTrue(DnsMath.TwoBytesToPointer(0xC0, 12) == 12);
        }

        [TestMethod]
        public void BigPointerIsCorrect()
        {
            Assert.IsTrue(DnsMath.TwoBytesToPointer(0xC1, 1) == 257);
        }
    }
}
