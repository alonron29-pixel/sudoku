public class Cell
{
    public int Value { get; set; }
    public int CandidatesMask { get; set; } // bits representing the options of the value for this cell
    public int CandidatesCount { get; set; }  // number of on bits

    public const int EmptyCellValue = 0;

    public Cell(int value, int mask=0, int count=0)
    {
        Value = value;
        CandidatesMask = mask;
        CandidatesCount = count;
    }
    public void RemoveCandidate(int candidateMask)
    {
        CandidatesMask &= ~candidateMask;
        CandidatesCount--;
    }
}