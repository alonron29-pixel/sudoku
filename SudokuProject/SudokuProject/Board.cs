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
    /// Renders the current state of the Sudoku board to the console with grid formatting.
    /// </summary>
    public void Print()
    {
        // Calculate the width of the horizontal divider based on board size and sub-grids
        string dashes = new string('-', 2 * (Size + _boxRows) - 1);

        for (int row = 0; row < Size; row++)
        {
            // Print horizontal divider at the start of each sub-grid row
            if (row % _boxRows == 0)
            {
                Console.WriteLine($" |{dashes}|");
            }

            for (int col = 0; col < Size; col++)
            {
                // Print vertical divider at the start of each sub-grid column
                if (col % _boxCols == 0)
                {
                    Console.Write(" |");
                }

                // Retrieve the character representation of the cell value
                int cellValue = Cells[row * Size + col].Value;
                Console.Write($" {ValueToChar[cellValue]}");
            }

            // End of row vertical border
            Console.WriteLine(" |");
        }

        // Final bottom border
        Console.WriteLine($" |{dashes}|");
    }
	
	/// <summary>
    /// Propagates constraints from fixed cells to narrow down candidates and solve "Naked Singles."
    /// Uses a work stack to recursively process cells that become solved during propagation.
    /// </summary>
    /// <exception cref="UnsolvableBoard">Thrown if a conflict is detected or a cell runs out of candidates.</exception>
    public void InitialPropagation()
    {
        while (_workPtr >= 0)
        {
            int currentCellIndex = _workStack[_workPtr--];
            int currentCellValue = Cells[currentCellIndex].Value;
            long currentCellBitmask = 1L << (currentCellValue - 1);
            int offset = NeighborOffsets[currentCellIndex];

            for (int i = 0; i < NeighborsPerCell; i++)
            {
                int neighborIndex = AllNeighbors[offset + i];
                ref Cell neighbor = ref Cells[neighborIndex];

                // Case 1: The neighbor cell is empty
                if (neighbor.Value == Cell.EmptyCellValue)
                {
                    // If the current value is still a candidate in the neighbor cell
                    if ((neighbor.CandidatesMask & currentCellBitmask) != 0)
                    {
                        // If this was the last possible candidate, the board is unsolvable
                        if (neighbor.CandidatesCount == 1)
                        {
                            throw new UnsolvableBoard();
                        }

                        neighbor.RemoveCandidate(currentCellBitmask);

                        // If exactly one candidate remains, we found a "Naked Single"
                        if (neighbor.CandidatesCount == 1)
                        {
                            neighbor.Value = Solver.BitmaskToValue(neighbor.CandidatesMask);
                            _workStack[++_workPtr] = neighborIndex;
                        }
                    }
                }
                // Case 2: The neighbor already contains the same value (Conflict)
                else if (neighbor.Value == currentCellValue)
                {
                    throw new UnsolvableBoard();
                }
            }
        }
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
	
	/// <summary>
    /// Maps the game's character symbols to internal integer values and vice-versa.
    /// This allows the engine to work with integers while the UI displays characters.
    /// </summary>
    /// <exception cref="InvalidBoardDimensions">Thrown when the symbols string is too short for the board size.</exception>
    private void InitializeDictionaries()
    {
        CharToValue = new Dictionary<char, int>();
        ValueToChar = new Dictionary<int, char>();

        // Map the designated empty cell character to the internal empty value
        CharToValue[EmptyCellChar] = Cell.EmptyCellValue;
        ValueToChar[Cell.EmptyCellValue] = EmptyCellChar;

        int currentValue = 1;

        // Iterate through symbols and map them to sequential integer values
        for (int i = 0; i < _symbols.Length && currentValue <= Size; i++)
        {
            char symbol = _symbols[i];

            CharToValue[symbol] = currentValue;
            ValueToChar[currentValue] = symbol;

            currentValue++;
        }

        // Validate that we have enough symbols to represent all possible values in this board size
        if (currentValue <= Size)
        {
            throw new InvalidBoardDimensions(InvalidBoardDimensions.BoardTooBig(Size));
        }
    }
	
	/// <summary>
    /// Populates the board cells with initial values from the input string 
    /// and pre-calculates neighbor indices for efficient lookup.
    /// </summary>
    /// <param name="boardString">The string representing the initial Sudoku state.</param>
    private void BuildNeighborsAndInitCells(string boardString)
    {
        int neighborPointer = 0;

        for (int i = 0; i < TotalCells; i++)
        {
            int cellValue = GetValueFromChar(boardString[i]);

            // Add fixed cells to the work stack for initial propagation
            if (cellValue != Cell.EmptyCellValue)
            {
                _workStack[_workPtr++] = i;
            }

            Cells[i] = new Cell(cellValue, _fullMask, Size);
            
            // Map neighbors into the continuous AllNeighbors array
            NeighborOffsets[i] = neighborPointer;
            neighborPointer = MapNeighborsToFlatArray(i, neighborPointer);
        }

        // Adjust pointer after the final increment
        _workPtr--;
    }

    /// <summary>
    /// Validates a character from the board string and returns its internal integer value.
    /// </summary>
    private int GetValueFromChar(char symbol)
    {
        if (!CharToValue.ContainsKey(symbol))
        {
            throw new InvalidCharacter(InvalidCharacter.InvalidCharMsg(symbol));
        }
        return CharToValue[symbol];
    }

    /// <summary>
    /// Calculates neighbors for a cell and copies them into the flat AllNeighbors array.
    /// </summary>
    /// <param name="cellIndex">The index of the cell being processed.</param>
    /// <param name="pointer">The current insertion index in AllNeighbors.</param>
    /// <returns>The updated pointer index after insertion.</returns>
    private int MapNeighborsToFlatArray(int cellIndex, int pointer)
    {
        HashSet<int> neighborsSet = GetNeighborsForCell(cellIndex);

        foreach (int neighborIndex in neighborsSet)
        {
            AllNeighbors[pointer++] = neighborIndex;
        }

        return pointer;
    }
	
	/// <summary>
    /// Returns a unique set of indices for all cells sharing a row, column, or box with the target cell.
    /// </summary>
    private HashSet<int> GetNeighborsForCell(int cellIndex)
    {
        HashSet<int> neighbors = new HashSet<int>();
        int row = cellIndex / Size;
        int col = cellIndex % Size;

        AddRowNeighbors(neighbors, row);
        AddColumnNeighbors(neighbors, col);
        AddBoxNeighbors(neighbors, row, col);

        // A cell is not its own neighbor
        neighbors.Remove(cellIndex);

        return neighbors;
    }

    /// <summary>
    /// Collects all cell indices belonging to the specified row.
    /// </summary>
    private void AddRowNeighbors(HashSet<int> neighbors, int row)
    {
        int rowStart = row * Size;
        for (int c = 0; c < Size; c++)
            neighbors.Add(rowStart + c);
    }

    /// <summary>
    /// Collects all cell indices belonging to the specified column.
    /// </summary>
    private void AddColumnNeighbors(HashSet<int> neighbors, int col)
    {
        for (int r = 0; r < Size; r++)
            neighbors.Add(r * Size + col);
    }

    /// <summary>
    /// Collects all cell indices belonging to the specific rectangular sub-grid (box).
    /// </summary>
    private void AddBoxNeighbors(HashSet<int> neighbors, int row, int col)
    {
        // Integer division trick to find the start of the block
        int boxStartRow = (row / _boxRows) * _boxRows;
        int boxStartCol = (col / _boxCols) * _boxCols;

        for (int r = boxStartRow; r < boxStartRow + _boxRows; r++)
        {
            for (int c = boxStartCol; c < boxStartCol + _boxCols; c++)
            {
                neighbors.Add(r * Size + c);
            }
        }
    }
}