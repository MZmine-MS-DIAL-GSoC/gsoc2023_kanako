﻿using CompMs.Common.Extension;
using CompMs.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class Omega3nChainGenerator : IChainGenerator
    {
        public bool CarbonIsValid(int carbon) {
            return true;
        }

        public bool DoubleBondIsValid(int carbon, int doubleBond) {
            return carbon >= doubleBond * 3 + 3 && doubleBond >= 0;
        }

        private static readonly int eariestPositionOfOx = 2;
        public IEnumerable<IChain> Generate(AcylChain chain) {
            var bs = EnumerateDoubleBond(chain.CarbonCount, chain.DoubleBond);
            var os = EnumerateOxidized(chain.CarbonCount, chain.Oxidized, eariestPositionOfOx, chain.CarbonCount - 1).ToArray();
            return bs.SelectMany(_ => os, (b, o) => new AcylChain(chain.CarbonCount, b, o));
        }

        public IEnumerable<IChain> Generate(AlkylChain chain) {
            IEnumerable<IDoubleBond> bs;
            if (chain.IsPlasmalogen) {
                bs = EnumerateDoubleBondInPlasmalogen(chain.CarbonCount, chain.DoubleBond);
            }
            else {
                bs = EnumerateDoubleBondInEther(chain.CarbonCount, chain.DoubleBond);
            }
            var os = EnumerateOxidized(chain.CarbonCount, chain.Oxidized, eariestPositionOfOx, chain.CarbonCount - 1).ToArray();
            return bs.SelectMany(_ => os, (b, o) => new AlkylChain(chain.CarbonCount, b, o));
        }

        private static readonly int eariestPositionOfOxInSphingosine = 4;
        public IEnumerable<IChain> Generate(SphingoChain chain) {
            var bs = EnumerateDoubleBondInSphingosine(chain.CarbonCount, chain.DoubleBond);
            var os = EnumerateOxidized(chain.CarbonCount, chain.Oxidized, eariestPositionOfOxInSphingosine, chain.CarbonCount - 1).ToArray();
            return bs.SelectMany(_ => os, (b, o) => new SphingoChain(chain.CarbonCount, b, o));
        }

        private IEnumerable<IDoubleBond> EnumerateDoubleBond(int carbon, IDoubleBond doubleBond) {
            if (doubleBond.UnDecidedCount == 0) {
                return IEnumerableExtension.Return(doubleBond);
            }
            if (!DoubleBondIsValid(carbon, doubleBond.Count)) {
                return Enumerable.Empty<IDoubleBond>();
            }
            var result = InnerEnumerateDoubleBond(carbon, doubleBond);
            if (carbon == 14 && doubleBond.Count == 1 && doubleBond.UnDecidedCount == 1) {
                result = result.Prepend(new DoubleBond(DoubleBondInfo.Z(9)));
            }
            if (carbon == 16 && doubleBond.Count == 1 && doubleBond.UnDecidedCount == 1) {
                result = result.Prepend(new DoubleBond(DoubleBondInfo.Z(9)));
            }
            return result;
        }

        private IEnumerable<IDoubleBond> InnerEnumerateDoubleBond(int carbon, IDoubleBond doubleBond) {
            var set = new HashSet<int>(doubleBond.Bonds.Select(bond => bond.Position));
            for (var i = 0; i < doubleBond.UnDecidedCount; i++) {
                set.Add(carbon - i * 3);
            }
            return GenerateDoubleBonds(set, SetToBitArray(carbon, doubleBond), carbon - doubleBond.UnDecidedCount * 3, carbon);
        }

        private IEnumerable<IDoubleBond> EnumerateDoubleBondInEther(int carbon, IDoubleBond doubleBond) {
            if (doubleBond.UnDecidedCount == 0) {
                return IEnumerableExtension.Return(doubleBond);
            }
            if (!DoubleBondIsValid(carbon, doubleBond.Count - 1)) {
                return Enumerable.Empty<IDoubleBond>();
            }
            return InnerEnumerateDoubleBondInEther(carbon, doubleBond);
        }

        private IEnumerable<IDoubleBond> InnerEnumerateDoubleBondInEther(int carbon, IDoubleBond doubleBond) {
            var set = new HashSet<int>(doubleBond.Bonds.Select(bond => bond.Position));
            for (var i = 0; i < doubleBond.UnDecidedCount; i++) {
                set.Add(carbon - i * 3);
            }
            var sup = SetToBitArray(carbon, doubleBond);
            IEnumerable<IDoubleBond> result = GenerateDoubleBonds(set, sup, carbon - doubleBond.UnDecidedCount * 3, carbon).ToArray();
            if (doubleBond.UnDecidedCount >= 1) {
                sup.Add(1);
                set = new HashSet<int>(doubleBond.Bonds.Select(bond => bond.Position));
                set.Add(1);
                for (var i = 0; i < doubleBond.UnDecidedCount - 1; i++) {
                    set.Add(carbon - i * 3);
                }
                result = result.Concat(GenerateDoubleBonds(set, sup, carbon - (doubleBond.UnDecidedCount - 1) * 3, carbon));
            }
            return result;
        }

        private IEnumerable<IDoubleBond> EnumerateDoubleBondInPlasmalogen(int carbon, IDoubleBond doubleBond) {
            if (doubleBond.UnDecidedCount == 0) {
                return IEnumerableExtension.Return(doubleBond);
            }
            if (!DoubleBondIsValid(carbon, doubleBond.Count - 1)) {
                return Enumerable.Empty<IDoubleBond>();
            }
            return InnerEnumerateDoubleBondInPlasmalogen(carbon, doubleBond);
        }

        private IEnumerable<IDoubleBond> InnerEnumerateDoubleBondInPlasmalogen(int carbon, IDoubleBond doubleBond) {
            var set = new HashSet<int>(doubleBond.Bonds.Select(bond => bond.Position));
            for (var i = 0; i < doubleBond.UnDecidedCount; i++) {
                set.Add(carbon - i * 3);
            }
            return GenerateDoubleBonds(set, SetToBitArray(carbon, doubleBond), carbon - doubleBond.UnDecidedCount * 3, carbon);
        }

        private IEnumerable<IDoubleBond> EnumerateDoubleBondInSphingosine(int carbon, IDoubleBond doubleBond) {
            if (doubleBond.UnDecidedCount == 0) {
                return new[]{ doubleBond, };
            }
            return GenerateDoubleBondsForSphingosine(doubleBond, carbon);
        }

        private IEnumerable<IDoubleBond> GenerateDoubleBonds(HashSet<int> set, HashSet<int> sup, int nextHead, int prevTail) {
            if (nextHead == prevTail) {
                if (set.IsSupersetOf(sup)) {
                    yield return BitArrayToBond(set);
                }
                yield break;
            }
            while (nextHead > 3) {
                if (!set.Contains(nextHead)) {
                    set.Add(nextHead);
                    set.Remove(prevTail);
                    if (set.IsSupersetOf(sup)) {
                        yield return BitArrayToBond(set);
                    }
                    prevTail -= 3;
                    if (sup.Contains(prevTail)) {
                        break;
                    }
                }
                nextHead -= 3;
            }
        }

        private IEnumerable<IDoubleBond> GenerateDoubleBondsForSphingosine(IDoubleBond doubleBond, int length) {
            var sets = new HashSet<int>(doubleBond.Bonds.Select(b => b.Position));

            var blank = 3;
            if (length > 4 && sets.Contains(4)) --blank;
            if (length > 8 && sets.Contains(8)) --blank;
            if (length > 14 && sets.Contains(14)) --blank;

            IEnumerable<IDoubleBond> bs = new[] { doubleBond };
            var next = bs;
            var result = Enumerable.Empty<IDoubleBond>();
            if (length > 4 && !sets.Contains(4)) {
                var tmp = next.Select(b => b.Decide(DoubleBondInfo.Create(4))).ToArray();
                result = result.Concat(tmp.Where(b => b.UnDecidedCount == 0));
                next = next.Concat(tmp.Where(b => b.UnDecidedCount > 0));
                sets.Add(4);
            }
            if (length > 8 && !sets.Contains(8)) {
                var tmp = next.Select(b => b.Decide(DoubleBondInfo.Create(8))).ToArray();
                result = result.Concat(tmp.Where(b => b.UnDecidedCount == 0));
                next = next.Concat(tmp.Where(b => b.UnDecidedCount > 0));
                sets.Add(8);
            }
            if (length > 14 && !sets.Contains(14)) {
                var tmp = next.Select(b => b.Decide(DoubleBondInfo.Create(14))).ToArray();
                result = result.Concat(tmp.Where(b => b.UnDecidedCount == 0));
                next = next.Concat(tmp.Where(b => b.UnDecidedCount > 0));
                sets.Add(14);
            }

            if (blank <= doubleBond.UnDecidedCount) {
                next = next.Where(b => b.UnDecidedCount > 0 && new[] { 4, 8, 14, }.All(i => b.Bonds.Select(bd => bd.Position).Contains(i)));
                for (int i = 6; i < length; i += 2) {
                    if (!sets.Contains(i)) {
                        sets.Add(i);
                        var tmp = next.Select(b => b.Decide(DoubleBondInfo.Create(i))).ToArray();
                        result = result.Concat(tmp.Where(b => b.UnDecidedCount == 0));
                        next = next.Concat(tmp.Where(b => b.UnDecidedCount > 0));
                    }
                }
            }
            return result;
        }

        private HashSet<int> SetToBitArray(int length, IDoubleBond doubleBond) {
            if (doubleBond.DecidedCount == 0) {
                return new HashSet<int>();
            }
            var result = new HashSet<int>();
            foreach (var bond in doubleBond.Bonds) {
                result.Add(bond.Position);
            }
            return result;
        }

        private IDoubleBond BitArrayToBond(HashSet<int> arr) {
            var bonds = new List<IDoubleBondInfo>();
            foreach (var v in arr.OrderBy(v => v)){
                bonds.Add(DoubleBondInfo.Create(v));
            }
            return new DoubleBond(bonds.Count, bonds);
        }

        private IEnumerable<IOxidized> EnumerateOxidized(int carbon, IOxidized oxidized, int begin, int end) {
            if (oxidized.UnDecidedCount == 0) {
                return IEnumerableExtension.Return(oxidized);
            }

            IEnumerable<IOxidized> rec(int i, List<int> infos) {
                if (infos.Count == oxidized.UnDecidedCount) {
                    yield return Oxidized.CreateFromPosition(oxidized.Oxidises.Concat(infos).OrderBy(p => p).ToArray());
                    yield break;
                }
                for (var j = i; j < carbon + 1; j++){ 
                    if (oxidized.Oxidises.Contains(j)) {
                        continue;
                    }
                    infos.Add(j);
                    foreach (var res in rec(j + 1, infos)) {
                        yield return res;
                    }
                    infos.RemoveAt(infos.Count - 1);
                }
            }

            return rec(begin, new List<int>(oxidized.UnDecidedCount));
        }
    }
}
