using sudoku_solver;

public class SudokuBoard
{
    // --- Structural Properties (Constants set during initialization) ---
    public int Size { get; }          
    public int BoxRows { get; }      
    public int BoxCols { get; }       
    public int TotalCells { get; }     // Size * Size 

    // --- Performance Caches (Read-only after Pre-computation) ---
    // A single continuous block of memory containing all neighbors for all cells.
    private readonly int[] _allNeighbors;

    // Starting index in _allNeighbors for each cell.
    private readonly int[] _neighborOffsets;

    // How many neighbors each cell has (constant for a given board size)
    private readonly int _neighborsPerCell;

    // Fast lookup for bitmasks
    private readonly int[] _valueToBitmask;

    // All bits on
    public int AllOptionsMask { get; }

    // --- Board State (Dynamic data modified during solving) ---
    public Cell[] Cells { get; }
    public int FixedCellsCount { get; private set; }
}
