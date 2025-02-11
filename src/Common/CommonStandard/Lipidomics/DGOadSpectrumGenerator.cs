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
    public class DGOadSpectrumGenerator : ILipidSpectrumGenerator
    {
        private static readonly double NH3 = new[]
        {
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.NitrogenMass,
        }.Sum();
        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();
        private static readonly double Electron = 0.00054858026;

        private readonly IOadSpectrumPeakGenerator spectrumGenerator;
        public DGOadSpectrumGenerator()
        {
            spectrumGenerator = new OadSpectrumPeakGenerator();
        }

        public DGOadSpectrumGenerator(IOadSpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (adduct.AdductIonName == "[M+NH4]+"
                )
            {
                return true;
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null)
        {
            var abundance = 30;
            var nlMass =0.0;
            var spectrum = new List<SpectrumPeak>();
            spectrum.AddRange(GetDGOadSpectrum(lipid, adduct, nlMass));
            string[] oadId =
                new string[] {
                "OAD01",
                "OAD02",
                //"OAD02+O",
                "OAD03",
                "OAD04",
                //"OAD05",
                //"OAD06",
                //"OAD07",
                //"OAD08",
                //"OAD09",
                //"OAD10",
                //"OAD11",
                //"OAD12",
                //"OAD13",
                "OAD14",
                "OAD15",
                //"OAD15+O",
                "OAD16",
                "OAD17",
                //"OAD12+O",
                //"OAD12+O+H",
                //"OAD12+O+2H",
                "OAD01+H" } ;
            string[] oadIdLossH2O =
                new string[] {
                "OAD01",
                "OAD02",
                //"OAD02+O",
                //"OAD03",
                //"OAD04",
                //"OAD05",
                //"OAD06",
                //"OAD07",
                //"OAD08",
                //"OAD09",
                //"OAD10",
                //"OAD11",
                //"OAD12",
                //"OAD13",
                "OAD14",
                "OAD15",
                //"OAD15+O",
                "OAD16",
                //"OAD17",
                //"OAD12+O",
                //"OAD12+O+H",
                //"OAD12+O+2H",
                //"OAD01+H"
                };

            if (lipid.Chains is PositionLevelChains plChains)
            {
                foreach (AcylChain chain in plChains.Chains)
                {
                    spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, chain, adduct, nlMass, abundance, oadId));
                    spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, chain, adduct, NH3 + H2O, abundance, oadIdLossH2O));
                }
            }
            spectrum = spectrum.GroupBy(spec => spec, comparer)
                .Select(specs => new SpectrumPeak(specs.First().Mass, specs.Sum(n => n.Intensity), string.Join(", ", specs.Select(spec => spec.Comment)), specs.Aggregate(SpectrumComment.none, (a, b) => a | b.SpectrumComment)))
                .OrderBy(peak => peak.Mass)
                .ToList();
            return CreateReference(lipid, adduct, spectrum, molecule);
        }

        private SpectrumPeak[] GetDGOadSpectrum(Lipid lipid, AdductIon adduct, double nlMass)
        {
            var spectrum = new List<SpectrumPeak>();

            if (adduct.AdductIonName == "[M+NH4]+")
            {
                spectrum.AddRange
                (
                    new[]
                    {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                        new SpectrumPeak(lipid.Mass + MassDiffDictionary.ProtonMass, 50d, "[M+H]+") { SpectrumComment = SpectrumComment.metaboliteclass },
                        new SpectrumPeak(lipid.Mass - H2O + MassDiffDictionary.ProtonMass, 200d, "[M+H]+ -H2O") { SpectrumComment = SpectrumComment.metaboliteclass },
                    }
                );
                if (lipid.Chains is SeparatedChains Chains)
                {
                    foreach (AcylChain chain in Chains.Chains)
                    {
                        spectrum.AddRange
                        (
                            new[]
                            {
                                new SpectrumPeak(lipid.Mass - chain.Mass - MassDiffDictionary.OxygenMass - Electron, 50d, $"-{chain}") { SpectrumComment = SpectrumComment.acylchain },
                                //new SpectrumPeak(adduct.ConvertToMz(chain.Mass - MassDiffDictionary.HydrogenMass), 20d, $"{chain} Acyl+") { SpectrumComment = SpectrumComment.acylchain },
                                //new SpectrumPeak(adduct.ConvertToMz(chain.Mass ), 5d, $"{chain} Acyl+ +H") { SpectrumComment = SpectrumComment.acylchain },
                            }
                        );
                    }
                }
            }
            else
            {
                spectrum.AddRange
                (
                    new[] {
                        new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                    }
                );
            }
            return spectrum.ToArray();
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

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();

    }
}

