using CompMs.Common.Components;
using CompMs.Common.DataObj.Ion;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.Common.FormulaGenerator.Function.Tests
{
    [TestClass()]
    public class ChemicalOntologyAnnotationTests
    {
        private readonly List<ProductIon> productIonDB = FragmentDbParser.GetProductIonDB(@"Resources\FormulaGenerator\ProductIonLib_vs1.pid", out _);
        private readonly List<NeutralLoss> neutralLossDB = FragmentDbParser.GetNeutralLossDB(@"Resources\FormulaGenerator\NeutralLossDB_vs2.ndb", out _);
        private readonly List<ChemicalOntology> chemicalOntDB = ChemOntologyDbParser.Read(@"Resources\FormulaGenerator\ChemOntologyDB_vs2.cho", out _);
        private readonly AnalysisParamOfMsfinder param = new();
        private List<FormulaResult> results = FormulaResultParcer.FormulaResultReader(@"Resources\FormulaGenerator\Function\formula_results_without_chemont.fgt", out _);

        [TestMethod()]
        public void ProcessByOverRepresentationAnalysisTest()
        {
            Logger.LogMessage(chemicalOntDB.Count.ToString());

            var adduct = AdductIon.GetAdductIon("[M+H]+");

            var formulaResults = FormulaResultParcer.FormulaResultReader(@"Resources\FormulaGenerator\Function\formula_results_without_chemont.fgt", out string error);

            Logger.LogMessage(error);
            Assert.AreEqual(formulaResults[0].Formula.FormulaString, "C33H40N2O9");
            Assert.AreEqual(formulaResults[0].ChemicalOntologyDescriptions.Count, 0);
            Assert.AreEqual(formulaResults[0].ChemicalOntologyIDs.Count, 0);
            Assert.AreEqual(formulaResults[0].ChemicalOntologyScores.Count, 0);
            Assert.AreEqual(formulaResults[0].ChemicalOntologyRepresentativeInChIKeys.Count, 0);

            ChemicalOntologyAnnotation.ProcessByOverRepresentationAnalysis(formulaResults, chemicalOntDB, IonMode.Positive,
                param, adduct, productIonDB, neutralLossDB);

            Logger.LogMessage(string.Join(",\n", formulaResults[0].ChemicalOntologyDescriptions));
            Console.WriteLine(formulaResults[0].Formula.FormulaString);
            Console.WriteLine(formulaResults[0].ChemicalOntologyDescriptions.Count);
            CollectionAssert.AreEquivalent(formulaResults[0].ChemicalOntologyDescriptions, new List<string> { "Alkaloids and derivatives", "Carbolines", "Coumarin and derivatives", "Indole and derivatives", "Benzoic acids and derivatives", "Benzophenones", "Morphinans", "N-acyl-amino acids", "Other flavonoids", "Phenylacetamides", "Pyridinecarboxylic acids", "Pyridines and derivatives", "Phenylpiperidines", "Quinazolines", "Amines", "Aminoalcohols", "Aminobenzoic acids", "Purines", "Alkylphenylketones", "Amino acids and derivatives", "Anisoles", "Aurone O-glycosides", "Retrochalcones", "Saccharides", "Steroids" });
        }
                
    }
}