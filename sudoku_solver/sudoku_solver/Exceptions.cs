using System;

public class InvalidBoardDimensions : Exception
{
    public const string EmptyStringMsg = "Empty board string";
    public const string InvalidDimensionsMsg = "Number of cells has to be a perfect squre";
    public const string InvalidRowColSize = "rectangle length * rectangle width != rectangle size";
    public static string BoardTooBig(int size) => $"Size {size} is too large for the standard char mapping";
    public InvalidBoardDimensions(string message) : base("Invalid dimensions: " + message) { }
}

public class InvalidCharacter : Exception
{
    public static string InvalidCharMsg(char c) => $"Invalid character: {c}";
    public InvalidCharacter(string message) : base(message) {}
}

public class UnsolvableBoard : Exception
{
    public const string UnsolvableBoardMsg = "Board logic is invalid";
    public UnsolvableBoard() : base("Unsolvable board: " + UnsolvableBoardMsg) { }
}
