﻿using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public interface IOxidized
    {
        int Count { get; }
        int DecidedCount { get; }
        int UnDecidedCount { get; }

        ReadOnlyCollection<int> Oxidises { get; }
    }

    public class Oxidized : IOxidized
    {
        public Oxidized(int count, params int[] oxidises) {
            Count = count;
            Oxidises = new ReadOnlyCollection<int>(oxidises);
        }

        public int Count { get; }

        public int DecidedCount => Oxidises.Count;

        public int UnDecidedCount => Count - DecidedCount;

        public ReadOnlyCollection<int> Oxidises { get; }

        public override string ToString() {
            if (Count == 0) {
                return "";
            }
            else if (UnDecidedCount == 0) {
                return ";" + string.Join(",", Oxidises.Select(o => o.ToString() + "OH"));
            }
            else if (Count == 1) {
                return ";O";
            }
            else {
                return $";O{Count}";
            }
        }

        public static Oxidized CreateFromPosition(params int[] oxidises) {
            return new Oxidized(oxidises.Length, oxidises);
        }
    }
}