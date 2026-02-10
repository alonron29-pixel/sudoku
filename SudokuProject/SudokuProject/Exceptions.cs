using System;

public class InvalidBoardDimensionsException : Exception
{
    public const string EmptyStringMsg = "Empty board string";
    public const string InvalidDimensionsMsg = "Number of cells has to be a perfect squre";
    public const string InvalidRowColSize = "rectangle length * rectangle width != rectangle size";
    public static string BoardTooBig(int size) => $"Size {size} is too large for the standard char mapping";
    public InvalidBoardDimensionsException(string message) : base("Invalid dimensions: " + message) { }
}

public class InvalidCharacterException : Exception
{
    public static string InvalidCharMsg(char c) => $"Invalid character: {c}";
    public InvalidCharacterException(string message) : base(message) {}
}

public class UnsolvableBoardException : Exception
{
    public const string UnsolvableBoardMsg = "Board logic is invalid";
    public UnsolvableBoardException() : base("Unsolvable board: " + UnsolvableBoardMsg) { }
}