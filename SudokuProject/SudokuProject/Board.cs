using System;

public class Board
{
    public int Size { get; private set; }
    
    public int TotalCells { get; private set; } 

    // A single continuous block of memory containing all neighbors indexes for all cells.
    public int[] AllNeighbors { get; private set; }

    // Starting index in _allNeighbors for each cell.
    public int[] NeighborOffsets { get; private set; }

    public int NeighborsPerCell { get; private set; }
    
    public Cell[] Cells { get; private set; }

    private int _boxRows;
    private int _boxCols;
    private long _fullMask;

    private int[] _workStack;
    private int _workPtr = 0;

    private const char EmptyCellCharValue = '0';
    private readonly string _symbols = "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    public Dictionary<char, int> CharToVal { get; private set; }
    public Dictionary<int, char> ValToChar { get; private set; }

    public Board(string boardString)
    {
        InitializeDimensions(boardString);
        InitializeDictionaries();
        BuildNeighborsAndInitCells(boardString);
    }
	
	/// <summary>
    /// Orchestrates the initialization of board dimensions, resource allocation, and input validation.
    /// </summary>
    /// <param name="boardString">A string representation of the initial board state.</param>
    private void InitializeDimensions(string boardString)
    {
        ValidateAndSetSize(boardString);
        SetBoxDimensions();
        AllocateArrays();
    }

    /// <summary>
    /// Validates if the board string length is a perfect square and calculates the side length (Size).
    /// </summary>
    /// <param name="boardString">The raw input string to validate.</param>
    /// <exception cref="InvalidBoardDimensions">Thrown when the string is null/empty or not a perfect square.</exception>
    private void ValidateAndSetSize(string boardString)
    {
        if (string.IsNullOrEmpty(boardString))
        {
            throw new InvalidBoardDimensions(InvalidBoardDimensions.EmptyStringMsg);
        }

        TotalCells = boardString.Length;
        Size = (int)Math.Sqrt(TotalCells);

        if (Size * Size != TotalCells)
        {
            throw new InvalidBoardDimensions(InvalidBoardDimensions.InvalidDimensionsMsg);
        }
    }

    /// <summary>
    /// Determines the sub-grid (Box) dimensions, creates the bitmask, and calculates neighbors per cell.
    /// </summary>
    private void SetBoxDimensions()
    {
        _boxRows = (int)Math.Sqrt(Size);

        // Standard case: Board is a perfect square of squares (e.g., 9x9, 16x16)
        if (_boxRows * _boxRows == Size)
        {
            _boxCols = _boxRows;
        }
        else
        {
            // Rectangular sub-grids: Requires manual input for row/column distribution
            Console.Write("Enter number of rows per rectangle: ");
            _boxRows = int.Parse(Console.ReadLine());
            Console.Write("Enter number of columns per rectangle: ");
            _boxCols = int.Parse(Console.ReadLine());

            if (_boxRows * _boxCols != Size)
            {
                throw new InvalidBoardDimensions(InvalidBoardDimensions.InvalidRowColSize);
            }
        }

        // Generate a full bitmask representing all possible candidates for the current board size
        _fullMask = (1L << Size) - 1;
        
        // Calculate the number of unique neighbors for any cell (Row + Column + Box - self)
        NeighborsPerCell = (Size * 3) - _boxRows - _boxCols - 1;
    }

    /// <summary>
    /// Allocates memory for the board's core data structures, including cells, neighbor arrays, and work stacks.
    /// </summary>
    private void AllocateArrays()
    {
        Cells = new Cell[TotalCells];
        AllNeighbors = new int[TotalCells * NeighborsPerCell];
        NeighborOffsets = new int[TotalCells];
        _workStack = new int[TotalCells];
    }
}