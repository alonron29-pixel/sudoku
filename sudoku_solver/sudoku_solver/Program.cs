using System;
using System.Diagnostics;

namespace sudoku_solver
{
    public class Program
    {
        private static string WelcomeMsg = "-- Welcome to the fastest solver in the Middle East! --";
        private static string ExitString = "exit";
        public static void Main(string[] args)
        {
            Console.WriteLine(WelcomeMsg);
            Console.WriteLine($"Enter board string or type \"{ExitString}\" to exit:");
            string boardString = Console.ReadLine();
            
            while (boardString != ExitString)
            {
                try
                {
                    // Attempt to create the board
                    Board board = new Board(boardString);

                    Console.WriteLine("Initial board:");
                    board.Print();

                    // If board creation succeeded, try to solve it
                    Solver solver = new Solver(board);

                    Stopwatch sw = new Stopwatch(); // Create Timer
                    sw.Start(); // Start timer

                    if (solver.Solve())
                    {
                        sw.Stop(); // Stop timer

                        // print solved board
                        Console.WriteLine("\nSolved board:");
                        board.Print();

                        // print time
                        Console.WriteLine($"\nTime taken: {sw.Elapsed.TotalSeconds:F4} seconds");
                    }
                    else
                    {
                        // Throw exception if solver fails
                        throw new UnsolvableBoard();
                    }
                }
                catch (InvalidBoardDimensions ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (InvalidCharacter ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (UnsolvableBoard ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                Console.WriteLine($"\nEnter board string or type \"{ExitString}\" to exit:");
                boardString = Console.ReadLine();
            }
        }
    }
}
