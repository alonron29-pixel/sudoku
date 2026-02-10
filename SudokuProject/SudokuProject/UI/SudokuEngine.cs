using Exceprions;
using Logic;
using System;
using System.Diagnostics;
using Validation;

namespace UI
{
    public class SudokuEngine
    {
        private static string WelcomeMsg = "-- Welcome to the fastest solver in the Middle East! --";
        private static string ExitString = "exit";

        /// <summary>
        /// Starts the main application loop, handling the continuous interaction with the user.
        /// timing the execution, and handling all potential exceptions to keep the program running.
        /// </summary>
        public void run()
        {
            Console.WriteLine(WelcomeMsg);
            Console.WriteLine($"Enter board string or type \"{ExitString}\" to exit:");
            string boardString = Console.ReadLine();

            while (boardString != ExitString)
            {
                try
                {
                    Board board = HandleNewBoard(boardString);

                    Console.WriteLine("Initial board:");
                    PrintBoard(board);

                    Solver solver = new Solver(board);

                    Stopwatch sw = new Stopwatch(); // Create Timer
                    sw.Start(); // Start timer

                    if (solver.Solve())
                    {
                        sw.Stop(); // Stop timer

                        // print solved board
                        Console.WriteLine("\nSolved board:");
                        PrintBoard(board);

                        // print time
                        Console.WriteLine($"\nTime taken: {sw.Elapsed.TotalSeconds:F4} seconds");
                    }
                    else
                    {
                        // Throw exception if solver fails
                        throw new UnsolvableBoardException();
                    }
                }
                catch (InvalidBoardDimensionsException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (InvalidCharacterException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (UnsolvableBoardException ex)
                {
                    Console.WriteLine(ex.Message);
                }

                catch (TimeoutException ex)
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

        /// <summary>
        /// Processes a new board string by validating it and determining the appropriate 
        /// sub-grid (box) dimensions. If the board is non-standard, it prompts the user for manual input.
        /// </summary>
        /// <param name="boardString">The raw string representation of the Sudoku board.</param>
        /// <returns>A fully initialized <see cref="Board"/> object ready for solving.</returns>
        public static Board HandleNewBoard(string boardString)
        {
            BoardValidator.ValidateBoardDimensions(boardString);

            int size = (int)Math.Sqrt(boardString.Length);
            int boxRows, boxCols;

            if (BoardValidator.IsStandardLayout(size))
            {
                boxRows = boxCols = (int)Math.Sqrt(size);
            }

            else
            {
                Console.WriteLine("Non-standard board detected.");
                Console.Write("Enter rectangle hight: ");
                boxRows = int.Parse(Console.ReadLine());
                Console.Write("Enter rectangle width: ");
                boxCols = int.Parse(Console.ReadLine());
            }

            return new Board(boardString, size, boxRows, boxCols);
        }

        /// <summary>
        /// Renders the current state of the Sudoku board to the console with grid formatting.
        /// </summary>
        private void PrintBoard(Board board)
        {
            string dashes = new string('-', 2 * (board.Size + board.BoxRows) - 1);

            for (int row = 0; row < board.Size; row++)
            {
                if (row % board.BoxRows == 0)
                {
                    Console.WriteLine($" |{dashes}|");
                }

                for (int col = 0; col < board.Size; col++)
                {
                    if (col % board.BoxCols == 0)
                    {
                        Console.Write(" |");
                    }

                    int cellValue = board.Cells[row * board.Size + col].Value;
                    Console.Write($" {board.ValueToChar[cellValue]}");
                }
                Console.WriteLine(" |");
            }
            Console.WriteLine($" |{dashes}|");
        }
    }
}