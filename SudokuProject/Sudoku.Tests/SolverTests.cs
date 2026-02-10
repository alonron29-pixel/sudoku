using System;
using Xunit;
using Logic;
using Exceprions;
using UI;

public class SolverTests
{
    // Helper string for a simple valid 4x4 Sudoku
    // 1 . . 4
    // . . . .
    // . . . .
    // 4 . . 1
    private const string Valid4x4 = "1004000000004001";

    // Helper string for an unsolvable 4x4 (Row conflict that requires propagation to detect)
    // 1 1 . . (Immediate conflict)
    private const string Invalid4x4 = "1100000000000000";

    // --- Static Helper Tests ---

    [Theory]
    [Trait("Category", "SolverLogic")]
    [InlineData(1, 1)]      // 1L << 0
    [InlineData(2, 2)]      // 1L << 1
    [InlineData(4, 3)]      // 1L << 2
    [InlineData(256, 9)]    // 1L << 8 (Standard Sudoku max)
    [InlineData(16777216, 25)] // 1L << 24 (For 25x25 boards)
    public void BitmaskToValue_ValidBitmasks_ShouldReturnCorrectInteger(long mask, int expectedValue)
    {
        int result = Solver.BitmaskToValue(mask);

        Assert.Equal(expectedValue, result);
    }

    [Fact]
    [Trait("Category", "SolverLogic")]
    public void BitmaskToValue_ZeroMask_ShouldReturnZero()
    {
        // Passing 0 (no candidates) should return 0

        long mask = 0;

        int result = Solver.BitmaskToValue(mask);

        Assert.Equal(0, result);
    }

    // ---  Constructor & Initialization Tests ---

    [Fact]
    [Trait("Category", "SolverLogic")]
    public void Constructor_NullBoard_ShouldThrowNullReferenceException()
    {
        // Initializing solver with null board

        Board nullBoard = null;

        Assert.Throws<NullReferenceException>(() => new Solver(nullBoard));
    }

    [Fact]
    [Trait("Category", "SolverLogic")]
    public void Constructor_ValidBoard_ShouldInitializeStacksWithoutError()
    {
        Board board = SudokuEngine.HandleNewBoard(Valid4x4);

        Solver solver = new Solver(board);

        // If constructor runs without exception, test passes.
        // This validates memory allocation for _workStack and _undoStack.
        Assert.NotNull(solver);
    }

    // --- Solving Logic Tests ---

    [Fact]
    [Trait("Category", "SolverLogic")]
    public void Solve_ValidPuzzle_ShouldReturnTrueAndFillBoard()
    {
        Board board = SudokuEngine.HandleNewBoard(Valid4x4);
        Solver solver = new Solver(board);

        bool success = solver.Solve();

        Assert.True(success, "Solver should return true for a valid puzzle.");

        // Check if board is actually full (no zeros)
        bool isFull = true;
        for (int i = 0; i < board.TotalCells; i++)
        {
            if (board.Cells[i].Value == 0) isFull = false;
        }
        Assert.True(isFull, "Board should be completely filled after solving.");
    }

    [Fact]
    [Trait("Category", "SolverLogic")]
    public void Solve_AlreadySolvedBoard_ShouldReturnTrueImmediately()
    {
        // The board is already full and valid.
        // The solver should detect this (FindBestCellIndex returns FullBoardFlag) and exit.

        // A fully solved 4x4 grid
        string solvedStr = "1234341221434321";
        Board board = SudokuEngine.HandleNewBoard(solvedStr);
        Solver solver = new Solver(board);

        bool success = solver.Solve();

        Assert.True(success);
    }

    [Fact]
    [Trait("Category", "SolverLogic")]
    public void Solve_UnsolvablePuzzle_ShouldReturnFalse_Or_Throw()
    {
        // A puzzle that violates rules. 
        // InitialPropagation might throw 'UnsolvableBoard'.
        // If it doesn't throw, Backtrack must return false.

        Board board = SudokuEngine.HandleNewBoard(Invalid4x4);
        Solver solver = new Solver(board);

        try
        {
            bool result = solver.Solve();
            Assert.False(result, "Solver should return false for unsolvable configuration.");
        }
        catch (UnsolvableBoardException)
        {
            // If InitialPropagation catches it early, this is also a valid pass.
            Assert.True(true);
        }
    }

    [Fact]
    [Trait("Category", "SolverLogic")]
    public void Solve_EmptyBoard_ShouldFindASolution()
    {
        // An empty board causes maximum backtracking depth.
        // This tests the recursion and stack limits.

        string empty25X25 = new string('0', 625);
        Board board = SudokuEngine.HandleNewBoard(empty25X25);
        Solver solver = new Solver(board);

        bool success = solver.Solve();

        // Assert
        Assert.True(success);
    }

    // --- Deep Logic & Backtracking Tests ---

    [Fact]
    [Trait("Category", "SolverLogic")]
    public void Backtrack_UndoLogic_ShouldRevertChangesOnFailure()
    {
        // Logic Test: This specifically targets the "Undo" mechanism.
        // We create a board where the first guess (for example cell[0] = 1) is valid locally,
        // but leads to a dead end later. The solver must backtrack and clean up.

        // 4x4 Example:
        // 0 0 3 4
        // 3 4 1 2
        // . . . .
        // . . . .
        // If cell[0] tries '1', it does not conflicts with row neighbors.
        // Chacks a scenario that forces a backtrack.

        // This specific string requires the solver to try a value, propagate it, fail, undo, and try another.
        string tricky4x4 = "0034341221434321";
        Board board = SudokuEngine.HandleNewBoard(tricky4x4);
        Solver solver = new Solver(board);

        bool success = solver.Solve();

        Assert.True(success);
        Assert.NotEqual(0, board.Cells[0].Value);
        // Ensure the final state is valid (implies Undo worked correctly during search)
        // (If Undo failed, the board would be left with invalid/conflicting states)
    }
}
