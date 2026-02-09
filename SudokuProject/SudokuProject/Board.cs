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
