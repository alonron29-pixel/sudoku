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
	
	/// <summary>
    /// Executes the full solving process by performing an initial constraint propagation 
    /// followed by a recursive backtracking search.
    /// </summary>
    public bool Solve()
    {
        _board.InitialPropagation();
        return Backtrack();
    }

    /// <summary>
    /// Converts a single-bit bitmask into its corresponding Sudoku integer value
    /// </summary>
    public static int BitmaskToValue(long bitmask)
    {
        int count = 0;
        while (bitmask > 0)
        {
            bitmask >>= 1;
            count++;
        }
        return count;
    }

    // <summary>
    /// The core recursive algorithm that explores potential cell values. 
    /// Uses a snapshot-based undo system to revert changes during failed branches.
    /// </summary>
    private bool Backtrack()
    {
        for (int i = 0; i < _board.TotalCells; i++)
        {
            if (_board.Cells[i].Value == Cell.EmptyCellValue && _board.Cells[i].CandidatesCount == 0)
                return false;
        }

        int cellIndex = FindBestCellIndex();
        
        if (cellIndex == FullBoardFlag)
        {
            return true;
        }

        int snapshot = _undoPtr;
  
        ref Cell currentCell = ref _board.Cells[cellIndex];
        for (int val = 1; val <= _board.Size; val++)
        {
            // Create a bitmask for the current value 
            long valBit = 1L << (val - 1);
            // Check if the current value is a candidate for this cell
            if ((currentCell.CandidatesMask & valBit) != 0)
            {
                currentCell.Value = val;
                _undoStack[_undoPtr++] = new UndoStep(cellIndex, UndoStep.ValueAssignmentFlag);
              
                if (TryValue(cellIndex))
                {
                    if (Backtrack())
                    {
                        return true; // got to a solution
                    }
                }
                
                Undo(snapshot);
            }
        }
    
        return false; //no option is valid, backtrack to previous call
    }
}