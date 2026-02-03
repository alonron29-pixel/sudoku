public struct UndoStep
{
    public int CellIdx;
    public int RemovedBit;

    public const short ValueAssignmentFlag = 0;

    public UndoStep(int cellIdx, int removedBit)
    {
        CellIdx = cellIdx;
        RemovedBit = removedBit;
    }
}
