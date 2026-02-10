using System;
using System.Diagnostics;

namespace Logic
{
    public class Solver
    {
        private readonly Board _board;
        private readonly int[] _workStack;

        private readonly UndoStep[] _undoStack;
        private int _undoPtr = 0;

        private const short FullBoardFlag = -1;

        private readonly Stopwatch _timer = new Stopwatch();
        private long _maxMilliseconds;
        private double msPerCell = 50.0;

        public Solver(Board board)
        {
            _board = board;

            // Pre-allocated to the maximum possible number of dependency updates to ensure safety and speed without resizing.
            _workStack = new int[board.TotalCells * board.NeighborsPerCell];

            // Each cell affects a maximum of (3 * Size - CONST) neighbors.
            // The undoStack size is pre-allocated to (TotalCells * Size) to cover the 
            // worst-case scenario where every candidate removal is tracked during propagation.
            _undoStack = new UndoStep[board.TotalCells * board.Size];

            _maxMilliseconds = (long)(_board.TotalCells * msPerCell);
        }

        /// <summary>
        /// Executes the full solving process by performing an initial constraint propagation 
        /// followed by a recursive backtracking search.
        /// </summary>
        public bool Solve()
        {
            _timer.Restart();
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

        /// <summary>
        /// The core recursive algorithm that explores potential cell values. 
        /// Uses a snapshot-based undo system to revert changes during failed branches.
        /// </summary>
        private bool Backtrack()
        {
            if (_timer.ElapsedMilliseconds > _maxMilliseconds)
            {
                _timer.Stop();
                throw new TimeoutException($"Solver timed out after {_timer.ElapsedMilliseconds}ms (Limit for size {_board.Size}x{_board.Size} is {_maxMilliseconds}ms).");
            }

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

        /// <summary>
        /// Selects the most constrained cell using the Minimum Remaining Values (MRV) heuristic.
        /// This method scans the board for an empty cell with the smallest number of candidates.
        /// </summary>
        private int FindBestCellIndex()
        {
            int bestCellIndex = FullBoardFlag;
            int leastCandidates = _board.Size + 1;
            int mostEmptyNeighbors = 0;

            Cell currentCell;
            int neighborIndex;
            int currentEmptyNeighbors;
            int offset;

            for (int i = 0; i < _board.TotalCells; i++)
            {
                currentCell = _board.Cells[i];
                if (currentCell.Value == Cell.EmptyCellValue && currentCell.CandidatesCount <= leastCandidates)
                {
                    currentEmptyNeighbors = 0;
                    for (int j = 0; j < _board.NeighborsPerCell; j++)
                    {
                        offset = _board.NeighborOffsets[i];
                        neighborIndex = _board.AllNeighbors[offset + j];
                        if (_board.Cells[neighborIndex].Value == Cell.EmptyCellValue)
                        {
                            currentEmptyNeighbors++;
                        }
                    }

                    if (currentCell.CandidatesCount == leastCandidates)
                    {
                        if (currentEmptyNeighbors < mostEmptyNeighbors)
                        {
                            continue;
                        }
                    }

                    bestCellIndex = i;
                    leastCandidates = currentCell.CandidatesCount;
                    mostEmptyNeighbors = currentEmptyNeighbors;
                }
            }

            return bestCellIndex;
        }

        /// <summary>
        /// Attempts to assign a value to a cell and immediately propagates the constraints 
        /// to all affected neighbors to prune the search tree.
        /// </summary>
        private bool TryValue(int startCellIndex)
        {
            long currentCellBitVal;
            int currentCellIndex, currentCellValue, offset, currentNeighborIndex, workPtr = 0;
            _workStack[workPtr] = startCellIndex;

            while (workPtr >= 0)
            {
                currentCellIndex = _workStack[workPtr--];
                currentCellValue = _board.Cells[currentCellIndex].Value;
                currentCellBitVal = 1L << (currentCellValue - 1);
                offset = _board.NeighborOffsets[currentCellIndex];

                for (int i = 0; i < _board.NeighborsPerCell; i++)
                {
                    currentNeighborIndex = _board.AllNeighbors[offset + i];
                    ref Cell neighbor = ref _board.Cells[currentNeighborIndex];

                    // if the neighbore cell is empty
                    if (neighbor.Value == Cell.EmptyCellValue)
                    {
                        // if the candidate bit is on in the neighbore cell
                        if ((neighbor.CandidatesMask & currentCellBitVal) != 0)
                        {
                            // if the only candidate of the neighbore cell is the checked value 
                            if (neighbor.CandidatesCount == 1)
                            {
                                return false;
                            }

                            // save neighbor candidate
                            _undoStack[_undoPtr++] = new UndoStep(currentNeighborIndex, currentCellBitVal);

                            neighbor.RemoveCandidate(currentCellBitVal);

                            // if one candidate left after removal(naked single)
                            if (neighbor.CandidatesCount == 1)
                            {
                                neighbor.Value = BitmaskToValue(neighbor.CandidatesMask);
                                _undoStack[_undoPtr++] = new UndoStep(currentNeighborIndex, UndoStep.ValueAssignmentFlag);
                                _workStack[++workPtr] = currentNeighborIndex;
                            }
                        }
                    }

                    // if the neighbore cell contain the checked value
                    else if (neighbor.Value == currentCellValue)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Reverts all board changes (value assignments and candidate removals) made 
        /// since a specific point in the search process.
        /// </summary>
        private void Undo(int snapshot)
        {
            while (_undoPtr > snapshot)
            {
                _undoPtr--;
                UndoStep step = _undoStack[_undoPtr];
                ref Cell currentCell = ref _board.Cells[step.CellIdx];

                if (step.RemovedBit == UndoStep.ValueAssignmentFlag)
                {
                    currentCell.Value = Cell.EmptyCellValue;
                }

                else
                {
                    currentCell.CandidatesMask |= step.RemovedBit;
                    currentCell.CandidatesCount++;
                }
            }
        }
    }
}