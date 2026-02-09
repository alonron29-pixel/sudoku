public struct UndoStep
{
    public int CellIdx;
    public long RemovedBit;

    public const short ValueAssignmentFlag = 0;

    public UndoStep(int cellIdx, long removedBit)
    {
        CellIdx = cellIdx;
        RemovedBit = removedBit;
    }
}