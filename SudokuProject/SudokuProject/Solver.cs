using System;
using System.Security.Cryptography;

public class Solver
{
    private readonly Board _board;
    private readonly int[] _workStack;

    private readonly UndoStep[] _undoStack;
    private int _undoPtr = 0;

    private const short FullBoardFlag = -1;

    public Solver(Board board)
    {
        _board = board;

        // Pre-allocated to the maximum possible number of dependency updates to ensure safety and speed without resizing.
        _workStack = new int[board.TotalCells * board.NeighborsPerCell];

        // Each cell affects a maximum of (3 * Size - CONST) neighbors.
        // The undoStack size is pre-allocated to (TotalCells * Size) to cover the 
        // worst-case scenario where every candidate removal is tracked during propagation.
        _undoStack = new UndoStep[board.TotalCells * board.Size];

        _undoPtr = 0;
    }
}