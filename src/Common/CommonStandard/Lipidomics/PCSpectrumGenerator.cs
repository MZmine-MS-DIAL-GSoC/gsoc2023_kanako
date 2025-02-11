﻿using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class PCSpectrumGenerator : ILipidSpectrumGenerator
    {
        private static readonly double C5H14NO4P = new[] {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 14,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double C3H9N = new[] {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 9,
            MassDiffDictionary.NitrogenMass,
        }.Sum();

        private static readonly double Gly_C = new[] {
            MassDiffDictionary.CarbonMass * 8,
            MassDiffDictionary.HydrogenMass * 18,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double Gly_O = new[] {
            MassDiffDictionary.CarbonMass * 7,
            MassDiffDictionary.HydrogenMass * 16,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 5,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        private static readonly double CH2 = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        public PCSpectrumGenerator() {
            spectrumGenerator = new SpectrumPeakGenerator();
        }

        public PCSpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator) {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.PC)
            {
                if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M+Na]+")
                {
                    return true;
                }
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null) {
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetPCSpectrum(lipid, adduct));
            if (lipid.Chains is MolecularSpeciesLevelChains mlChains) {
                spectrum.AddRange(GetAcylLevelSpectrum(lipid, mlChains.Chains, adduct));
                spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, mlChains.Chains.OfType<AcylChain>().Where(c => c.DoubleBond.UnDecidedCount == 0 && c.Oxidized.UnDecidedCount == 0), adduct));
            }
            if (lipid.Chains is PositionLevelChains plChains) {
                spectrum.AddRange(GetAcylLevelSpectrum(lipid, plChains.Chains, adduct));
                spectrum.AddRange(GetAcylPositionSpectrum(lipid, plChains.Chains[0], adduct));
                spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, plChains.Chains.OfType<AcylChain>().Where(c => c.DoubleBond.UnDecidedCount == 0 && c.Oxidized.UnDecidedCount == 0), adduct));
            }
            spectrum = spectrum.GroupBy(spec => spec, comparer)
                .Select(specs => new SpectrumPeak(specs.First().Mass, specs.Sum(n => n.Intensity), string.Join(", ", specs.Select(spec => spec.Comment)), specs.Aggregate(SpectrumComment.none, (a, b) => a | b.SpectrumComment)))
                .OrderBy(peak => peak.Mass)
                .ToList();
            return CreateReference(lipid, adduct, spectrum, molecule);
        }

        private MoleculeMsReference CreateReference(ILipid lipid, AdductIon adduct, List<SpectrumPeak> spectrum, IMoleculeProperty molecule) {
            return new MoleculeMsReference
            {
                PrecursorMz = adduct.ConvertToMz(lipid.Mass),
                IonMode = adduct.IonMode,
                Spectrum = spectrum,
                Name = lipid.Name,
                Formula = molecule?.Formula,
                Ontology = molecule?.Ontology,
                SMILES = molecule?.SMILES,
                InChIKey = molecule?.InChIKey,
                AdductType = adduct,
                CompoundClass = lipid.LipidClass.ToString(),
                Charge = adduct.ChargeNumber,
            };
        }

        private SpectrumPeak[] GetPCSpectrum(ILipid lipid, AdductIon adduct) {
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak(adduct.ConvertToMz(C5H14NO4P), 300d, "Header") { SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                new SpectrumPeak(adduct.ConvertToMz(Gly_C), 150d, "Gly-C")  { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak(adduct.ConvertToMz(Gly_O), 100d, "Gly-O") { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C5H14NO4P - MassDiffDictionary.HydrogenMass*2), 25d, "Precursor-Header") { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass)/2, 150d, "[Precursor]2+") { SpectrumComment = SpectrumComment.metaboliteclass },
            };
            if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                         new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C5H14NO4P), 300d, "Precursor-Header") { SpectrumComment = SpectrumComment.metaboliteclass },
                         new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C3H9N), 200d, "Precursor-C3H9N") { SpectrumComment = SpectrumComment.metaboliteclass },
                     }
                );
            }
            return spectrum.ToArray();
        }

        private IEnumerable<SpectrumPeak> GetAcylLevelSpectrum(ILipid lipid, IEnumerable<IChain> acylChains, AdductIon adduct) {
            return acylChains.SelectMany(acylChain => GetAcylLevelSpectrum(lipid, acylChain, adduct));
        }

        private SpectrumPeak[] GetAcylLevelSpectrum(ILipid lipid, IChain acylChain, AdductIon adduct) {
            var lipidMass = lipid.Mass;
            var chainMass = acylChain.Mass - MassDiffDictionary.HydrogenMass;
            return new[]
            {
                new SpectrumPeak(adduct.ConvertToMz(lipidMass - chainMass), 50d, $"-{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                new SpectrumPeak(adduct.ConvertToMz(lipidMass - chainMass - H2O), 50d, $"-{acylChain}-O") { SpectrumComment = SpectrumComment.acylchain },
            };
        }

        private SpectrumPeak[] GetAcylPositionSpectrum(ILipid lipid, IChain acylChain, AdductIon adduct) {
            var lipidMass = lipid.Mass;
            var chainMass = acylChain.Mass;
            return new[]
            {
                new SpectrumPeak(adduct.ConvertToMz(lipidMass - chainMass - MassDiffDictionary.OxygenMass - CH2), 50d, "-CH2(Sn1)") { SpectrumComment = SpectrumComment.snposition },
            };
        }

        private IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, IEnumerable<AcylChain> acylChains, AdductIon adduct) {
            return acylChains.SelectMany(acylChain => spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acylChain, adduct, 0d, 10d));
        }

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();
    }
}
