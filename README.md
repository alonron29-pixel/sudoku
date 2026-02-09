# High-Performance Sudoku Solver with C#

This project features a high-performance Sudoku solver built in .NET, optimized for speed and efficiency. It is capable of solving standard 9x9 boards, rectangular 6x6 grids, and large-scale puzzles using advanced constraint propagation and heuristic search.

## Key Features:
* **Advanced Heuristics**: Implements the MRV (Minimum Remaining Values) heuristic to prioritize the most constrained cells, which significantly prunes the search tree.
* **Bitmasking Optimization**: Uses integer bitmasks to track candidates for each cell, enabling ultra-fast logical operations and reducing memory overhead.
* **Recursive Backtracking with Undo Stack**: Utilizes a custom flat "Undo Stack" to manage state changes efficiently, avoiding the performance cost of cloning board objects.
* **Constraint Propagation**: Automatically solves "Naked Singles" and narrows down possibilities before guessing, using a dedicated propagation engine to speed up the process.
* **Flexible Dimensions**: Supports square (9x9, 16x16) and rectangular (6x6) sub-grids by calculating neighbor indices based on defined scale and offsets.

## Project Structure:
* **Board.cs**: Manages the internal grid state, pre-calculates neighbor indices for every cell, and handles coordinate mapping.
* **Solver.cs**: Contains the core logic for the backtracking algorithm and the constraint propagation engine.
* **Cell.cs**: A lightweight structure representing a single cell, storing its current value and a bitmask of remaining candidates.

## Testing and Performance:
The project includes a comprehensive test suite (using xUnit) designed to validate both the correctness of solutions and the efficiency of the algorithm.

### Performance Benchmarks:
* **Dataset**: Over 50,000 Sudoku boards processed during testing.
* **Performance Target**: Every board must be solved in under 1.0 second.
* **Results**: The solver successfully processes massive datasets of "Hard" and "Expert" puzzles, with many 9x9 boards solved in just a few milliseconds.

### Running the Tests
To run the performance suite, use the Test Explorer in Visual Studio or execute the following bash command in the terminal:
dotnet test --configuration Release
