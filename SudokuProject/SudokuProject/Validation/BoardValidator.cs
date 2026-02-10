using Exceprions;
using System;

namespace Validation
{
    public static class BoardValidator
    {
        /// <summary>
        /// Validates the input string length and calculates the side length of the board.
        /// Throws InvalidBoardDimensionsException if the string is empty or not a perfect square.
        /// </summary>
        public static void ValidateBoardDimensions(string boardString)
        {
            if (string.IsNullOrEmpty(boardString))
            {
                throw new InvalidBoardDimensionsException(InvalidBoardDimensionsException.EmptyStringMsg);
            }

            if (!BoardValidator.IsPerfectSquare(boardString.Length))
            {
                throw new InvalidBoardDimensionsException(InvalidBoardDimensionsException.InvalidDimensionsMsg);
            }
        }

        /// <summary>
        /// Determines if the board size allows for a standard Sudoku layout 
        /// where the sub-grids (boxes) are perfect squares.
        /// </summary>
        /// <param name="size">The side length of the board.</param>
        /// <returns>True if the square root of the size is an integer; otherwise, false.</returns>
        public static bool IsStandardLayout(int size)
        {
            double root = Math.Sqrt(size);
            return root % 1 == 0; // root is an intager or not
        }

        /// <summary>
        /// Validates if the total number of cells in the input string forms a perfect square,
        /// which is a prerequisite for any valid N x N Sudoku board.
        /// </summary>
        /// <param name="length">The total length of the input string.</param>
        /// <returns>True if the length is a perfect square; otherwise, false.</returns>
        private static bool IsPerfectSquare(int length)
        {
            int size = (int)Math.Sqrt(length);
            return size * size == length;
        }
    }

}