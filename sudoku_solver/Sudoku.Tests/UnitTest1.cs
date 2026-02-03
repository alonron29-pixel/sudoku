using sudoku_solver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace sudoku_tests
{
    // test the solver with almost 50000 boards strings from testBoards.txt
    public class SudokuPerformanceTests
    {
        // This method reads the file and returns the data to the test
        public static IEnumerable<object[]> GetBoardsFromFile()
        {
            string filePath = "testBoards.txt";

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Could not find {filePath}. Did you set 'Copy to Output Directory'?");
            }

            var lines = File.ReadAllLines(filePath);
            var data = new List<object[]>();

            foreach (var line in lines)
            {
                // Skip empty lines if any
                if (!string.IsNullOrWhiteSpace(line))
                {
                    data.Add(new object[] { line.Trim() });
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(GetBoardsFromFile))]
        public void Solve_ShouldFinishUnderOneSecond(string boardString)
        {
            Board board;
            try
            {
                board = new Board(boardString);
            }
            catch (Exception ex)
            {
                // Fail the test if the board string from the file is invalid
                Assert.Fail($"Board creation failed for string: {boardString}. Error: {ex.Message}");
                return;
            }

            Solver solver = new Solver(board);
            Stopwatch sw = new Stopwatch();

            sw.Start(); // start timer
            bool result = solver.Solve();
            sw.Stop(); // stop timer

            // Check if it was actually solved
            Assert.True(result, $"The solver failed to find a solution for: {boardString}");

            // Check the time constraint (1 second)
            Assert.True(sw.Elapsed.TotalSeconds < 1.0,
                $"Performance failed! Took {sw.Elapsed.TotalSeconds:F4}s (Limit: 1.0s) for board: {boardString}");
        }
    }
}