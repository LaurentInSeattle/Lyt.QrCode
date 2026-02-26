namespace Lyt.QrCode.Encoder;

// Helper class for GetPenaltyScore().
// Internal the run history is organized in reverse order (compared to Nayuki's code) to avoid
// the copying when adding to the history.
internal readonly struct FinderPenalty(int size)
{
    private readonly short[] runHistory = new short[7];
    private readonly int size = size;

    // Can only be called immediately after a light run is added, and returns either 0, 1, or 2. 
    internal readonly int CountPatterns()
    {
        int n = this.runHistory[1];
        Debug.Assert(n <= this.size * 3);

        bool core = 
            n > 0 && 
            this.runHistory[2] == n && 
            this.runHistory[3] == n * 3 &&
            this.runHistory[4] == n &&
            this.runHistory[5] == n;
        return (core && this.runHistory[0] >= n * 4 && this.runHistory[6] >= n ? 1 : 0)
             + (core && this.runHistory[6] >= n * 4 && this.runHistory[0] >= n ? 1 : 0);
    }

    // Must be called at the end of a line (row or column) of modules. 
    internal int TerminateAndCount(bool currentRunColor, int currentRunLength)
    {
        if (currentRunColor)
        {
            // Terminate dark run
            this.AddHistory(currentRunLength);
            currentRunLength = 0;
        }

        currentRunLength += this.size;  // Add light border to final run
        this.AddHistory(currentRunLength);
        return this.CountPatterns();
    }

    // Pushes the given value to the front and drops the last value. 
    internal readonly void AddHistory(int currentRunLength)
    {
        if (this.runHistory[0] == 0)
        {
            currentRunLength += this.size;  // Add light border to initial run
        }

        Array.Copy(this.runHistory, 0, this.runHistory, 1, 6);
        this.runHistory[0] = (short)currentRunLength;
    }
}