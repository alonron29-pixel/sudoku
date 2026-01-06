public struct Cell
{
    public int Value;
    public int CandidatesMask;
    public int CandidatesCount;

    public Cell(int mask, int count)
    {
        Value = 0;
        CandidatesMask = mask;
        CandidatesCount = count;
    }
}