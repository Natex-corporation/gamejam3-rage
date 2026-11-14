using System;
using System.Linq;
using UnityEngine;
using RageRefinement;

namespace RageBench
{
    public static class Runner
    {
        public static void StartBenchmark()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-rage-qa-profile");
            if (index < 0 || index + 1 >= args.Length) throw new InvalidOperationException("QA profile required.");
            string key = "RageRefinement.QA.Progression." + args[index + 1];
            string expected = PlayerPrefs.GetString(key + ".expected", "missing");
            string actual = Progression.Clay + "|" + string.Join(",", Enumerable.Range(0, 8).Select(i => Progression.Rank(i).ToString()).ToArray());
            bool passed = expected == actual;
            Debug.Log("SAVE PROBE " + (passed ? "PASS " : "FAIL ") + actual);
            Application.Quit(passed ? 0 : 1);
        }
    }
}
