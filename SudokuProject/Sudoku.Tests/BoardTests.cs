using Exceprions;
using Logic;
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
    public void Constructor_InvalidCharacter_ShouldThrow_InvalidCharacterException()
    {
        string invalidCharBoard = "000000000000000$"; // 16 chars
        Assert.Throws<InvalidCharacterException>(() => new Board(invalidCharBoard, 4, 2, 2));
    }

    [Fact]
    [Trait("Category", "BoardLogic")]
    public void Constructor_Valid9x9_ShouldInitializeCorrectly()
    {
        // New Signature: string, size, boxRows, boxCols
        var board = new Board(_empty9x9, 9, 3, 3);

        Assert.Equal(9, board.Size);
        Assert.Equal(81, board.TotalCells);
        Assert.Equal(20, board.NeighborsPerCell);
    }

    // --- Neighbor & Mapping Logic Tests ---

    [Fact]
    [Trait("Category", "BoardLogic")]
    public void Neighbors_CornerCell_ShouldHaveCorrectIndices_4x4()
    {
        // 4x4 with 2x2 boxes
        var board = new Board(_empty4x4, 4, 2, 2);
        int cellIndex = 0;
        int offset = board.NeighborOffsets[cellIndex];

        var actualNeighbors = new HashSet<int>();
        for (int i = 0; i < board.NeighborsPerCell; i++)
        {
            actualNeighbors.Add(board.AllNeighbors[offset + i]);
        }

        Assert.Equal(7, board.NeighborsPerCell);
        Assert.Contains(1, actualNeighbors);
        Assert.Contains(4, actualNeighbors);
        Assert.Contains(5, actualNeighbors);
        Assert.DoesNotContain(0, actualNeighbors);
    }

    [Fact]
    [Trait("Category", "BoardLogic")]
    public void Constructor_NonSquareBoxSize_ShouldInitializeDirectly()
    {
        // 6x6 board with 2x3 boxes
        string board6x6 = new string('0', 36);
        var board = new Board(board6x6, 6, 2, 3);

        Assert.Equal(6, board.Size);
        Assert.Equal(12, board.NeighborsPerCell);
    }

    // --- Logic & Constraint Propagation Tests ---

    [Fact]
    [Trait("Category", "BoardLogic")]
    public void InitialPropagation_ImmediateConflict_ShouldThrowUnsolvable()
    {
        char[] cells = new char[16];
        for (int i = 0; i < cells.Length; i++) cells[i] = '0';

        cells[0] = '1';
        cells[1] = '1';
        string conflictingBoard = new string(cells);

        var board = new Board(conflictingBoard, 4, 2, 2);

        Assert.Throws<UnsolvableBoardException>(() => board.InitialPropagation());
    }

    [Fact]
    [Trait("Category", "BoardLogic")]
    public void InitialPropagation_NakedSingle_ShouldSolveCell()
    {
        string nakedSingleBoard = "0234" + "0000" + "0000" + "0000";
        var board = new Board(nakedSingleBoard, 4, 2, 2);

        board.InitialPropagation();

        Assert.Equal(1, board.Cells[0].Value);
    }

    // --- 25x25 Large Board Tests ---

    [Fact]
    [Trait("Category", "BoardLogic")]
    public void Constructor_Valid25x25_ShouldInitializeCorrectly()
    {
        // 25x25 with 5x5 boxes
        var board = new Board(_empty25x25, 25, 5, 5);

        Assert.Equal(25, board.Size);
        Assert.Equal(64, board.NeighborsPerCell);

        long expectedMask = (1L << 25) - 1;
        Assert.Equal(expectedMask, board.Cells[0].CandidatesMask);
    }

    [Fact]
    [Trait("Category", "BoardLogic")]
    public void Constructor_25x25_ShouldMapLettersToValues()
    {
        string boardWithP = new string('0', 624) + "P";
        var board = new Board(boardWithP, 25, 5, 5);

        Assert.Equal(25, board.Cells[624].Value);
    }
}