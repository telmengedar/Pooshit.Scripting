
using System.Collections.Generic;
using NightlyCode.Scripting;
using NightlyCode.Scripting.Parser;
using NUnit.Framework;

namespace Scripting.Tests {

    [TestFixture, Parallelizable]
    public class TokenTests {
        readonly ScriptParser parser=new ScriptParser();

        [TestCase("0x9Ab", (byte)0x9A)]
        [TestCase("0x6Csb", (sbyte)0x6C)]
        [TestCase("0xA779us", (ushort)0xA779)]
        [TestCase("0x7C66s", (short)0x7C66)]
        [TestCase("0xBEEFCACEu", 0xBEEFCACEu)]
        [TestCase("0x0F330800", 0x0F330800)]
        [TestCase("0xEFFEBAFFDEADBEEFul", 0xEFFEBAFFDEADBEEFul)]
        [TestCase("0x3A006D8A184C9CD0l", 0x3A006D8A184C9CD0L)]
        [Parallelizable]
        public void HexNumber(string data, object expected) {
            Assert.AreEqual(expected, parser.Parse(data).Execute());
        }


        [TestCase("0o377b", (byte)0xFF)]
        [TestCase("-0o23sb", (sbyte)-19)]
        [TestCase("0o63574s", (short)26492)]
        [TestCase("0o146101us", (ushort)52289)]
        [TestCase("0o32044555731u", 3499285465u)]
        [TestCase("0o10", 8)]
        [TestCase("0o604506157232L", 52161994394L)]
        [TestCase("0o177313372543203UL", 8754685462147UL)]
        [Parallelizable]
        public void OctNumber(string data, object expected) {
            Assert.AreEqual(expected, parser.Parse(data).Execute());
        }

        [TestCase("0b11010100b", (byte)212)]
        [TestCase("0b1001110sb", (sbyte)78)]
        [TestCase("0b11001011000001s", (short)12993)]
        [TestCase("0b1100000001000100us", (ushort)49220)]
        [TestCase("0b1110110001010000011111111101101", 1982349293)]
        [TestCase("0b11101010100010010001101101101010u", 3934853994u)]
        [TestCase("0b1001000011110110101100000100000011000100011101L", 39847298347293L)]
        [TestCase("0b10010010010011001101100110111010101000101110010UL", 80429384028530UL)]
        [Parallelizable]
        public void BinaryNumber(string data, object expected) {
            Assert.AreEqual(expected, parser.Parse(data).Execute());
        }

        [TestCase("199b", (byte)199)]
        [TestCase("83sb", (sbyte)83)]
        [TestCase("8229s", (short)8229)]
        [TestCase("56112us", (ushort)56112)]
        [TestCase("129394", 129394)]
        [TestCase("2342359993u", 2342359993u)]
        [TestCase("23299923849233L", 23299923849233L)]
        [TestCase("523923249994343UL", 523923249994343UL)]
        [Parallelizable]
        public void DecimalNumber(string data, object expected) {
            Assert.AreEqual(expected, parser.Parse(data).Execute());
        }

        [Test, Parallelizable]
        public void Double() {
            Assert.AreEqual(8232.221, parser.Parse("8232.221").Execute());
        }

        [Test, Parallelizable]
        public void Float() {
            Assert.AreEqual(23299.943f, parser.Parse("23299.943f").Execute());
        }

        [Test, Parallelizable]
        public void Decimal() {
            Assert.AreEqual(23478.20002m, parser.Parse("23478.20002d").Execute());
        }

        [Test, Parallelizable]
        public void New() {
            Assert.That(parser.Parse("new list()").Execute() is List<object>);
        }
    }
}