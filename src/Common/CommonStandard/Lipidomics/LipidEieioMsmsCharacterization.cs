﻿using System.Linq;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Lipidomics
{
    public sealed class LipidEieioMsmsCharacterization
    {
        private LipidEieioMsmsCharacterization() { }

        private const double Electron = 0.00054858026;
        private const double Proton = 1.00727641974;
        private const double H2O = 18.010564684;
        private const double Sugar162 = 162.052823422;
        private const double Na = 22.98977;

        public static LipidMolecule JudgeIfPhosphatidylcholine(IMSScanProperty msScanProp, double ms2Tolerance,
           double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
           AdductIon adduct)
        {

            var spectrum = msScanProp.Spectrum;
            var candidates = new List<LipidMolecule>();

            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {

                    if (sn1Carbon < 10 || sn2Carbon < 10) return null;
                    if (sn1Double > 6 || sn2Double > 6) return null;

                    // seek 184.07332 (C5H15NO4P)
                    var threshold = 3.0;
                    var diagnosticMz = 184.07332;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // for eieio
                    var PEHeaderLoss = theoreticalMz - 141.019094261 + MassDiffDictionary.ProtonMass;
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, PEHeaderLoss, 5.0);
                    if (isClassIonFound2 && LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, PEHeaderLoss, diagnosticMz))
                    {
                        return null;
                    }

                    // from here, acyl level annotation is executed.
                    var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN1_H2O = nl_SN1 - H2O;

                    var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;
                    var nl_NS2_H2O = nl_SN2 - H2O;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN1_H2O, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_NS2_H2O, Intensity = 0.01 }
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount >= 2)
                    { // now I set 2 as the correct level
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PC", LbmClass.PC, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC", LbmClass.PC, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);

                }
                //addMT
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek 184.07332 (C5H15NO4P)
                    var threshold = 3.0;
                    var diagnosticMz = 184.07332;
                    // seek [M+Na -C5H14NO4P]+
                    var diagnosticMz2 = theoreticalMz - 183.06604;
                    // seek [M+Na -C3H9N]+
                    var diagnosticMz3 = theoreticalMz - 59.0735;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold);
                    if (isClassIonFound == false || isClassIon2Found == false) return null;

                    // from here, acyl level annotation is executed.
                    var nl_SN1 = diagnosticMz3 - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - H2O + MassDiffDictionary.HydrogenMass;
                    var nl_SN2 = diagnosticMz3 - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - H2O + MassDiffDictionary.HydrogenMass;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 0.01 },
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount >= 2)
                    { // now I set 2 as the correct level
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PC", LbmClass.PC, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC", LbmClass.PC, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfPhosphatidylethanolamine(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PE 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
            AdductIon adduct)
        {

            var spectrum = msScanProp.Spectrum;
            var candidates = new List<LipidMolecule>();

            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek -141.019094261 (C2H8NO4P)
                    var threshold = 2.5;
                    var diagnosticMz = theoreticalMz - 141.019094261;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var sn1 = LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - Electron;
                    var sn2 = LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - Electron;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = sn1, Intensity = 0.1 },
                                new SpectrumPeak() { Mass = sn2, Intensity = 0.1 }
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2)
                    { // now I set 2 as the correct level
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PE", LbmClass.PE, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE", LbmClass.PE, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                //addMT
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek -141.019094261 (C2H8NO4P)
                    var threshold = 3.0;
                    var diagnosticMz = theoreticalMz - 141.019094261;
                    // seek - 43.042199 (C2H5N)
                    var diagnosticMz2 = theoreticalMz - 43.042199;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var sn1 = LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - Electron;
                    var sn2 = LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - Electron;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = sn1, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = sn2, Intensity = 0.01 },
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2)
                    { // now I set 2 as the correct level
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PE", LbmClass.PE, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE", LbmClass.PE, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }

            return null;
        }

        public static LipidMolecule JudgeIfPhosphatidylglycerol(IMSScanProperty msScanProp, double ms2Tolerance,
           double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PS 46:6, totalCarbon = 46 and totalDoubleBond = 6
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
           AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            var candidates = new List<LipidMolecule>();

            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek -189.040227 (C3H8O6P+NH4)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 189.040227;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 0.01 }
                            };

                    if (LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, nl_SN1, diagnosticMz) &&
                        LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, nl_SN2, diagnosticMz))
                    {
                        // meaning high possibility that the spectrum belongs to BMP
                        return null;
                    }

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2)
                    {
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PG", LbmClass.PG, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PG", LbmClass.PG, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek -171.005851 (C3H8O6P) - 22.9892207 (Na+)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 171.005851 - 22.9892207;// + MassDiffDictionary.HydrogenMass;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PG", LbmClass.PG, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);

                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfBismonoacylglycerophosphate(IMSScanProperty msScanProp, double ms2Tolerance,
           double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PS 46:6, totalCarbon = 46 and totalDoubleBond = 6
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
           AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek -189.040227 (C3H8O6P+NH4)
                    var threshold = 0.01;
                    var diagnosticMz = theoreticalMz - 189.040227;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    //if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 10 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 10 }
                            };

                    if (LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, diagnosticMz, nl_SN1) &&
                        LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, diagnosticMz, nl_SN2))
                    {
                        // meaning high possibility that the spectrum belongs to PG
                        return null;
                    }

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2)
                    {
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("BMP", LbmClass.BMP, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    if (candidates.Count == 0) return null;
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("BMP", LbmClass.BMP, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);

                }
            }
            return null;
        }



        public static LipidMolecule JudgeIfLysopc(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // 
            int snCarbon, int snDoubleBond,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (snCarbon > totalCarbon) snCarbon = totalCarbon;
            if (snDoubleBond > totalDoubleBond) snDoubleBond = totalDoubleBond;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPC
                    // seek 184.07332 (C5H15NO4P)
                    var threshold = 3.0;
                    var diagnosticMz = 184.07332;
                    var diagnosticMz2 = 104.106990;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIon1Found != true) return null;

                    // for eieio
                    var PEHeaderLoss = theoreticalMz - 141.019094261 + MassDiffDictionary.ProtonMass;
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, PEHeaderLoss, 3.0);
                    if (isClassIonFound2 && LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, PEHeaderLoss, diagnosticMz))
                    {
                        return null;
                    }


                    var candidates = new List<LipidMolecule>();
                    var chainSuffix = "";
                    var diagnosticMzExist = 0.0;
                    var diagnosticMzIntensity = 0.0;
                    var diagnosticMzExist2 = 0.0;
                    var diagnosticMzIntensity2 = 0.0;

                    for (int i = 0; i < spectrum.Count; i++)
                    {
                        var mz = spectrum[i].Mass;
                        var intensity = spectrum[i].Intensity;

                        if (intensity > threshold && Math.Abs(mz - diagnosticMz) < ms2Tolerance)
                        {
                            diagnosticMzExist = mz;
                            diagnosticMzIntensity = intensity;
                        }
                        else if (intensity > threshold && Math.Abs(mz - diagnosticMz2) < ms2Tolerance)
                        {
                            diagnosticMzExist2 = mz;
                            diagnosticMzIntensity2 = intensity;
                        }
                    };

                    if (diagnosticMzIntensity2 / diagnosticMzIntensity > 0.3) //
                    {
                        chainSuffix = "/0:0";
                    }

                    var score = 0.0;
                    if (totalCarbon < 30) score = score + 1.0;
                    var molecule = LipidMsmsCharacterizationUtility.getSingleacylchainwithsuffixMoleculeObjAsLevel2("LPC", LbmClass.LPC, totalCarbon, totalDoubleBond,
                    score, chainSuffix);
                    candidates.Add(molecule);

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPC", LbmClass.LPC, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);

                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPC
                    // seek PreCursor - 59 (C3H9N)
                    var threshold = 3.0;
                    var diagnosticMz = theoreticalMz - 59.072951;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();
                    var score = 0.0;
                    if (totalCarbon < 30) score = score + 1.0;
                    var molecule = LipidMsmsCharacterizationUtility.getSingleacylchainMoleculeObjAsLevel2("LPC", LbmClass.LPC, totalCarbon, totalDoubleBond,
                    score);
                    candidates.Add(molecule);

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPC", LbmClass.LPC, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }

            return null;
        }

        public static LipidMolecule JudgeIfLysope(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // 
            int sn1Carbon, int sn1Double,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPE

                    // seek PreCursor -141(C2H8NO4P)
                    var threshold = 2.5;
                    var diagnosticMz = theoreticalMz - 141.019094;

                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIon1Found == false) return null;
                    var sn1alkyl = (MassDiffDictionary.CarbonMass * sn1Carbon)
                                        + (MassDiffDictionary.HydrogenMass * ((sn1Carbon * 2) - (sn1Double * 2) + 1));//sn1(ether)

                    var NL_sn1 = diagnosticMz - sn1alkyl + Proton;
                    var sn1_rearrange = sn1alkyl + MassDiffDictionary.HydrogenMass * 2 + 139.00290;//sn1(ether) + C2H5NO4P + proton 

                    // reject EtherPE 
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, NL_sn1, threshold);
                    var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, sn1_rearrange, threshold);
                    if (isClassIon2Found == true || isClassIon3Found == true) return null;

                    var candidates = new List<LipidMolecule>();
                    if (totalCarbon > 30)
                    {
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE", LbmClass.EtherPE, "e", theoreticalMz, adduct,
                           totalCarbon, totalDoubleBond + 1, 0, candidates, 1);
                    }
                    else
                    {
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPE", LbmClass.LPE, "", theoreticalMz, adduct,
                           totalCarbon, totalDoubleBond, 0, candidates, 1);
                    }


                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek PreCursor -141(C2H8NO4P)
                    var threshold = 3.0;
                    var diagnosticMz = theoreticalMz - 141.019094;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // reject EtherPE 
                    var sn1alkyl = (MassDiffDictionary.CarbonMass * sn1Carbon)
                                        + (MassDiffDictionary.HydrogenMass * ((sn1Carbon * 2) - (sn1Double * 2) + 1));//sn1(ether)

                    var NL_sn1 = diagnosticMz - sn1alkyl + Proton;
                    var sn1_rearrange = sn1alkyl + 139.00290 + MassDiffDictionary.HydrogenMass * 2;//sn1(ether) + C2H5NO4P + proton 

                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, NL_sn1, threshold);
                    var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, sn1_rearrange, threshold);
                    if (isClassIon2Found == true || isClassIon3Found == true) return null;

                    var candidates = new List<LipidMolecule>();
                    if (totalCarbon > 30)
                    {
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE", LbmClass.EtherPE, "e", theoreticalMz, adduct,
                           totalCarbon, totalDoubleBond + 1, 0, candidates, 2);
                    }
                    else
                    {
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPE", LbmClass.LPE, "", theoreticalMz, adduct,
                           totalCarbon, totalDoubleBond, 0, candidates, 1);
                    }
                }
            }

            return null;
        }
        public static LipidMolecule JudgeIfLysopg(IMSScanProperty msScanProp, double ms2Tolerance,
    double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PE 46:6, totalCarbon = 46 and totalDoubleBond = 6
    int snCarbon, int snDoubleBond,
    AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Negative)
            { //
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C3H6O5P-

                    var diagnosticMz1 = 152.99583;  // seek C3H6O5P-
                    var threshold1 = 1.0;
                    var diagnosticMz2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(totalCarbon, totalDoubleBond); // seek [FA-H]-
                    var threshold2 = 10.0;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;

                    //
                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPG", LbmClass.LPG, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            else if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek Header loss (MG+ + chain Acyl) 
                    var threshold = 5.0;
                    var diagnosticMz = LipidMsmsCharacterizationUtility.acylCainMass(snCarbon, snDoubleBond) + (12 * 3 + MassDiffDictionary.HydrogenMass * 5 + MassDiffDictionary.OxygenMass * 2) + MassDiffDictionary.ProtonMass;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPG", LbmClass.LPG, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }

            return null;
        }

        public static LipidMolecule JudgeIfLysopi(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PE 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int snCarbon, int snDoubleBond,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Negative)
            { //negative ion mode only
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C3H6O5P-

                    var diagnosticMz1 = 241.0118806 + Electron;  // seek C3H6O5P-
                    var threshold1 = 1.0;
                    var diagnosticMz2 = 315.048656; // seek C9H16O10P-
                    var threshold2 = 1.0;
                    var diagnosticMz3 = LipidMsmsCharacterizationUtility.fattyacidProductIon(totalCarbon, totalDoubleBond); // seek [FA-H]-
                    var threshold3 = 10.0;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold3);
                    if (isClassIon1Found != true || isClassIon2Found != true || isClassIon3Found != true) return null;

                    //
                    var candidates = new List<LipidMolecule>();
                    //var averageIntensity = 0.0;

                    //var molecule = getSingleacylchainMoleculeObjAsLevel2("LPI", LbmClass.LPI, totalCarbon, totalDoubleBond,
                    //averageIntensity);
                    //candidates.Add(molecule);
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPI", LbmClass.LPI, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            else if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek Header loss (MG+ + chain Acyl) 
                    var threshold = 5.0;
                    var diagnosticMz = LipidMsmsCharacterizationUtility.acylCainMass(snCarbon, snDoubleBond) + (12 * 3 + MassDiffDictionary.HydrogenMass * 5 + MassDiffDictionary.OxygenMass * 2) + MassDiffDictionary.ProtonMass;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPI", LbmClass.LPI, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }
        public static LipidMolecule JudgeIfLysops(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PE 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int snCarbon, int snDoubleBond,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Negative)
            { //negative ion mode only
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C3H6O5P-

                    var diagnosticMz1 = 152.99583;  // seek C3H6O5P-
                    var threshold1 = 10.0;
                    var diagnosticMz2 = theoreticalMz - 87.032029; // seek -C3H6NO2-H
                    var threshold2 = 5.0;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;

                    //
                    var candidates = new List<LipidMolecule>();
                    //var averageIntensity = 0.0;

                    //var molecule = getSingleacylchainMoleculeObjAsLevel2("LPS", LbmClass.LPS, totalCarbon, totalDoubleBond,
                    //averageIntensity);
                    //candidates.Add(molecule);
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPS", LbmClass.LPS, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            else if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek Header loss (MG+ + chain Acyl) 
                    var threshold = 5.0;
                    var diagnosticMz = LipidMsmsCharacterizationUtility.acylCainMass(snCarbon, snDoubleBond) + (12 * 3 + MassDiffDictionary.HydrogenMass * 5 + MassDiffDictionary.OxygenMass * 2) + MassDiffDictionary.ProtonMass;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPS", LbmClass.LPS, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfSphingomyelin(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int sphCarbon, int acylCarbon, int sphDouble, int acylDouble,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek 184.07332 (C5H15NO4P)
                    var threshold = 10.0;
                    var diagnosticMz = 184.07332;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;
                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    if (sphCarbon <= 13) return null;
                    if (sphCarbon == 16 && sphDouble >= 3) return null;
                    if (acylCarbon < 8) return null;

                    var C5H14NO4P = 183.066047;
                    var C2H2N = 40.018724;

                    var diagnosChain1 = LipidMsmsCharacterizationUtility.acylCainMass(acylCarbon, acylDouble) + C2H2N + MassDiffDictionary.HydrogenMass + Proton;
                    var diagnosChain2 = diagnosChain1 + C5H14NO4P - MassDiffDictionary.HydrogenMass;
                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = diagnosChain1, Intensity = 0.5 },
                                new SpectrumPeak() { Mass = diagnosChain2, Intensity = 1.0 }
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount > 1)
                    { // the diagnostic acyl ion must be observed for level 2 annotation
                        var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("SM", LbmClass.SM, "d", sphCarbon, sphDouble,
                            acylCarbon, acylDouble, averageIntensity);
                        candidates.Add(molecule);
                    }

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("SM", LbmClass.SM, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);

                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek -59.0735 [M-C3H9N+Na]+
                    var threshold = 20.0;
                    var diagnosticMz = theoreticalMz - 59.0735;
                    // seek -183.06604 [M-C5H14NO4P+Na]+
                    var threshold2 = 30.0;
                    var diagnosticMz2 = theoreticalMz - 183.06604;
                    // seek -183.06604 [M-C5H16NO5P+H]+
                    var threshold3 = 1;
                    var diagnosticMz3 = theoreticalMz - 183.06604 - 39.993064;

                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold3);
                    //if (isClassIonFound == !true || isClassIon2Found == !true || isClassIon3Found == !true) return null;
                    if (isClassIonFound == false || isClassIon2Found == false) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("SM", LbmClass.SM, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);

                }

            }

            return null;
        }

        public static LipidMolecule JudgeIfAcylcarnitine(IMSScanProperty msScanProp, double ms2Tolerance, float theoreticalMz,
            int totalCarbon, int totalDoubleBond, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M]+")
                {
                    // seek 85.028405821 (C4H5O2+)
                    var threshold = 5.0;
                    var diagnosticMz = 85.028405821;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);

                    // seek -CHO2
                    var diagnosticMz1 = theoreticalMz - (MassDiffDictionary.CarbonMass + MassDiffDictionary.HydrogenMass + MassDiffDictionary.OxygenMass * 2);
                    var isClassIonFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold);

                    // seek Acyl loss
                    var acyl = LipidMsmsCharacterizationUtility.acylCainMass(totalCarbon, totalDoubleBond);
                    var diagnosticMz2 = theoreticalMz - acyl + MassDiffDictionary.ProtonMass;
                    var threshold2 = 0.01;
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);

                    // seek 144.1019 (Acyl and H2O loss) // not found at PasefOn case
                    var diagnosticMz3 = diagnosticMz2 - H2O;
                    var threshold3 = 0.01;
                    var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold3);

                    if (isClassIonFound == false && isClassIonFound1 == false)
                    {
                        if (isClassIonFound2 == false && isClassIonFound3 == false)
                        {
                            return null;
                        }
                    };
                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("CAR", LbmClass.CAR, string.Empty, theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 1);

                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfEtherpe(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PE 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int minSnCarbon, int maxSnCarbon, int minSnDoubleBond, int maxSnDoubleBond,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (maxSnCarbon > totalCarbon) maxSnCarbon = totalCarbon;
            if (maxSnDoubleBond > totalDoubleBond) maxSnDoubleBond = totalDoubleBond;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek -141.019094261 (C2H8NO4P)
                    var threshold = 0.5;
                    var diagnosticMz = theoreticalMz - 141.019094261;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    for (int sn1Carbon = minSnCarbon; sn1Carbon <= maxSnCarbon; sn1Carbon++)
                    {
                        for (int sn1Double = minSnDoubleBond; sn1Double <= maxSnDoubleBond; sn1Double++)
                        {
                            if (sn1Double >= 5) continue;
                            var sn2Carbon = totalCarbon - sn1Carbon;
                            var sn2Double = totalDoubleBond - sn1Double;
                            var sn1alkyl = (MassDiffDictionary.CarbonMass * sn1Carbon)
                                        + (MassDiffDictionary.HydrogenMass * ((sn1Carbon * 2) - (sn1Double * 2) + 1));//sn1(carbon chain)

                            var NL_sn1 = theoreticalMz - sn1alkyl - MassDiffDictionary.OxygenMass;
                            var NL_sn2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - H2O;


                            var query = new List<SpectrumPeak> {
                                    new SpectrumPeak() { Mass = NL_sn1, Intensity = 1 },
                                    new SpectrumPeak() { Mass = NL_sn2, Intensity = 1 },
                                };

                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                            if (foundCount == 2)
                            { // now I set 2 as the correct level

                                var etherSuffix = "e";
                                var sn1Double2 = sn1Double;
                                if (sn1Double > 0)
                                {
                                    sn1Double2 = sn1Double - 1;
                                    etherSuffix = "p";
                                };

                                var molecule = LipidMsmsCharacterizationUtility.getEtherPhospholipidMoleculeObjAsLevel2("PE", LbmClass.EtherPE, sn1Carbon, sn1Double2,
                                    sn2Carbon, sn2Double, averageIntensity, etherSuffix);
                                candidates.Add(molecule);
                            }
                        }
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE", LbmClass.EtherPE, "e", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            else
            {
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C5H11NO5P-
                    var threshold = 5.0;
                    var diagnosticMz = 196.03803;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    for (int sn1Carbon = minSnCarbon; sn1Carbon <= maxSnCarbon; sn1Carbon++)
                    {
                        for (int sn1Double = minSnDoubleBond; sn1Double <= maxSnDoubleBond; sn1Double++)
                        {

                            var sn2Carbon = totalCarbon - sn1Carbon;
                            var sn2Double = totalDoubleBond - sn1Double;
                            if (sn1Carbon >= 24 && sn1Double >= 5) return null;

                            var sn2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn2Carbon, sn2Double);
                            var NL_sn2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;

                            var query = new List<SpectrumPeak> {
                            new SpectrumPeak() { Mass = sn2, Intensity = 10.0 },
                            new SpectrumPeak() { Mass = NL_sn2, Intensity = 0.1 }
                        };

                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                            if (foundCount == 2)
                            { // now I set 2 as the correct level
                                var molecule = LipidMsmsCharacterizationUtility.getEtherPhospholipidMoleculeObjAsLevel2("PE", LbmClass.EtherPE, sn1Carbon, sn1Double,
                                    sn2Carbon, sn2Double, averageIntensity, "e");
                                candidates.Add(molecule);
                            }
                        }
                    }

                    if (candidates.Count == 0) return null;
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE", LbmClass.EtherPE, "e", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfPhosphatidylcholineD5(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            var candidates = new List<LipidMolecule>();

            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek 184.07332 (C5H15NO4P)
                    var threshold = 3.0;
                    var diagnosticMz = 184.07332;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // for eieio
                    var PEHeaderLoss = theoreticalMz - 141.019094261 + MassDiffDictionary.ProtonMass;
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, PEHeaderLoss, 5.0);
                    //if (isClassIonFound2 && LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, PEHeaderLoss, diagnosticMz))
                    if (isClassIonFound2)
                    {
                        return null;
                    }

                    // from here, acyl level annotation is executed.
                    if (sn1Carbon < 10 || sn2Carbon < 10) return null;
                    if (sn1Double > 6 || sn2Double > 6) return null;

                    var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN1_H2O = nl_SN1 - H2O;

                    var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;
                    var nl_NS2_H2O = nl_SN2 - H2O;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN1_H2O, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_NS2_H2O, Intensity = 0.01 }
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount >= 2)
                    { // now I set 2 as the correct level
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PC_d5", LbmClass.PC_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC_d5", LbmClass.PC_d5, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);

                }
                //addMT
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek 184.07332 (C5H15NO4P)
                    var threshold = 5.0;
                    var diagnosticMz = 184.07332;
                    // seek [M+Na -C5H14NO4P]+
                    var diagnosticMz2 = theoreticalMz - 183.06604;
                    // seek [M+Na -C3H9N]+
                    var diagnosticMz3 = theoreticalMz - 59.0735;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold);
                    if (isClassIonFound == false || isClassIon2Found == false) return null;

                    // from here, acyl level annotation is executed.
                    var nl_SN1 = diagnosticMz3 - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - H2O + MassDiffDictionary.HydrogenMass;
                    var nl_SN2 = diagnosticMz3 - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - H2O + MassDiffDictionary.HydrogenMass;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 0.01 },
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount >= 2)
                    { // now I set 2 as the correct level
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PC_d5", LbmClass.PC_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC_d5", LbmClass.PC_d5, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfPhosphatidylethanolamineD5(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PE 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            var candidates = new List<LipidMolecule>();

            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek -141.019094261 (C2H8NO4P)
                    var threshold = 2.5;
                    var diagnosticMz = theoreticalMz - 141.019094261;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var sn1 = LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - Electron;
                    var sn2 = LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - Electron;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = sn1, Intensity = 0.1 },
                                new SpectrumPeak() { Mass = sn2, Intensity = 0.1 }
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2)
                    { // now I set 2 as the correct level
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PE_d5", LbmClass.PE_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE_d5", LbmClass.PE_d5, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                //addMT
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek -141.019094261 (C2H8NO4P)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 141.019094261;
                    // seek - 43.042199 (C2H5N)
                    var diagnosticMz2 = theoreticalMz - 43.042199;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var sn1 = LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - Electron;
                    var sn2 = LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - Electron;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = sn1, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = sn2, Intensity = 0.01 },
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2)
                    { // now I set 2 as the correct level
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PE_d5", LbmClass.PE_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE_d5", LbmClass.PE_d5, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }


        public static LipidMolecule JudgeIfPhosphatidylserineD5(IMSScanProperty msScanProp, double ms2Tolerance,
           double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PS 46:6, totalCarbon = 46 and totalDoubleBond = 6
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
           AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek -185.008927 (C3H8NO6P)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 185.008927;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN1_H2O = nl_SN1 - H2O;

                    var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;
                    var nl_NS2_H2O = nl_SN2 - H2O;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN1_H2O, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_NS2_H2O, Intensity = 0.01 }
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount >= 2)
                    { // now I set 2 as the correct level
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PS_d5", LbmClass.PS_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PS_d5", LbmClass.PS_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 2);

                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek -185.008927 (C3H8NO6P)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 185.008927;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // acyl level may be not able to annotate.
                    var candidates = new List<LipidMolecule>();
                    //var score = 0;
                    //var molecule = getLipidAnnotaionAsLevel1("PS_d5", LbmClass.PS_d5, totalCarbon, totalDoubleBond, score, "");
                    //candidates.Add(molecule);

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PS_d5", LbmClass.PS_d5, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfPhosphatidylglycerolD5(IMSScanProperty msScanProp, double ms2Tolerance,
           double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PS 46:6, totalCarbon = 46 and totalDoubleBond = 6
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
           AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek -189.040227 (C3H8O6P+NH4)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 189.040227;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 0.01 }
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2 && averageIntensity < 30)
                    { // average intensity < 30 is nessesarry to distinguish it from BMP
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PG_d5", LbmClass.PG_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PG_d5", LbmClass.PG_d5, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek -171.005851 (C3H8O6P) - 22.9892207 (Na+)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 171.005851 - 22.9892207;// + MassDiffDictionary.HydrogenMass;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PG_d5", LbmClass.PG_d5, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);

                }
            }
            return null;
        }
        public static LipidMolecule JudgeIfPhosphatidylinositolD5(IMSScanProperty msScanProp, double ms2Tolerance,
           double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PS 46:6, totalCarbon = 46 and totalDoubleBond = 6
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
           AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek -277.056272 (C6H12O9P+NH4)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 277.056272;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PI_d5", LbmClass.PI_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // 
                    var threshold = 10.0;
                    var diagnosticMz1 = theoreticalMz - (259.021895 + 22.9892207);  // seek -(C6H12O9P +Na)
                    var diagnosticMz2 = theoreticalMz - (260.02972);                 // seek -(C6H12O9P + H)
                    var diagnosticMz3 = (260.02972 + 22.9892207);                   // seek (C6H13O9P +Na)

                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold);
                    var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold);
                    if (!isClassIon1Found || !isClassIon2Found || !isClassIon3Found) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    //var score = 0;
                    //var molecule = getLipidAnnotaionAsLevel1("PI_d5", LbmClass.PI_d5, totalCarbon, totalDoubleBond, score, "");
                    //candidates.Add(molecule);

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PI_d5", LbmClass.PI_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }

            }
            return null;
        }
        public static LipidMolecule JudgeIfLysopcD5(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // 
            int snCarbon, int snDoubleBond,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPC
                    // seek 184.07332 (C5H15NO4P)
                    var threshold = 3.0;
                    var diagnosticMz = 184.07332;
                    var diagnosticMz2 = 104.106990;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIon1Found != true) return null;

                    // for eieio
                    var PEHeaderLoss = theoreticalMz - 141.019094261 + MassDiffDictionary.ProtonMass;
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, PEHeaderLoss, 3.0);
                    if (isClassIonFound2 && LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, PEHeaderLoss, diagnosticMz))
                    {
                        return null;
                    }
                    //
                    var candidates = new List<LipidMolecule>();
                    var chainSuffix = "";
                    var diagnosticMzExist = 0.0;
                    var diagnosticMzIntensity = 0.0;
                    var diagnosticMzExist2 = 0.0;
                    var diagnosticMzIntensity2 = 0.0;

                    for (int i = 0; i < spectrum.Count; i++)
                    {
                        var mz = spectrum[i].Mass;
                        var intensity = spectrum[i].Intensity;

                        if (intensity > threshold && Math.Abs(mz - diagnosticMz) < ms2Tolerance)
                        {
                            diagnosticMzExist = mz;
                            diagnosticMzIntensity = intensity;
                        }
                        else if (intensity > threshold && Math.Abs(mz - diagnosticMz2) < ms2Tolerance)
                        {
                            diagnosticMzExist2 = mz;
                            diagnosticMzIntensity2 = intensity;
                        }
                    };

                    if (diagnosticMzIntensity2 / diagnosticMzIntensity > 0.3) //
                    {
                        chainSuffix = "/0:0";
                    }

                    var score = 0.0;
                    if (totalCarbon < 30) score = score + 1.0;
                    var molecule = LipidMsmsCharacterizationUtility.getSingleacylchainwithsuffixMoleculeObjAsLevel2("LPC_d5", LbmClass.LPC_d5, totalCarbon, totalDoubleBond,
                    score, chainSuffix);
                    candidates.Add(molecule);

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPC_d5", LbmClass.LPC_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);

                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPC
                    // seek PreCursor - 59 (C3H9N)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 59.072951;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;
                    // seek 104.1070 (C5H14NO) maybe not found
                    //var threshold2 = 1.0;
                    //var diagnosticMz2 = 104.1070;

                    //
                    var candidates = new List<LipidMolecule>();
                    var score = 0.0;
                    if (totalCarbon < 30) score = score + 1.0;
                    var molecule = LipidMsmsCharacterizationUtility.getSingleacylchainMoleculeObjAsLevel2("LPC_d5", LbmClass.LPC_d5, totalCarbon, totalDoubleBond,
                    score);
                    candidates.Add(molecule);

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPC_d5", LbmClass.LPC_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfLysopeD5(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // 
            int snCarbon, int snDoubleBond,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPE

                    // seek PreCursor -141(C2H8NO4P)
                    var threshold = 2.5;
                    var diagnosticMz = theoreticalMz - 141.019094;

                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIon1Found == false) return null;
                    // reject EtherPE 
                    var sn1alkyl = (MassDiffDictionary.CarbonMass * snCarbon)
                                        + (MassDiffDictionary.HydrogenMass * ((snCarbon * 2) - (snDoubleBond * 2) + 1));//sn1(ether)

                    var NL_sn1 = diagnosticMz - sn1alkyl + Proton;
                    var sn1_rearrange = sn1alkyl + MassDiffDictionary.HydrogenMass * 2 + 139.00290;//sn1(ether) + C2H5NO4P + proton 

                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, NL_sn1, threshold);
                    var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, sn1_rearrange, threshold);
                    if (isClassIon2Found == true || isClassIon3Found == true) return null;


                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPE_d5", LbmClass.LPE_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek PreCursor -141(C2H8NO4P)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 141.019094;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // reject EtherPE 
                    var sn1alkyl = (MassDiffDictionary.CarbonMass * snCarbon)
                                       + (MassDiffDictionary.HydrogenMass * ((snCarbon * 2) - (snDoubleBond * 2) + 1));//sn1(ether)

                    var NL_sn1 = diagnosticMz - sn1alkyl + Proton;
                    var sn1_rearrange = sn1alkyl + 139.00290 + MassDiffDictionary.HydrogenMass * 2;//sn1(ether) + C2H5NO4P + proton 

                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, NL_sn1, threshold);
                    var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, sn1_rearrange, threshold);
                    if (isClassIon2Found == true || isClassIon3Found == true) return null;

                    //
                    var candidates = new List<LipidMolecule>();
                    //var score = 0.0;
                    //if (totalCarbon < 30) score = score + 1.0;
                    //var molecule = getSingleacylchainMoleculeObjAsLevel2("LPE_d5", LbmClass.LPE_d5, totalCarbon, totalDoubleBond,
                    //score);
                    //candidates.Add(molecule);
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPE_d5", LbmClass.LPE_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }
        public static LipidMolecule JudgeIfLysopgD5(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PE 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int snCarbon, int snDoubleBond,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Negative)
            { //
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C3H6O5P-

                    var diagnosticMz1 = 152.99583;  // seek C3H6O5P-
                    var threshold1 = 1.0;
                    var diagnosticMz2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(totalCarbon, totalDoubleBond); // seek [FA-H]-
                    var threshold2 = 10.0;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;

                    //
                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPG_d5", LbmClass.LPG_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            else if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek Header loss (MG+ + chain Acyl) 
                    var threshold = 5.0;
                    var diagnosticMz = LipidMsmsCharacterizationUtility.acylCainMass(snCarbon, snDoubleBond) + (12 * 3 + MassDiffDictionary.Hydrogen2Mass * 5 + MassDiffDictionary.OxygenMass * 2) + MassDiffDictionary.ProtonMass;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPG_d5", LbmClass.LPG_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }

            return null;
        }

        public static LipidMolecule JudgeIfLysopiD5(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PE 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int snCarbon, int snDoubleBond,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Negative)
            { //negative ion mode only
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C3H6O5P-

                    var diagnosticMz1 = 241.0118806 + Electron;  // seek C3H6O5P-
                    var threshold1 = 1.0;
                    var diagnosticMz2 = 315.048656; // seek C9H16O10P-
                    var threshold2 = 1.0;
                    var diagnosticMz3 = LipidMsmsCharacterizationUtility.fattyacidProductIon(totalCarbon, totalDoubleBond); // seek [FA-H]-
                    var threshold3 = 10.0;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold3);
                    if (isClassIon1Found != true || isClassIon2Found != true || isClassIon3Found != true) return null;

                    //
                    var candidates = new List<LipidMolecule>();
                    //var averageIntensity = 0.0;

                    //var molecule = getSingleacylchainMoleculeObjAsLevel2("LPI_d5", LbmClass.LPI_d5, totalCarbon, totalDoubleBond,
                    //averageIntensity);
                    //candidates.Add(molecule);
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPI_d5", LbmClass.LPI_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            else if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek Header loss (MG+ + chain Acyl) 
                    var threshold = 5.0;
                    var diagnosticMz = LipidMsmsCharacterizationUtility.acylCainMass(snCarbon, snDoubleBond) + (12 * 3 + MassDiffDictionary.Hydrogen2Mass * 5 + MassDiffDictionary.OxygenMass * 2) + MassDiffDictionary.ProtonMass;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPI_d5", LbmClass.LPI_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }
        public static LipidMolecule JudgeIfLysopsD5(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PE 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int snCarbon, int snDoubleBond,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Negative)
            { //negative ion mode only
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C3H6O5P-

                    var diagnosticMz1 = 152.99583;  // seek C3H6O5P-
                    var threshold1 = 10.0;
                    var diagnosticMz2 = theoreticalMz - 87.032029; // seek -C3H6NO2-H
                    var threshold2 = 5.0;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;

                    //
                    var candidates = new List<LipidMolecule>();
                    //var averageIntensity = 0.0;

                    //var molecule = getSingleacylchainMoleculeObjAsLevel2("LPS_d5", LbmClass.LPS_d5, totalCarbon, totalDoubleBond,
                    //averageIntensity);
                    //candidates.Add(molecule);
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPS_d5", LbmClass.LPS_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            else if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek Header loss (MG+ + chain Acyl) 
                    var threshold = 5.0;
                    var diagnosticMz = LipidMsmsCharacterizationUtility.acylCainMass(snCarbon, snDoubleBond) + (12 * 3 + MassDiffDictionary.Hydrogen2Mass * 5 + MassDiffDictionary.OxygenMass * 2) + MassDiffDictionary.ProtonMass;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPS_d5", LbmClass.LPS_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }
        public static LipidMolecule JudgeIfDagD5(IMSScanProperty msScanProp, double ms2Tolerance,
           double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PS 46:6, totalCarbon = 46 and totalDoubleBond = 6
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
           AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (totalCarbon > 52) return null; // currently, very large DAG is excluded.
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek -17.026549 (NH3)
                    var threshold = 1.0;
                    var diagnosticMz = theoreticalMz - 17.026549;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    if (sn2Double >= 7) return null;

                    var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - H2O + MassDiffDictionary.HydrogenMass;
                    var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - H2O + MassDiffDictionary.HydrogenMass;

                    //Console.WriteLine(sn1Carbon + ":" + sn1Double + "-" + sn2Carbon + ":" + sn2Double + 
                    //    " " + nl_SN1 + " " + nl_SN2);

                    var query = new List<SpectrumPeak>
                    {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 5 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 5 },
                            };
                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                    if (foundCount == 2)
                    {
                        var molecule = LipidMsmsCharacterizationUtility.getDiacylglycerolMoleculeObjAsLevel2("DG_d5", LbmClass.DG_d5, sn1Carbon, sn1Double,
                        sn2Carbon, sn2Double,
                        averageIntensity);
                        candidates.Add(molecule);
                    }
                    if (candidates == null || candidates.Count == 0)
                        return null;

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("DG_d5", LbmClass.DG_d5, string.Empty, theoreticalMz, adduct,

                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    /// DG[M+Na]+ is cannot determine acyl chain


                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("DG_d5", LbmClass.DG_d5, string.Empty, theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);

                }
            }
            return null;
        }
        public static LipidMolecule JudgeIfTriacylglycerolD5(IMSScanProperty msScanProp, double ms2Tolerance,
           double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PS 46:6, totalCarbon = 46 and totalDoubleBond = 6
           int sn1Carbon, int sn2Carbon, int sn3Carbon, int sn1Double, int sn2Double, int sn3Double,
           AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek -17.026549 (NH3)
                    var threshold = 1.0;
                    var diagnosticMz = theoreticalMz - 17.026549;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    if ((sn1Carbon == 18 && sn1Double == 5) || (sn2Carbon == 18 && sn2Double == 5) || (sn3Carbon == 18 && sn3Double == 5)) return null;

                    var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - H2O + MassDiffDictionary.HydrogenMass;
                    var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - H2O + MassDiffDictionary.HydrogenMass;
                    var nl_SN3 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn3Carbon, sn3Double) - H2O + MassDiffDictionary.HydrogenMass;
                    var query = new List<SpectrumPeak> {
                                        new SpectrumPeak() { Mass = nl_SN1, Intensity = 5 },
                                        new SpectrumPeak() { Mass = nl_SN2, Intensity = 5 },
                                        new SpectrumPeak() { Mass = nl_SN3, Intensity = 5 }
                                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 3)
                    { // these three chains must be observed.
                        var molecule = LipidMsmsCharacterizationUtility.getTriacylglycerolMoleculeObjAsLevel2("TG_d5", LbmClass.TG_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, sn3Carbon, sn3Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    if (candidates == null || candidates.Count == 0) return null;
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("TG_d5", LbmClass.TG_d5, string.Empty, theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 3);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {   //add MT
                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    var diagnosticMz = theoreticalMz; // - 22.9892207 + MassDiffDictionary.HydrogenMass; //if want to choose [M+H]+
                    var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - H2O + MassDiffDictionary.HydrogenMass;
                    var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - H2O + MassDiffDictionary.HydrogenMass;
                    var nl_SN3 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn3Carbon, sn3Double) - H2O + MassDiffDictionary.HydrogenMass;
                    var query = new List<SpectrumPeak> {
                                        new SpectrumPeak() {  Mass = nl_SN1, Intensity = 0.1 },
                                        new SpectrumPeak() { Mass = nl_SN2, Intensity = 0.1 },
                                        new SpectrumPeak() { Mass = nl_SN3, Intensity = 0.1 }
                                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount < 3)
                    {
                        var diagnosticMzH = theoreticalMz - 22.9892207 + MassDiffDictionary.HydrogenMass;
                        var nl_SN1_H = diagnosticMzH - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - H2O + MassDiffDictionary.HydrogenMass;
                        var nl_SN2_H = diagnosticMzH - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - H2O + MassDiffDictionary.HydrogenMass;
                        var nl_SN3_H = diagnosticMzH - LipidMsmsCharacterizationUtility.acylCainMass(sn3Carbon, sn3Double) - H2O + MassDiffDictionary.HydrogenMass;
                        var query2 = new List<SpectrumPeak> {
                                        new SpectrumPeak() { Mass = nl_SN1_H, Intensity = 0.1 },
                                        new SpectrumPeak() { Mass = nl_SN2_H, Intensity = 0.1 },
                                        new SpectrumPeak() { Mass = nl_SN3_H, Intensity = 0.1 }
                                        };

                        var foundCount2 = 0;
                        var averageIntensity2 = 0.0;
                        LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity2);


                        if (foundCount2 == 3)
                        {
                            var molecule = LipidMsmsCharacterizationUtility.getTriacylglycerolMoleculeObjAsLevel2("TG_d5", LbmClass.TG_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, sn3Carbon, sn3Double, averageIntensity2);
                            candidates.Add(molecule);
                        }
                    }
                    else
                    if (foundCount == 3)
                    { // these three chains must be observed.
                        var molecule = LipidMsmsCharacterizationUtility.getTriacylglycerolMoleculeObjAsLevel2("TG_d5", LbmClass.TG_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, sn3Carbon, sn3Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    if (candidates == null || candidates.Count == 0) return null;
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("TG_d5", LbmClass.TG_d5, string.Empty, theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 3);
                }
            }
            return null;
        }
        public static LipidMolecule JudgeIfSphingomyelinD9(IMSScanProperty msScanProp, double ms2Tolerance,
        double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
        int sphCarbon, int acylCarbon, int sphDouble, int acylDouble,
        AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    var C5H5D9NO4P = new[] {
                        MassDiffDictionary.CarbonMass * 5,
                        MassDiffDictionary.HydrogenMass * 5,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        MassDiffDictionary.PhosphorusMass,
                        MassDiffDictionary.Hydrogen2Mass * 9,
                    }.Sum();

                    // seek 184.07332 (C5H15NO4P) D9
                    var threshold = 10.0;
                    var diagnosticMz = C5H5D9NO4P + MassDiffDictionary.ProtonMass;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    if (sphCarbon <= 13) return null;
                    if (sphCarbon == 16 && sphDouble >= 3) return null;
                    if (acylCarbon < 8) return null;
                    var C5H14NO4P = 183.066047 + MassDiffDictionary.HydrogenMass * 9;
                    var C2H2N = 40.018724;

                    var diagnosChain1 = LipidMsmsCharacterizationUtility.acylCainMass(acylCarbon, acylDouble) + C2H2N + MassDiffDictionary.HydrogenMass + Proton;
                    var diagnosChain2 = diagnosChain1 + C5H5D9NO4P - MassDiffDictionary.HydrogenMass;
                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = diagnosChain1, Intensity = 0.5 },
                                new SpectrumPeak() { Mass = diagnosChain2, Intensity = 1.0 }
                            };


                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2)
                    { // the diagnostic acyl ion must be observed for level 2 annotation
                        var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("SM_d9", LbmClass.SM_d9, "d", sphCarbon, sphDouble,
                            acylCarbon, acylDouble, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("SM_d9", LbmClass.SM_d9, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);

                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek -59.0735 [M-C3H9N+Na]+
                    var threshold = 20.0;
                    var diagnosticMz = theoreticalMz - (59.0735 + MassDiffDictionary.HydrogenMass * 9);
                    // seek -183.06604 [M-C5H14NO4P+Na]+
                    var threshold2 = 30.0;
                    var diagnosticMz2 = theoreticalMz - (183.06604 + MassDiffDictionary.HydrogenMass * 9);
                    // seek -183.06604 [M-C5H16NO5P+H]+
                    var threshold3 = 1;
                    var diagnosticMz3 = theoreticalMz - (183.06604 + MassDiffDictionary.HydrogenMass * 9) - 39.993064;

                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold3);
                    //if (isClassIonFound == !true || isClassIon2Found == !true || isClassIon3Found == !true) return null;
                    if (isClassIonFound == false || isClassIon2Found == false) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("SM_d9", LbmClass.SM_d9, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }

            return null;
        }
        public static LipidMolecule JudgeIfCeramidensD7(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int sphCarbon, int acylCarbon, int sphDouble, int acylDouble,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                var adductform = adduct.AdductIonName;
                if (adductform == "[M+H]+" || adductform == "[M+H-H2O]+")
                {
                    // seek -H2O
                    var threshold = 5.0;
                    var diagnosticMz = adductform == "[M+H]+" ? theoreticalMz - H2O : theoreticalMz;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    if (acylDouble >= 7) return null;
                    var sph1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(acylCarbon, acylDouble) + MassDiffDictionary.HydrogenMass;
                    var sph2 = sph1 - H2O;
                    var sph3 = sph2 - 12; //[Sph-CH4O2+H]+
                    var acylamide = acylCarbon * 12 + (((2 * acylCarbon) - (2 * acylDouble) + 2) * MassDiffDictionary.HydrogenMass) + MassDiffDictionary.OxygenMass + MassDiffDictionary.NitrogenMass;

                    // must query
                    var queryMust = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = sph2, Intensity = 5 },
                            };
                    var foundCountMust = 0;
                    var averageIntensityMust = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, queryMust, ms2Tolerance, out foundCountMust, out averageIntensityMust);
                    if (foundCountMust == 0) return null;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = sph1, Intensity = 1 },
                                new SpectrumPeak() { Mass = sph3, Intensity = 1 },
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                    var foundCountThresh = acylCarbon < 12 ? 2 : 1; // to exclude strange annotation

                    if (foundCount >= foundCountThresh)
                    { // 
                        var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("Cer_d7", LbmClass.Cer_NS_d7, "d", sphCarbon, sphDouble,
                            acylCarbon, acylDouble, averageIntensity);
                        candidates.Add(molecule);
                    }
                    if (candidates.Count == 0) return null;

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("Cer_d7", LbmClass.Cer_NS_d7, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adductform == "[M+Na]+")
                {
                    // reject HexCer
                    var threshold = 1.0;
                    var diagnosticMz = theoreticalMz - 162.052833 - H2O;
                    if (LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold)) { return null; }
                    var candidates = new List<LipidMolecule>();
                    var sph1 = LipidMsmsCharacterizationUtility.SphingoChainMass(sphCarbon, sphDouble) + MassDiffDictionary.HydrogenMass - MassDiffDictionary.OxygenMass + (MassDiffDictionary.HydrogenMass * 7);
                    var sph3 = sph1 - H2O + Proton;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = sph3, Intensity = 1 },
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 1)
                    { // 
                        var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("Cer_d7", LbmClass.Cer_NS_d7, "d", sphCarbon, sphDouble,
                            acylCarbon, acylDouble, averageIntensity);
                        candidates.Add(molecule);
                    }
                    if (candidates.Count == 0) return null;
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("Cer_d7", LbmClass.Cer_NS_d7, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }

            return null;
        }
        public static LipidMolecule JudgeIfCholesterylEsterD7(IMSScanProperty msScanProp, double ms2Tolerance, float theoreticalMz,
        int totalCarbon, int totalDoubleBond, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek 369.3515778691 (C27H45+)+ MassDiffDictionary.HydrogenMass*7
                    var threshold = 20.0;
                    var diagnosticMz = 369.3515778691 + MassDiffDictionary.HydrogenMass * 7;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;
                    if (totalCarbon >= 41 && totalDoubleBond >= 4) return null;

                    var candidates = new List<LipidMolecule>();

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("CE_d7", LbmClass.CE_d7, string.Empty, theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek 369.3515778691 (C27H45+)+ MassDiffDictionary.HydrogenMass*7
                    var threshold = 10.0;
                    var diagnosticMz = 369.3515778691 + MassDiffDictionary.HydrogenMass * 7;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    // if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("CE_d7", LbmClass.CE_d7, string.Empty, theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 1);
                }

            }
            return null;
        }

    }
}
