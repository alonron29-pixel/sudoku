using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq; 
using Xunit;
using Logic;
using UI;

public class SudokuPerformanceTests
{
    private const string DataFolder = "TestsData";
    private const string DataFileName = "50k_9X9_Puzzles.txt";
    private const double SecondsTimeout = 1.0;
    private const int NumberOfPuzzlesToSolve = 10000;

    public static IEnumerable<object[]> GetTestData()
    {
        // Combines your constants into a single path
        string path = Path.Combine(DataFolder, DataFileName);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"File not found at {path}. Check 'Copy to Output' settings.");
        }

        // Streams the file and stops after NumberOfPuzzlesToSolve lines
        var lines = File.ReadLines(path).Take(NumberOfPuzzlesToSolve);

        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                yield return new object[] { line.Trim(), SecondsTimeout }; // using yield to read the file line by line 
            }
        }
    }

    [Theory] // Marks the method as a parameter-driven test that runs multiple times with different data.
    [MemberData(nameof(GetTestData))] // Tells the test where to find its input data by calling the 'GetTestData' method.
    [Trait("Category", "Performance")] // Assigns a 'Performance' label to this test so I can use a filter in the Test Explorer.
    public void SolveAndMeasure(string boardString, double limit)
    {
        Board board = SudokuEngine.HandleNewBoard(boardString);
        Solver solver = new Solver(board);

        var sw = Stopwatch.StartNew();
        bool solved = solver.Solve();
        sw.Stop();

        Assert.True(solved, $"Failed to solve: {boardString}");
        Assert.True(sw.Elapsed.TotalSeconds < limit,
            $"Board took {sw.Elapsed.TotalSeconds:F4}s which exceeds limit of {limit}s");
    }
}