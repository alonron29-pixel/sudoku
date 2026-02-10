using System;
using System.IO;
using System.Collections.Generic;
using Xunit;

public class BoardTests
{
    // Standard empty board strings for testing
    private readonly string _empty9x9 = new string('0', 81);
    private readonly string _empty4x4 = new string('0', 16);
    private readonly string _empty25x25 = new string('0', 625);

    // --- Validation & Constructor Tests ---

    [Fact]
    [Trait("Category", "BoardLogic")]
    public void Constructor_NullOrEmptyString_ShouldThrowException()
    {
        // Check how the board handles null or empty input strings
        Assert.Throws<InvalidBoardDimensions>(() => new Board(null));
        Assert.Throws<InvalidBoardDimensions>(() => new Board(""));
    }

    [Theory]
    [Trait("Category", "BoardLogic")]
    [InlineData("000")]
    [InlineData("00000")]
    [InlineData("1234567890")]
    public void Constructor_InvalidLength_ShouldThrow_InvalidDimensions(string invalidBoard)
    {
        // The total length must be a perfect square
        Assert.Throws<InvalidBoardDimensions>(() => new Board(invalidBoard));
    }

    [Fact]
    [Trait("Category", "BoardLogic")]
    public void Constructor_InvalidCharacter_ShouldThrow_InvalidCharacterException()
    {
        //  Input contains a character not defined in the _symbols string or '0'
        string invalidCharBoard = "000000000000000$";
        Assert.Throws<InvalidCharacter>(() => new Board(invalidCharBoard));
    }

    [Fact]
    [Trait("Category", "BoardLogic")]
    public void Constructor_Valid9x9_ShouldInitializeCorrectly()
    {
        // Ensure a standard 9x9 board sets its properties correctly
        var board = new Board(_empty9x9);

        Assert.Equal(9, board.Size);
        Assert.Equal(81, board.TotalCells);
        // Formula: (Size*3) - BoxRows - BoxCols - 1 
        // For 9x9: (27) - 3 - 3 - 1 = 20 neighbors per cell
        Assert.Equal(20, board.NeighborsPerCell);
    }

    // --- Neighbor & Mapping Logic Tests ---

    [Fact]
    [Trait("Category", "BoardLogic")]
    public void Neighbors_CornerCell_ShouldHaveCorrectIndices_4x4()
    {
        // Corner cells are high-risk for math errors
        // In 4x4, Cell 0 (Top-Left) neighbors:
        // Row: 1, 2, 3 | Col: 4, 8, 12 | Box: 1, 4, 5
        // Unique Neighbors: {1, 2, 3, 4, 5, 8, 12} (Total 7)

        var board = new Board(_empty4x4);
        int cellIndex = 0;
        int offset = board.NeighborOffsets[cellIndex];

        var actualNeighbors = new HashSet<int>();
        for (int i = 0; i < board.NeighborsPerCell; i++)
        {
            actualNeighbors.Add(board.AllNeighbors[offset + i]);
        }

        Assert.Equal(7, board.NeighborsPerCell);
        Assert.Contains(1, actualNeighbors); // Row neighbor
        Assert.Contains(4, actualNeighbors); // Column neighbor
        Assert.Contains(5, actualNeighbors); // Box neighbor (diagonal)
        Assert.DoesNotContain(0, actualNeighbors); // A cell cannot be its own neighbor
    }

    // --- I/O and Non-Standard Dimensions ---

    [Fact]
    [Trait("Category", "BoardLogic")]
    public void Constructor_NonSquareBoxSize_ShouldReadFromConsole()
    {
        // A 6x6 board has 36 cells. Sqrt(6) is not an integer.
        // The code calls Console.ReadLine() to get box dimensions (for example 2x3).
        // We use StringReader to simulate user input and prevent the test from hanging.

        string board6x6 = new string('0', 36);
        string simulatedInput = "2\n3\n"; // Rows: 2, Cols: 3

        using (var sr = new StringReader(simulatedInput))
        {
            Console.SetIn(sr); // Redirect Console.In to our simulated input

            var board = new Board(board6x6);

            Assert.Equal(6, board.Size);
            // Neighbors for 6x6 with 2x3 boxes: (18) - 2 - 3 - 1 = 12
            Assert.Equal(12, board.NeighborsPerCell);
        }
    }

    // --- Logic & Constraint Propagation Tests ---

    [Fact]
    [Trait("Category", "BoardLogic")]
    public void InitialPropagation_ImmediateConflict_ShouldThrowUnsolvable()
    {
        // Board has two identical values in the same row/column/box initially.
        // Propagation should detect this violation of Sudoku rules immediately.
        char[] cells = new char[16];

        // Fill the array with zeros
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = '0';
        }

        cells[0] = '1';
        cells[1] = '1'; // Conflict in the first row
        string conflictingBoard = new string(cells);

        var board = new Board(conflictingBoard);

        Assert.Throws<UnsolvableBoard>(() => board.InitialPropagation());
    }

    [Fact]
    [Trait("Category", "BoardLogic")]
    public void InitialPropagation_NakedSingle_ShouldSolveCell()
    {
        // If a cell has only one possible candidate due to its neighbors,
        // InitialPropagation should automatically fill it (Naked Single).

        // 4x4 Board where cell 0 is surrounded by 2, 3, 4. It MUST be 1.
        // Row 1: [0, 2, 3, 4] -> only 1 is possible for the 0
        string nakedSingleBoard = "0234" + "0000" + "0000" + "0000";

        var board = new Board(nakedSingleBoard);

        // Before propagation, cell value is 0 (empty)
        Assert.Equal(0, board.Cells[0].Value);

        board.InitialPropagation();

        // After propagation, the system should have solved the naked single
        Assert.Equal(1, board.Cells[0].Value);
    }

    // --- 25x25 Large Board Tests ---

    [Fact]
    [Trait("Category", "BoardLogic")]
    public void Constructor_Valid25x25_ShouldInitializeCorrectly()
    {
        var board = new Board(_empty25x25);

        Assert.Equal(25, board.Size);
        Assert.Equal(625, board.TotalCells);

        // Formula: (Size * 3) - BoxRows - BoxCols - 1
        // For 25x25 (5x5 boxes): (25 * 3) - 5 - 5 - 1 = 75 - 11 = 64
        Assert.Equal(64, board.NeighborsPerCell);

        // Ensure the full mask uses the 25th bit correctly (1L << 25) - 1
        // This confirms 'long' bitmask is working for sizes > 16
        long expectedMask = (1L << 25) - 1;
        Assert.Equal(expectedMask, board.Cells[0].CandidatesMask);
    }

    [Fact]
    [Trait("Category", "BoardLogic")]
    public void Constructor_25x25_ShouldMapLettersToValues()
    {
        // In _symbols string, 1-9 are 1-9, A is 10, B is 11... 
        // P should be the 25th value (9 digits + 16th letter)
        string boardWithP = new string('0', 624) + "P";

        var board = new Board(boardWithP);

        // Check the last cell
        int pValue = board.CharToValue['P'];
        Assert.Equal(25, pValue);
        Assert.Equal(25, board.Cells[624].Value);
    }

    [Fact]
    [Trait("Category", "BoardLogic")]
    public void Neighbors_CenterCell_ShouldHaveUniqueIndices_25x25()
    {
        var board = new Board(_empty25x25);
        int centerIndex = 312; // Roughly the middle of 625 cells
        int offset = board.NeighborOffsets[centerIndex];

        // Collect all neighbors in a hashset to check for duplicates
        var neighborSet = new HashSet<int>();
        for (int i = 0; i < board.NeighborsPerCell; i++)
        {
            neighborSet.Add(board.AllNeighbors[offset + i]);
        }

        // Should have exactly 64 unique neighbors
        Assert.Equal(64, neighborSet.Count);
        Assert.DoesNotContain(centerIndex, neighborSet);
    }

    [Fact]
    [Trait("Category", "BoardLogic")]
    public void InitialPropagation_ConflictInLargeBoard_ShouldThrow()
    {
        // Place two 'P' (value 25) in the same row of a 25x25 board
        char[] cells = new char[625];

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = '0';
        }

        cells[0] = 'P';
        cells[1] = 'P';
        string conflictStr = new string(cells);

        var board = new Board(conflictStr);
        Assert.Throws<UnsolvableBoard>(() => board.InitialPropagation());
    }
}
