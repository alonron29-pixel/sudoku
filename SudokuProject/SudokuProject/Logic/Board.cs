using System.Collections.Generic;
using Exceprions;

namespace Logic
{
    public class Board
    {
        public int BoxRows { get; private set; }
        public int BoxCols { get; private set; }
        public int Size { get; private set; }
        public int TotalCells { get; private set; }
        public int[] AllNeighbors { get; private set; }
        public int[] NeighborOffsets { get; private set; }
        public int NeighborsPerCell { get; private set; }
        public Cell[] Cells { get; private set; }
        public Dictionary<char, int> CharToValue { get; private set; }
        public Dictionary<int, char> ValueToChar { get; private set; }

        private long _fullMask;
        private int[] _workStack;
        private int _workPtr = 0;
        private const char EmptyCellChar = '0';
        private readonly string _symbols = "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        public Board(string boardString, int size, int boxRows, int boxCols)
        {
            InitializeDimensions(size, boxRows, boxCols);
            InitializeDictionaries();
            BuildNeighborsAndInitCells(boardString);
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
                                throw new UnsolvableBoardException();
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
                        throw new UnsolvableBoardException();
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the core structural dimensions of the board and pre-calculates essential values 
        /// for the bitmasking and neighbor-graph systems.
        /// </summary>
        /// <param name="size">The total number of cells in a single row or column.</param>
        /// <param name="boxRows">The number of rows within a single sub-grid rectangle.</param>
        /// <param name="boxCols">The number of columns within a single sub-grid rectangle.</param>
        private void InitializeDimensions(int size, int boxRows, int boxCols)
        {
            TotalCells = size * size;
            Size = size;

            BoxRows = boxRows;
            BoxCols = boxCols;

            // Used to represent all possible candidates for a cell.
            _fullMask = (1L << Size) - 1;

            // Calculates the fixed number of neighbors for any cell: 
            // (Row neighbors + Column neighbors + Box neighbors) - overlaps.
            NeighborsPerCell = (Size * 3) - BoxRows - BoxCols - 1;

            AllocateArrays();
        }

        /// <summary>
        /// Performs memory allocation for all primary board structures and the solver's work stack.
        /// </summary>
        private void AllocateArrays()
        {
            Cells = new Cell[TotalCells];
            AllNeighbors = new int[TotalCells * NeighborsPerCell];
            NeighborOffsets = new int[TotalCells];
            _workStack = new int[TotalCells];
        }

        /// <summary>
        /// Maps character symbols to integer values and vice versa based on the board's size.
        /// Supports standard digits and alphanumeric characters for large boards.
        /// </summary>
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
                throw new InvalidBoardDimensionsException(InvalidBoardDimensionsException.BoardTooBig(Size));
            }
        }

        /// <summary>
        /// Populates cells with initial values, generates the flattened neighbor graph, and prepares the propagation stack.
        /// </summary>
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

        /// <summary>
        /// Translates a single board character into its logical integer representation.
        /// </summary>
        private int GetValueFromChar(char symbol)
        {
            if (!CharToValue.ContainsKey(symbol))
            {
                throw new InvalidCharacterException(InvalidCharacterException.InvalidCharMsg(symbol));
            }
            return CharToValue[symbol];
        }

        /// <summary>
        /// Copies neighbor indices into the global flattened AllNeighbors array for high-speed access.
        /// </summary>
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
        /// Identifies all unique neighbor indices (row, column, and box) for a specific cell.
        /// </summary>
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

        /// <summary>
        /// Adds all cell indices from a specific row to the neighbor set.
        /// </summary>
        private void AddRowNeighbors(HashSet<int> neighbors, int row)
        {
            int rowStart = row * Size;
            for (int col = 0; col < Size; col++)
                neighbors.Add(rowStart + col);
        }

        /// <summary>
        /// Adds all cell indices from a specific column to the neighbor set.
        /// </summary>
        private void AddColumnNeighbors(HashSet<int> neighbors, int col)
        {
            for (int row = 0; row < Size; row++)
                neighbors.Add(row * Size + col);
        }

        /// <summary>
        /// Adds all cell indices from the local sub-grid (box) to the neighbor set.
        /// </summary>
        private void AddBoxNeighbors(HashSet<int> neighbors, int row, int col)
        {
            int boxStartRow = (row / BoxRows) * BoxRows;
            int boxStartCol = (col / BoxCols) * BoxCols;

            for (int r = boxStartRow; r < boxStartRow + BoxRows; r++)
            {
                for (int c = boxStartCol; c < boxStartCol + BoxCols; c++)
                {
                    neighbors.Add(r * Size + c);
                }
            }
        }
    }
}