using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

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
    private readonly string Symbols = "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    public Dictionary<char, int> CharToVal { get; private set; }
    public Dictionary<int, char> ValToChar { get; private set; }

    public Board(string boardString)
    {
        InitializeDimensions(boardString);
        InitializeDictionaries();
        BuildNeighborsAndInitCells(boardString);
    }

    /// <summary>
    /// Displays the Sudoku board to the console.
    /// </summary>
    public void Print()
    {
        string dashes = new string('-', 2 * (Size + _boxRows) - 1);

        for (int row = 0; row < Size; row++)
        {
            if (row % _boxRows == 0)
            {
                Console.WriteLine($" |{dashes}|");
            }

            for (int col = 0; col < Size; col++)
            {
                if (col % _boxCols == 0)
                {
                    Console.Write(" |");
                }

                Console.Write($" {ValToChar[Cells[row * Size + col].Value]}");
            }
            Console.WriteLine(" |");
        }
        Console.WriteLine($" |{dashes}|");
    }

    /// <summary>
    /// Propagates constraints from fixed cells to narrow down candidates and solve "Naked Singles."
    /// </summary>
    public void InitialPropagation()
    {
        long currentCellBitVal;
        int currentCellIndex, currentCellValue, offset, currentNeighborIndex;

        while (_workPtr >= 0)
        {
            currentCellIndex = _workStack[_workPtr--];
            currentCellValue = Cells[currentCellIndex].Value;
            currentCellBitVal = 1L << (currentCellValue - 1);
            offset = NeighborOffsets[currentCellIndex];

            for (int i = 0; i < NeighborsPerCell; i++)
            {
                currentNeighborIndex = AllNeighbors[offset + i];
                ref Cell neighbor = ref Cells[currentNeighborIndex];

                // if the neighbore cell is empty
                if (neighbor.Value == Cell.EmptyCellValue)
                {
                    // if the candidate bit is on in the neighbore cell
                    if ((neighbor.CandidatesMask & currentCellBitVal) != 0)
                    {
                        // if the only candidate of the neighbore cell is the checked value 
                        if (neighbor.CandidatesCount == 1)
                        {
                            throw new UnsolvableBoard();
                        }

                        neighbor.RemoveCandidate(currentCellBitVal);

                        // if one candidate left after removal (naked single)
                        if (neighbor.CandidatesCount == 1)
                        {
                            neighbor.Value = Solver.BitmaskToValue(neighbor.CandidatesMask);
                            _workStack[++_workPtr] = currentNeighborIndex;
                        }
                    }
                }

                // if the neighbore cell contain the checked value
                else if (neighbor.Value == currentCellValue)
                {
                    throw new UnsolvableBoard();
                }
            }
        }
    }

    /// <summary>
    /// Maps the game's symbols to internal integer values and vice-versa.
    /// </summary>
    public void InitializeDictionaries()
    {
        CharToVal = new Dictionary<char, int>();
        ValToChar = new Dictionary<int, char>();

        // Define empty cell value
        CharToVal[EmptyCellCharValue] = Cell.EmptyCellValue;
        ValToChar[Cell.EmptyCellValue] = EmptyCellCharValue;

        int currentVal = 1;

        for (int i = 0; i < Symbols.Length && currentVal <= Size; i++)
        {
            char c = Symbols[i];

            CharToVal[c] = currentVal;
            ValToChar[currentVal] = c;

            currentVal++;
        }

        // check if there is enogh symbols for the current board size
        if (currentVal <= Size)
        {
            throw new InvalidBoardDimensions(InvalidBoardDimensions.BoardTooBig(Size));
        }
    }

    /// <summary>
    /// Calculates the board's scale, defines sub-grid dimensions, and allocates necessary arrays.
    /// </summary>
    private void InitializeDimensions(string boardString)
    {
        if (string.IsNullOrEmpty(boardString))
        {
            throw new InvalidBoardDimensions(InvalidBoardDimensions.EmptyStringMsg);
        }

        TotalCells = boardString.Length;
        Size = (int)Math.Sqrt(TotalCells);

        // number of cells is not a perfect squre
        if (Size * Size != TotalCells)
        {
            throw new InvalidBoardDimensions(InvalidBoardDimensions.InvalidDimensionsMsg);
        }

        _boxRows = (int)Math.Sqrt(Size);

        // board consists of squres
        if (_boxRows * _boxRows == Size)
        {
            _boxCols = _boxRows;
        }

        // board consists of rectangles
        else
        {
            Console.Write("Enter number of rows per rectangle: ");
            _boxRows = int.Parse(Console.ReadLine());
            Console.Write("Enter number of colums per rectangle: ");
            _boxCols = int.Parse(Console.ReadLine());

            if (_boxRows * _boxCols != Size)
            {
                throw new InvalidBoardDimensions(InvalidBoardDimensions.InvalidRowColSize);
            }
        }

        _fullMask = (1L << Size) - 1;

        // calc: Size - 1 + Size - 1 + Size - (BoxRows + BoxCols - 1)
        NeighborsPerCell = (Size * 3) - _boxRows - _boxCols - 1;

        Cells = new Cell[TotalCells];
        AllNeighbors = new int[TotalCells * NeighborsPerCell];
        NeighborOffsets = new int[TotalCells];
        _workStack = new int[TotalCells];
    }

    /// <summary>
    /// Fills the board with initial values and pre-calculates the neighbor indices for every cell.
    /// </summary>
    private void BuildNeighborsAndInitCells(string boardString)
    {
        char currentChar;
        int val, neighborPointer = 0;
        for (int i = 0; i < TotalCells; i++)
        {
            currentChar = boardString[i];
            if (!CharToVal.ContainsKey(currentChar))
            {
                throw new InvalidCharacter(InvalidCharacter.InvalidCharMsg(currentChar));
            }
            
            val = CharToVal[currentChar];

            if (val != Cell.EmptyCellValue)
            {
                _workStack[_workPtr++] = i;
            }

            // Initialize the Cell object with value from the string
            Cells[i] = new Cell(val, _fullMask, Size);

            // Record the starting index in the flat neighbor array for this cell
            NeighborOffsets[i] = neighborPointer;   

            // Generate a unique set of neighbors (Row, Column, and Box)
            HashSet<int> neighborsSet = GetNeighborsForCell(i);

            foreach (int neighborIndex in neighborsSet)
            {
                // Copy the unique neighbors into the flat array
                AllNeighbors[neighborPointer++] = neighborIndex;
            }
        }
        _workPtr--;
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
