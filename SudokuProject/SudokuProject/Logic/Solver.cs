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
        private const short ZeroCandidatesCellFlag = -2;

        private readonly Stopwatch _timer = new Stopwatch();
        private long _maxMilliseconds;
        private double msPerCell = 50.0;

        private int _mrvThreshold; // Min number of fixed cells to activate MRV (FindBestCellIndex)
        private const double MrvActivationPercent = 0.05;

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

            _mrvThreshold = (int)(_board.TotalCells * MrvActivationPercent);

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

            // Board is full
            if (_board.FixedCellCount == _board.TotalCells)
            {
                return true; 
            }

            int cellIndex;
            if (_board.FixedCellCount >= _mrvThreshold)
            {
                cellIndex = FindBestCellIndex(); // MRV Logic
            }
            else
            {
                cellIndex = FindFirstEmptyCell(); // Simple Scan Logic
            }

            if (cellIndex == ZeroCandidatesCellFlag) 
                return false;

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
                    _board.FixedCellCount++;

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
        /// Finds the first available empty cell in the board using a simple linear scan.
        /// This is a lightweight alternative to MRV, typically used during the early stages 
        /// of the solving process to save computation time.
        /// </summary>
        private int FindFirstEmptyCell()
        {
            for (int i = 0; i < _board.TotalCells; i++)
            {
                if (_board.Cells[i].Value == Cell.EmptyCellValue)
                {
                    if (_board.Cells[i].CandidatesCount == 0) 
                        return ZeroCandidatesCellFlag;
                    return i;
                }
            }
            return FullBoardFlag;
        }

        
        /// <summary>
        /// Selects the next cell to branch on using the Minimum Remaining Values (MRV) heuristic, 
        /// with a Degree Heuristic (most empty neighbors) as a tie-breaker.
        /// </summary>
        private int FindBestCellIndex()
        {
            int bestCellIndex = FullBoardFlag;
            int leastCandidates = _board.Size + 1;
            int mostEmptyNeighbors = -1;

            for (int i = 0; i < _board.TotalCells; i++)
            {
                // Use 'ref' to avoid copying the Cell struct, improving performance during high-frequency scans
                ref Cell currentCell = ref _board.Cells[i];

                // Skip cells that are already assigned a value
                if (currentCell.Value != Cell.EmptyCellValue) 
                    continue;

                int candidatesCount = currentCell.CandidatesCount;

                // If an empty cell has no valid candidates, this branch is dead
                if (candidatesCount == 0) 
                    return ZeroCandidatesCellFlag;

                // Naked Single Shortcut: A cell with only 1 candidate is the most constrained possible.
                // Selecting it immediately minimizes branching without needing expensive tie-breaker logic.
                if (candidatesCount == 1) return i;

                // Filter cells with the minimum number of candidates
                if (candidatesCount <= leastCandidates)
                {
                    // Tie-breaker: Degree Heuristic. Count how many neighbors are also empty.
                    // This prioritizes cells that, once filled, will most restrict their surroundings.
                    int currentEmptyNeighbors = 0;
                    int offset = _board.NeighborOffsets[i];

                    for (int j = 0; j < _board.NeighborsPerCell; j++)
                    {
                        int neighborIndex = _board.AllNeighbors[offset + j];
                        if (_board.Cells[neighborIndex].Value == Cell.EmptyCellValue)
                        {
                            currentEmptyNeighbors++;
                        }
                    }

                    // Update best choice if:
                    // We found a cell with fewer candidates than before.
                    // We found a cell with the same candidates count but more empty neighbors.
                    if (candidatesCount < leastCandidates || currentEmptyNeighbors > mostEmptyNeighbors)
                    {
                        leastCandidates = candidatesCount;
                        mostEmptyNeighbors = currentEmptyNeighbors;
                        bestCellIndex = i;
                    }
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
                                _board.FixedCellCount++;
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
                    _board.FixedCellCount--;
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