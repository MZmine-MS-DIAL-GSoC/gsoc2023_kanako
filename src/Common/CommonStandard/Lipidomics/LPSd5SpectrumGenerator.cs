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
    public class LPSd5SpectrumGenerator : ILipidSpectrumGenerator
    {

        private static readonly double C3H8NO6P = new[]
        {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 8,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        private static readonly double CHO2 = new[]
        {
            MassDiffDictionary.CarbonMass * 1,
            MassDiffDictionary.HydrogenMass * 1,
            MassDiffDictionary.OxygenMass *2,
        }.Sum();

        private static readonly double C3H5NO2 = new[] {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 5,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 2,
        }.Sum();

        private static readonly double C3H4D5O6P = new[] { // 172.013675 OCC(O)COP(O)(O)=O
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 4,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.PhosphorusMass,
            MassDiffDictionary.Hydrogen2Mass * 5,
        }.Sum();

        private static readonly double Gly_C = new[] {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 7,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.PhosphorusMass,
            MassDiffDictionary.Hydrogen2Mass * 5,
        }.Sum();

        private static readonly double Gly_O = new[] {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 7,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 7,
            MassDiffDictionary.PhosphorusMass,
            MassDiffDictionary.Hydrogen2Mass * 3,
        }.Sum();

        private static readonly double CD2 = new[]
        {
            MassDiffDictionary.Hydrogen2Mass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        public LPSd5SpectrumGenerator()
        {
            spectrumGenerator = new SpectrumPeakGenerator();
        }

        public LPSd5SpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.LPS_d5)
            {
                if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M+Na]+")
                {
                    return true;
                }
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null)
        {
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetLPSSpectrum(lipid, adduct));
            if (lipid.Chains is MolecularSpeciesLevelChains mlChains)
            {
                spectrum.AddRange(GetAcylLevelSpectrum(lipid, mlChains.Chains, adduct));
                spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, mlChains.Chains.OfType<AcylChain>(), adduct));
            }
            if (lipid.Chains is PositionLevelChains plChains)
            {
                spectrum.AddRange(GetAcylLevelSpectrum(lipid, plChains.Chains, adduct));
                spectrum.AddRange(GetAcylPositionSpectrum(lipid, plChains.Chains[0], adduct));
                spectrum.AddRange(GetAcylDoubleBondSpectrum(lipid, plChains.Chains.OfType<AcylChain>(), adduct));
            }
            spectrum = spectrum.GroupBy(spec => spec, comparer)
                .Select(specs => new SpectrumPeak(specs.First().Mass, specs.Sum(n => n.Intensity), string.Join(", ", specs.Select(spec => spec.Comment)), specs.Aggregate(SpectrumComment.none, (a, b) => a | b.SpectrumComment)))
                .OrderBy(peak => peak.Mass)
                .ToList();
            return CreateReference(lipid, adduct, spectrum, molecule);
        }

        private MoleculeMsReference CreateReference(ILipid lipid, AdductIon adduct, List<SpectrumPeak> spectrum, IMoleculeProperty molecule)
        {
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

        private SpectrumPeak[] GetLPSSpectrum(ILipid lipid, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak(adduct.ConvertToMz(C3H8NO6P), 100d, "Header"){ SpectrumComment = SpectrumComment.metaboliteclass, IsAbsolutelyRequiredFragmentForAnnotation = true },
                new SpectrumPeak(adduct.ConvertToMz(Gly_C), 100d, "Gly-C"){ SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak(adduct.ConvertToMz(Gly_O), 100d, "Gly-O"){ SpectrumComment = SpectrumComment.metaboliteclass },
                //new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - H2O, 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
				//new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - CHO2, 200d, "Precursor -CHO2") { SpectrumComment = SpectrumComment.metaboliteclass },
                //new SpectrumPeak(adduct.ConvertToMz(lipid.Mass) - C3H8NO6P, 500d, "Precursor -C3H8NO6P") { SpectrumComment = SpectrumComment.metaboliteclass },
                //new SpectrumPeak(C3H8NO6P + MassDiffDictionary.ProtonMass, 100d, "Header") { SpectrumComment = SpectrumComment.metaboliteclass },
                //new SpectrumPeak(C3H9O6P + MassDiffDictionary.ProtonMass, 100d, "C3H9O6P") { SpectrumComment = SpectrumComment.metaboliteclass },
				//new SpectrumPeak(C3H9O6P + MassDiffDictionary.ProtonMass - H2O, 100d, "C3H9O6P") { SpectrumComment = SpectrumComment.metaboliteclass },
                //new SpectrumPeak(Gly_C + MassDiffDictionary.ProtonMass, 100d, "Gly-C") { SpectrumComment = SpectrumComment.metaboliteclass },
                //new SpectrumPeak(Gly_O + MassDiffDictionary.ProtonMass, 100d, "Gly-O") { SpectrumComment = SpectrumComment.metaboliteclass },
            };
            if (adduct.AdductIonName == "[M+H]+")
            {
                spectrum.AddRange
                (
                    new[]
                    {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C3H8NO6P), 500d, "Precursor -C3H8NO6P"){ SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - CHO2), 200d, "Precursor -CHO2"){ SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - H2O), 100d, "Precursor -H2O"){ SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(C3H4D5O6P), 100d, "C3H4D5O6P"){ SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(C3H4D5O6P- H2O), 100d, "C3H4D5O6P - H2O"){ SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
            }
            else if (adduct.AdductIonName == "[M+Na]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        //new SpectrumPeak(lipid.Mass - C3H8NO6P + MassDiffDictionary.ProtonMass, 250d, "Precursor -C3H8NO6P -Na") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass - C3H5NO2), 200d, "Precursor -C3H5NO2") { SpectrumComment = SpectrumComment.metaboliteclass },
                        //new SpectrumPeak(adduct.ConvertToMz(C3H8NO6P), 100d, "Header") { SpectrumComment = SpectrumComment.metaboliteclass },
                     }
                );
            }

            return spectrum.ToArray();
        }

        private IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, IEnumerable<AcylChain> acylChains, AdductIon adduct, double nlMass = 0.0)
        {
            return acylChains.SelectMany(acylChain => spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acylChain, adduct, 0, 30d));
        }


        private IEnumerable<SpectrumPeak> GetAcylLevelSpectrum(ILipid lipid, IEnumerable<IChain> acylChains, AdductIon adduct)
        {
            return acylChains.SelectMany(acylChain => GetAcylLevelSpectrum(lipid, acylChain, adduct));
        }

        private SpectrumPeak[] GetAcylLevelSpectrum(ILipid lipid, IChain acylChain, AdductIon adduct)
        {
            var chainMass = acylChain.Mass - MassDiffDictionary.HydrogenMass;
            var spectrum = new List<SpectrumPeak>();
            var lipidMass = lipid.Mass;
            if (chainMass != 0.0)
            {
                spectrum.AddRange
                (
                     new[]
                     {
                            new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass, 100d, $"{acylChain} acyl+") { SpectrumComment = SpectrumComment.acylchain },
                            new SpectrumPeak(adduct.ConvertToMz(lipidMass - chainMass), 100d, $"-{acylChain}") { SpectrumComment = SpectrumComment.acylchain },
                            new SpectrumPeak(adduct.ConvertToMz(lipidMass - chainMass-H2O), 100d, $"-{acylChain}-O") { SpectrumComment = SpectrumComment.acylchain },
                     }
                );
            }
            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetAcylPositionSpectrum(ILipid lipid, IChain acylChain, AdductIon adduct)
        {
            var lipidMass = lipid.Mass;
            var chainMass = acylChain.Mass - MassDiffDictionary.HydrogenMass;
            return new[]
            {
                new SpectrumPeak(adduct.ConvertToMz(lipidMass - chainMass - H2O - CD2), 100d, "-CD2(Sn1)") { SpectrumComment = SpectrumComment.snposition },
            };
        }


        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();
    }
}
