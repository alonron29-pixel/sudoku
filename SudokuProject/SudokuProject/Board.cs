using System;
using System.Collections.Generic;

public class Board
{
    // --- Properties ---
    public int Size { get; private set; }
    public int TotalCells { get; private set; } 
    public int[] AllNeighbors { get; private set; }
    public int[] NeighborOffsets { get; private set; }
    public int NeighborsPerCell { get; private set; }
    public Cell[] Cells { get; private set; }
    public Dictionary<char, int> CharToValue { get; private set; }
    public Dictionary<int, char> ValueToChar { get; private set; }

    // --- Private Fields ---
    private int _boxRows;
    private int _boxCols;
    private long _fullMask;
    private int[] _workStack;
    private int _workPtr = 0;
    private const char EmptyCellChar = '0';
    private readonly string _symbols = "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    // --- Constructor ---
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

                int cellValue = Cells[row * Size + col].Value;
                Console.Write($" {ValueToChar[cellValue]}");
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

                if (neighbor.Value == Cell.EmptyCellValue)
                {
                    if ((neighbor.CandidatesMask & currentCellBitmask) != 0)
                    {
                        if (neighbor.CandidatesCount == 1)
                        {
                            throw new UnsolvableBoard();
                        }

                        neighbor.RemoveCandidate(currentCellBitmask);

                        if (neighbor.CandidatesCount == 1)
                        {
                            neighbor.Value = Solver.BitmaskToValue(neighbor.CandidatesMask);
                            _workStack[++_workPtr] = neighborIndex;
                        }
                    }
                }
                else if (neighbor.Value == currentCellValue)
                {
                    throw new UnsolvableBoard();
                }
            }
        }
    }

    private void InitializeDimensions(string boardString)
    {
        ValidateAndSetSize(boardString);
        SetBoxDimensions();
        AllocateArrays();
    }

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

    private void SetBoxDimensions()
    {
        _boxRows = (int)Math.Sqrt(Size);

        if (_boxRows * _boxRows == Size)
        {
            _boxCols = _boxRows;
        }
        else
        {
            Console.Write("Enter number of rows per rectangle: ");
            _boxRows = int.Parse(Console.ReadLine());
            Console.Write("Enter number of columns per rectangle: ");
            _boxCols = int.Parse(Console.ReadLine());

            if (_boxRows * _boxCols != Size)
            {
                throw new InvalidBoardDimensions(InvalidBoardDimensions.InvalidRowColSize);
            }
        }

        _fullMask = (1L << Size) - 1;
        NeighborsPerCell = (Size * 3) - _boxRows - _boxCols - 1;
    }

    private void AllocateArrays()
    {
        Cells = new Cell[TotalCells];
        AllNeighbors = new int[TotalCells * NeighborsPerCell];
        NeighborOffsets = new int[TotalCells];
        _workStack = new int[TotalCells];
    }

    private void InitializeDictionaries()
    {
        CharToValue = new Dictionary<char, int>();
        ValueToChar = new Dictionary<int, char>();

        CharToValue[EmptyCellChar] = Cell.EmptyCellValue;
        ValueToChar[Cell.EmptyCellValue] = EmptyCellChar;

        int currentValue = 1;
        for (int i = 0; i < _symbols.Length && currentValue <= Size; i++)
        {
            char symbol = _symbols[i];
            CharToValue[symbol] = currentValue;
            ValueToChar[currentValue] = symbol;
            currentValue++;
        }

        if (currentValue <= Size)
        {
            throw new InvalidBoardDimensions(InvalidBoardDimensions.BoardTooBig(Size));
        }
    }

    private void BuildNeighborsAndInitCells(string boardString)
    {
        int neighborPointer = 0;
        for (int i = 0; i < TotalCells; i++)
        {
            int cellValue = GetValueFromChar(boardString[i]);
            if (cellValue != Cell.EmptyCellValue)
            {
                _workStack[_workPtr++] = i;
            }

            Cells[i] = new Cell(cellValue, _fullMask, Size);
            NeighborOffsets[i] = neighborPointer;
            neighborPointer = MapNeighborsToFlatArray(i, neighborPointer);
        }
        _workPtr--;
    }

    private int GetValueFromChar(char symbol)
    {
        if (!CharToValue.ContainsKey(symbol))
        {
            throw new InvalidCharacter(InvalidCharacter.InvalidCharMsg(symbol));
        }
        return CharToValue[symbol];
    }

    private int MapNeighborsToFlatArray(int cellIndex, int pointer)
    {
        HashSet<int> neighborsSet = GetNeighborsForCell(cellIndex);
        foreach (int neighborIndex in neighborsSet)
        {
            AllNeighbors[pointer++] = neighborIndex;
        }
        return pointer;
    }

    private HashSet<int> GetNeighborsForCell(int cellIndex)
    {
        HashSet<int> neighbors = new HashSet<int>();
        int row = cellIndex / Size;
        int col = cellIndex % Size;

        AddRowNeighbors(neighbors, row);
        AddColumnNeighbors(neighbors, col);
        AddBoxNeighbors(neighbors, row, col);

        neighbors.Remove(cellIndex);
        return neighbors;
    }

    private void AddRowNeighbors(HashSet<int> neighbors, int row)
    {
        int rowStart = row * Size;
        for (int col = 0; col < Size; col++)
            neighbors.Add(rowStart + col);
    }

    private void AddColumnNeighbors(HashSet<int> neighbors, int col)
    {
        for (int row = 0; row < Size; row++)
            neighbors.Add(row * Size + col);
    }

    private void AddBoxNeighbors(HashSet<int> neighbors, int row, int col)
    {
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