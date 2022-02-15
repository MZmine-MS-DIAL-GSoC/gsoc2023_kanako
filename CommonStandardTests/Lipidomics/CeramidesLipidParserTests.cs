﻿using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class CeramidesLipidParserTests
    {
        [TestMethod()]
        public void SMParseTest()
        {
            var parser = new SMLipidParser();

            var lipid = parser.Parse("SM 36:2;2O"); 
            Assert.AreEqual(728.58323, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.SM, lipid.LipidClass);

            lipid = parser.Parse("SM 18:1;2O/18:1"); // CCCCCCCCCCCCC\C=C\C(O)C(COP([O-])(=O)OCC[N+](C)(C)C)NC(=O)CCCCCCC\C=C/CCCCCCCC
            Assert.AreEqual(728.58323, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.SM, lipid.LipidClass);

            lipid = parser.Parse("SM 18:1;2O/18:1;O");
            Assert.AreEqual(744.57814, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.SM, lipid.LipidClass);

            lipid = parser.Parse("SM 36:1;5O"); 
            Assert.AreEqual(778.58362, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.SM, lipid.LipidClass);

            lipid = parser.Parse("SM 18:1;3O/18:0;2O"); 
            Assert.AreEqual(778.58362, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.SM, lipid.LipidClass);
        }
        [TestMethod()]
        public void CerNSParseTest()
        {
            var parser = new CeramideLipidParser();

            var lipid = parser.Parse("Cer 42:2;2O"); 
            Assert.AreEqual(647.6216455, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Undefined, lipid.LipidClass);

            lipid = parser.Parse("Cer 18:2;2O/24:0"); // O=C(NC(CO)C(O)C=CCCC=CCCCCCCCCC)CCCCCCCCCCCCCCCCCCCCCCC
            Assert.AreEqual(647.6216455, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_NS, lipid.LipidClass);
        }
        [TestMethod()]
        public void CerNDSParseTest()
        {
            var parser = new CeramideLipidParser();
            var lipid = parser.Parse("Cer 18:0;2O/24:0"); // O=C(NC(CO)C(O)CCCCCCCCCCCCCCC)CCCCCCCCCCCCCCCCCCCCCCC
            Assert.AreEqual(651.6529456, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_NDS, lipid.LipidClass);
        }
        [TestMethod()]
        public void CerNPParseTest()
        {
            var parser = new CeramideLipidParser();
            var lipid = parser.Parse("Cer 18:1;3O/24:0"); // O=C(NC(CO)C(O)C(O)CCCC=CCCCCCCCCC)CCCCCCCCCCCCCCCCCCCCCCC
            Assert.AreEqual(665.6322102, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_NP, lipid.LipidClass);

            lipid = parser.Parse("Cer 18:0;3O/24:0"); // O=C(NC(CO)C(O)C(O)CCCCCCCCCCCCCC)CCCCCCCCCCCCCCCCCCCCCCC
            Assert.AreEqual(667.6478602, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_NP, lipid.LipidClass);
        }
        [TestMethod()]
        public void CerASParseTest()
        {
            var parser = new CeramideLipidParser();
            var lipid = parser.Parse("Cer 18:1;2O/24:0;(2OH)"); // O=C(NC(CO)C(O)C=CCCCCCCCCCCCCC)C(O)CCCCCCCCCCCCCCCCCCCCCC
            Assert.AreEqual(665.6322102, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_AS, lipid.LipidClass);
        }
        [TestMethod()]
        public void CerADSParseTest()
        {
            var parser = new CeramideLipidParser();
            var lipid = parser.Parse("Cer 18:0;2O/24:0;(2OH)"); //  O=C(NC(CO)C(O)CCCCCCCCCCCCCCC)C(O)CCCCCCCCCCCCCCCCCCCCCC
            Assert.AreEqual(667.6478602, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_ADS, lipid.LipidClass);
        }
        [TestMethod()]
        public void CerAPParseTest()
        {
            var parser = new CeramideLipidParser();
            var lipid = parser.Parse("Cer 18:2;3O/24:0;(2OH)"); // O=C(NC(CO)C(O)C(O)CCCC=CCCC=CCCCCC)C(O)CCCCCCCCCCCCCCCCCCCCCC
            Assert.AreEqual(679.611474712, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_AP, lipid.LipidClass);

            lipid = parser.Parse("Cer 18:0;3O/24:0;(2OH)"); // O=C(NC(CO)C(O)C(O)CCCCCCCCCCCCCC)C(O)CCCCCCCCCCCCCCCCCCCCCC
            Assert.AreEqual(683.6427748, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_AP, lipid.LipidClass);
        }
        [TestMethod()]
        public void CerBSParseTest()
        {
            var parser = new CeramideLipidParser();
            var lipid = parser.Parse("Cer 18:1;2O/24:0;(3OH)"); // 
            Assert.AreEqual(665.6322102, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_BS, lipid.LipidClass);
        }
        [TestMethod()]
        public void CerBDSParseTest()
        {
            var parser = new CeramideLipidParser();
            var lipid = parser.Parse("Cer 18:0;2O/24:0;(3OH)"); //  
            Assert.AreEqual(667.6478602, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_BDS, lipid.LipidClass);
        }
        [TestMethod()]
        public void CerHSParseTest()
        {
            var parser = new CeramideLipidParser();
            var lipid = parser.Parse("Cer 18:1;2O/24:0;O"); // 
            Assert.AreEqual(665.6322102, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_HS, lipid.LipidClass);
        }
        [TestMethod()]
        public void CerHDSParseTest()
        {
            var parser = new CeramideLipidParser();
            var lipid = parser.Parse("Cer 18:0;2O/24:0;O"); //  
            Assert.AreEqual(667.6478602, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.Cer_HDS, lipid.LipidClass);
        }

    }
}