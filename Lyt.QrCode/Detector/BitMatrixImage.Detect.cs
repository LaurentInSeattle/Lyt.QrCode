namespace Lyt.QrCode.Image;

public sealed partial class BitMatrixImage
{
    internal bool TryDetect(
        DetectorCallback? detectorCallback,
        [NotNullWhen(true)] out DetectorResult? detectorResult)
    {
        detectorResult = null;
        if (this.TryFindPatterns(detectorCallback, out var patterns))
        {
            if (patterns.TryProcess(this, detectorCallback, out detectorResult))
            {
                return true;
            }
        }

        return false;
    }

    // Implements the actual pattern finding logic.
    internal bool TryFindPatterns(
        DetectorCallback? detectorCallback, [NotNullWhen(true)] out Patterns? patterns)
    {
        patterns = null;

        const int CENTER_QUORUM = 2;

        /// <summary> 1 pixel/module times 3 modules/center </summary>
        const int MIN_SKIP = 3;

        /// <summary> support up to version 20 for mobile clients </summary>
        const int MAX_MODULES = 97;

        const int INTEGER_MATH_SHIFT = 8;

        List<Pattern> possibleCenters = new(3);
        int[] crossCheckStateCount = new int[5];
        int[] stateCount = new int[5];

        int maxI = this.Height;
        int maxJ = this.Width;

        // We are looking for black/white/black/white/black modules in
        // 1:1:3:1:1 ratio; this tracks the number of such modules seen so far

        // Let's assume that the maximum version QR Code we support takes up 1/4 the height of the
        // image, and then account for the center being 3 modules in size. This gives the smallest
        // number of pixels the center could be, so skip this often. When trying harder, look for all
        // QR versions regardless of how dense they are.
        bool hasSkipped = false;
        int iSkip = (3 * maxI) / (4 * MAX_MODULES);
        if (iSkip < MIN_SKIP) // || tryHarder)
        {
            iSkip = MIN_SKIP;
        }

        #region Locals functions for pattern detection

        void ClearStateCounts() => Array.Clear(stateCount, 0, stateCount.Length);

        void ShiftCounts2()
        {
            stateCount[0] = stateCount[2];
            stateCount[1] = stateCount[3];
            stateCount[2] = stateCount[4];
            stateCount[3] = 1;
            stateCount[4] = 0;
        }

        /// <summary>
        /// Returns true iff the proportions of the counts is close enough to the 1/1/3/1/1 ratios
        /// used by finder patterns to be considered a match
        /// <param name="stateCountArray">count of black/white/black/white/black pixels just read</param>
        bool FoundPatternCross(int[] stateCountArray)
        {
            int totalModuleSize = 0;
            for (int i = 0; i < 5; i++)
            {
                int count = stateCountArray[i];
                if (count == 0)
                {
                    return false;
                }

                totalModuleSize += count;
            }

            if (totalModuleSize < 7)
            {
                return false;
            }

            // Allow less than 50% variance from 1-1-3-1-1 proportions
            int moduleSize = (totalModuleSize << INTEGER_MATH_SHIFT) / 7;
            int maxVariance = moduleSize / 2;
            return Math.Abs(moduleSize - (stateCountArray[0] << INTEGER_MATH_SHIFT)) < maxVariance &&
                   Math.Abs(moduleSize - (stateCountArray[1] << INTEGER_MATH_SHIFT)) < maxVariance &&
                   Math.Abs(3 * moduleSize - (stateCountArray[2] << INTEGER_MATH_SHIFT)) < 3 * maxVariance &&
                   Math.Abs(moduleSize - (stateCountArray[3] << INTEGER_MATH_SHIFT)) < maxVariance &&
                   Math.Abs(moduleSize - (stateCountArray[4] << INTEGER_MATH_SHIFT)) < maxVariance;
        }

        /// <summary>
        /// Returns true if the proportions of the counts is close enough to the 1/1/3/1/1 ratios
        /// by finder patterns to be considered a match</returns>
        /// <param name="stateCountArray">count of black/white/black/white/black pixels just read</param>
        /// </summary>
        bool FoundPatternDiagonal(int[] stateCountArray)
        {
            int totalModuleSize = 0;
            for (int i = 0; i < 5; i++)
            {
                int count = stateCountArray[i];
                if (count == 0)
                {
                    return false;
                }

                totalModuleSize += count;
            }

            if (totalModuleSize < 7)
            {
                return false;
            }

            // Allow less than 75% variance from 1-1-3-1-1 proportions
            float moduleSize = totalModuleSize / 7.0f;
            float maxVariance = moduleSize / 1.333f;
            return
                Math.Abs(moduleSize - stateCountArray[0]) < maxVariance &&
                Math.Abs(moduleSize - stateCountArray[1]) < maxVariance &&
                Math.Abs(3.0f * moduleSize - stateCountArray[2]) < 3 * maxVariance &&
                Math.Abs(moduleSize - stateCountArray[3]) < maxVariance &&
                Math.Abs(moduleSize - stateCountArray[4]) < maxVariance;
        }

        /// <summary>
        /// This is called when a horizontal scan finds a possible alignment pattern. It will
        /// cross check with a vertical scan, and if successful, will, ah, cross-cross-check
        /// with another horizontal scan. This is needed primarily to locate the real horizontal
        /// center of the pattern in cases of extreme skew.
        /// And then we cross-cross-cross check with another diagonal scan.
        /// If that succeeds the finder pattern location is added to a list that tracks
        /// the number of times each location has been nearly-matched as a finder pattern.
        /// Each additional find is more evidence that the location is in fact a finder
        /// pattern center
        /// </summary>
        /// <param name="i">row where finder pattern may be found</param>
        /// <param name="j">end of possible finder pattern in row</param>
        /// <returns> true if a finder pattern candidate was found this time </returns>
        bool HandlePossibleCenter(int i, int j)
        {
            /// <summary> 
            /// Given a count of black/white/black/white/black pixels just seen and an end position,
            /// figures the location of the center of this run.
            /// </summary>
            float? CenterFromEndState(int end)
            {
                float result = (end - stateCount[4] - stateCount[3]) - stateCount[2] / 2.0f;
                return float.IsNaN(result) ? null : result;
            }

            void ClearCrossCheckStateCounts() => Array.Clear(crossCheckStateCount, 0, crossCheckStateCount.Length);

            /// <summary> 
            /// Given a count of black/white/black/white/black pixels just seen and an end position,
            /// figures the location of the center of this run.
            /// </summary>
            float? CenterFromEndCrossCheckState(int end)
            {
                float result = (end - crossCheckStateCount[4] - crossCheckStateCount[3]) - crossCheckStateCount[2] / 2.0f;
                return float.IsNaN(result) ? null : result;
            }

            /// <summary>
            /// After a vertical and horizontal scan finds a potential finder pattern, this method
            /// "cross-cross-cross-checks" by scanning down diagonally through the center of the possible
            /// finder pattern to see if the same proportion is detected.
            /// </summary>
            /// <param name="centerI">row where a finder pattern was detected</param>
            /// <param name="centerJ">center of the section that appears to cross a finder pattern</param>
            /// <returns>true if proportions are withing expected limits</returns>
            bool CrossCheckDiagonal(int centerI, int centerJ)
            {
                ClearCrossCheckStateCounts();

                // Start counting up, left from center finding black center mass
                int i = 0;
                while (centerI >= i && centerJ >= i && this[centerJ - i, centerI - i])
                {
                    crossCheckStateCount[2]++;
                    i++;
                }

                if (crossCheckStateCount[2] == 0)
                {
                    return false;
                }

                // Continue up, left finding white space
                while (centerI >= i && centerJ >= i && !this[centerJ - i, centerI - i])
                {
                    crossCheckStateCount[1]++;
                    i++;
                }

                if (crossCheckStateCount[1] == 0)
                {
                    return false;
                }

                // Continue up, left finding black border
                while (centerI >= i && centerJ >= i && this[centerJ - i, centerI - i])
                {
                    crossCheckStateCount[0]++;
                    i++;
                }

                if (crossCheckStateCount[0] == 0)
                {
                    return false;
                }

                int maxI = this.Height;
                int maxJ = this.Width;

                // Now also count down, right from center
                i = 1;
                while (centerI + i < maxI && centerJ + i < maxJ && this[centerJ + i, centerI + i])
                {
                    crossCheckStateCount[2]++;
                    i++;
                }

                while (centerI + i < maxI && centerJ + i < maxJ && !this[centerJ + i, centerI + i])
                {
                    crossCheckStateCount[3]++;
                    i++;
                }

                if (crossCheckStateCount[3] == 0)
                {
                    return false;
                }

                while (centerI + i < maxI && centerJ + i < maxJ && this[centerJ + i, centerI + i])
                {
                    crossCheckStateCount[4]++;
                    i++;
                }

                if (crossCheckStateCount[4] == 0)
                {
                    return false;
                }

                return FoundPatternDiagonal(crossCheckStateCount);
            }

            /// <summary>
            ///   <p>After a horizontal scan finds a potential finder pattern, this method
            /// "cross-checks" by scanning down vertically through the center of the possible
            /// finder pattern to see if the same proportion is detected.</p>
            /// </summary>
            /// <param name="startI">row where a finder pattern was detected</param>
            /// <param name="centerJ">center of the section that appears to cross a finder pattern</param>
            /// <param name="maxCount">maximum reasonable number of modules that should be
            /// observed in any reading state, based on the results of the horizontal scan</param>
            /// <param name="originalStateCountTotal">The original state count total.</param>
            /// <returns>
            /// vertical center of finder pattern, or null if not found
            /// </returns>
            float? CrossCheckVertical(int startI, int centerJ, int maxCount, int originalStateCountTotal)
            {
                ClearCrossCheckStateCounts();

                // Start counting up from center
                int maxI = this.Height;
                int i = startI;
                while (i >= 0 && this[centerJ, i])
                {
                    crossCheckStateCount[2]++;
                    i--;
                }
                if (i < 0)
                {
                    return null;
                }
                while (i >= 0 && !this[centerJ, i] && crossCheckStateCount[1] <= maxCount)
                {
                    crossCheckStateCount[1]++;
                    i--;
                }
                // If already too many modules in this state or ran off the edge:
                if (i < 0 || crossCheckStateCount[1] > maxCount)
                {
                    return null;
                }
                while (i >= 0 && this[centerJ, i] && crossCheckStateCount[0] <= maxCount)
                {
                    crossCheckStateCount[0]++;
                    i--;
                }
                if (crossCheckStateCount[0] > maxCount)
                {
                    return null;
                }

                // Now also count down from center
                i = startI + 1;
                while (i < maxI && this[centerJ, i])
                {
                    crossCheckStateCount[2]++;
                    i++;
                }
                if (i == maxI)
                {
                    return null;
                }
                while (i < maxI && !this[centerJ, i] && crossCheckStateCount[3] < maxCount)
                {
                    crossCheckStateCount[3]++;
                    i++;
                }
                if (i == maxI || crossCheckStateCount[3] >= maxCount)
                {
                    return null;
                }
                while (i < maxI && this[centerJ, i] && crossCheckStateCount[4] < maxCount)
                {
                    crossCheckStateCount[4]++;
                    i++;
                }
                if (crossCheckStateCount[4] >= maxCount)
                {
                    return null;
                }

                // If we found a finder-pattern-like section, but its size is more than 40% different than
                // the original, assume it's a false positive
                int stateCountTotal = crossCheckStateCount[0] + crossCheckStateCount[1] + crossCheckStateCount[2] + crossCheckStateCount[3] + crossCheckStateCount[4];
                if (5 * Math.Abs(stateCountTotal - originalStateCountTotal) >= 2 * originalStateCountTotal)
                {
                    return null;
                }

                return FoundPatternCross(crossCheckStateCount) ? CenterFromEndCrossCheckState(i) : null;
            }

            /// <summary> <p>Like {@link #crossCheckVertical(int, int, int, int)}, and in fact is basically identical,
            /// except it reads horizontally instead of vertically. This is used to cross-cross
            /// check a vertical cross check and locate the real center of the alignment pattern.</p>
            /// </summary>
            float? CrossCheckHorizontal(int startJ, int centerI, int maxCount, int originalStateCountTotal)
            {
                ClearCrossCheckStateCounts();

                int j = startJ;
                int maxJ = this.Width;
                while (j >= 0 && this[j, centerI])
                {
                    crossCheckStateCount[2]++;
                    j--;
                }

                if (j < 0)
                {
                    return null;
                }

                while (j >= 0 && !this[j, centerI] && crossCheckStateCount[1] <= maxCount)
                {
                    crossCheckStateCount[1]++;
                    j--;
                }

                if (j < 0 || crossCheckStateCount[1] > maxCount)
                {
                    return null;
                }

                while (j >= 0 && this[j, centerI] && crossCheckStateCount[0] <= maxCount)
                {
                    crossCheckStateCount[0]++;
                    j--;
                }

                if (crossCheckStateCount[0] > maxCount)
                {
                    return null;
                }

                j = startJ + 1;
                while (j < maxJ && this[j, centerI])
                {
                    crossCheckStateCount[2]++;
                    j++;
                }

                if (j == maxJ)
                {
                    return null;
                }

                while (j < maxJ && !this[j, centerI] && crossCheckStateCount[3] < maxCount)
                {
                    crossCheckStateCount[3]++;
                    j++;
                }

                if (j == maxJ || crossCheckStateCount[3] >= maxCount)
                {
                    return null;
                }

                while (j < maxJ && this[j, centerI] && crossCheckStateCount[4] < maxCount)
                {
                    crossCheckStateCount[4]++;
                    j++;
                }

                if (crossCheckStateCount[4] >= maxCount)
                {
                    return null;
                }

                // If we found a finder-pattern-like section, but its size is significantly different than
                // the original, assume it's a false positive
                int stateCountTotal = crossCheckStateCount[0] + crossCheckStateCount[1] + crossCheckStateCount[2] + crossCheckStateCount[3] + crossCheckStateCount[4];
                if (5 * Math.Abs(stateCountTotal - originalStateCountTotal) >= originalStateCountTotal)
                {
                    return null;
                }

                return FoundPatternCross(crossCheckStateCount) ? CenterFromEndCrossCheckState(j) : null;
            }

            int stateCountTotal =
                        stateCount[0] + stateCount[1] + stateCount[2] + stateCount[3] + stateCount[4];
            float? centerJ = CenterFromEndState(j);
            if (centerJ == null)
            {
                return false;
            }

            float? centerI = CrossCheckVertical(i, (int)centerJ.Value, stateCount[2], stateCountTotal);
            if (centerI != null)
            {
                // Re-cross check
                centerJ = CrossCheckHorizontal(
                    (int)centerJ.Value, (int)centerI.Value, stateCount[2], stateCountTotal);
                if (centerJ != null && CrossCheckDiagonal((int)centerI, (int)centerJ))
                {
                    float estimatedModuleSize = stateCountTotal / 7.0f;
                    bool found = false;
                    for (int index = 0; index < possibleCenters.Count; index++)
                    {
                        var center = possibleCenters[index];
                        // Look for about the same center and module size:
                        if (center.AboutEquals(estimatedModuleSize, centerI.Value, centerJ.Value))
                        {
                            possibleCenters.RemoveAt(index);
                            possibleCenters.Insert(index, center.CombineEstimate(centerI.Value, centerJ.Value, estimatedModuleSize));

                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        var point = new Pattern(centerJ.Value, centerI.Value, estimatedModuleSize);
                        possibleCenters.Add(point);

                        // CONSIDER: Use the pattern object in the delegate 
                        detectorCallback?.Invoke(point);
                    }

                    return true;
                }
            }

            return false;
        }

        /// Returns the number of rows we could safely skip during scanning, based on the first
        /// two finder patterns that have been located. In some cases their position will
        /// allow us to infer that the third pattern must lie below a certain point farther
        /// down in the image.
        int FindRowSkip()
        {
            int max = possibleCenters.Count;
            if (max <= 1)
            {
                return 0;
            }
            ResultPoint? firstConfirmedCenter = null;
            foreach (var center in possibleCenters)
            {
                if (center.Count >= CENTER_QUORUM)
                {
                    if (firstConfirmedCenter == null)
                    {
                        firstConfirmedCenter = center;
                    }
                    else
                    {
                        // We have two confirmed centers
                        // How far down can we skip before resuming looking for the next
                        // pattern? In the worst case, only the difference between the
                        // difference in the x / y coordinates of the two centers.
                        // This is the case where you find top left last.
                        hasSkipped = true;

                        // UPGRADE_WARNING: Data types in Visual C# might be different.
                        // Verify the accuracy of narrowing conversions.
                        // "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1042'"
                        return (int)(Math.Abs(firstConfirmedCenter.X - center.X) - Math.Abs(firstConfirmedCenter.Y - center.Y)) / 2;
                    }
                }
            }

            return 0;
        }

        /// <returns> true iff we have found at least 3 finder patterns that have been detected
        /// at least {@link #CENTER_QUORUM} times each, and, the estimated module size of the
        /// candidates is "pretty similar"
        /// </returns>
        bool HaveMultiplyConfirmedCenters()
        {
            int confirmedCount = 0;
            float totalModuleSize = 0.0f;
            int max = possibleCenters.Count;
            foreach (var pattern in possibleCenters)
            {
                if (pattern.Count >= CENTER_QUORUM)
                {
                    confirmedCount++;
                    totalModuleSize += pattern.EstimatedModuleSize;
                }
            }
            if (confirmedCount < 3)
            {
                return false;
            }
            // OK, we have at least 3 confirmed centers, but, it's possible that one is a "false positive"
            // and that we need to keep looking. We detect this by asking if the estimated module sizes
            // vary too much. We arbitrarily say that when the total deviation from average exceeds
            // 5% of the total module size estimates, it's too much.
            float average = totalModuleSize / max;
            float totalDeviation = 0.0f;
            for (int i = 0; i < max; i++)
            {
                var pattern = possibleCenters[i];
                totalDeviation += Math.Abs(pattern.EstimatedModuleSize - average);
            }
            return totalDeviation <= 0.05f * totalModuleSize;
        }

        #endregion Locals functions for pattern detection

        bool done = false;
        for (int i = iSkip - 1; i < maxI && !done; i += iSkip)
        {
            ClearStateCounts();
            int currentState = 0;
            for (int j = 0; j < maxJ; j++)
            {
                bool pixelIsBlack = this[j, i];
                if (pixelIsBlack)
                {
                    // Black pixel
                    if ((currentState & 1) == 1)
                    {
                        // Counting white pixels
                        currentState++;
                    }
                    stateCount[currentState]++;
                }
                else
                {
                    // White pixel
                    if ((currentState & 1) == 0)
                    {
                        // Counting black pixels
                        if (currentState == 4)
                        {
                            // A winner?
                            if (FoundPatternCross(stateCount))
                            {
                                // Yes
                                bool confirmed = HandlePossibleCenter(i, j);
                                if (confirmed)
                                {
                                    // Start examining every other line. Checking each line turned out to be too
                                    // expensive and didn't improve performance.
                                    iSkip = 2;
                                    if (hasSkipped)
                                    {
                                        done = HaveMultiplyConfirmedCenters();
                                    }
                                    else
                                    {
                                        int rowSkip = FindRowSkip();
                                        if (rowSkip > stateCount[2])
                                        {
                                            // Skip rows between row of lower confirmed center
                                            // and top of presumed third confirmed center
                                            // but back up a bit to get a full chance of detecting
                                            // it, entire width of center of finder pattern

                                            // Skip by rowSkip, but back off by stateCount[2] (size of last center
                                            // of pattern we saw) to be conservative, and also back off by iSkip which
                                            // is about to be re-added
                                            i += rowSkip - stateCount[2] - iSkip;
                                            j = maxJ - 1;
                                        }
                                    }
                                }
                                else
                                {
                                    ShiftCounts2();
                                    currentState = 3;
                                    continue;
                                }

                                // Clear state to start looking again
                                currentState = 0;
                                ClearStateCounts();
                            }
                            else
                            {
                                // No, shift counts back by two
                                ShiftCounts2();
                                currentState = 3;
                            }
                        }
                        else
                        {
                            stateCount[++currentState]++;
                        }
                    }
                    else
                    {
                        // Counting white pixels
                        stateCount[currentState]++;
                    }
                }
            }

            if (FoundPatternCross(stateCount))
            {
                bool confirmed = HandlePossibleCenter(i, maxJ);
                if (confirmed)
                {
                    iSkip = stateCount[0];
                    if (hasSkipped)
                    {
                        // Found a third one
                        done = HaveMultiplyConfirmedCenters();
                    }
                }
            }
        }

        /// Returns the 3 best {@link FinderPattern}s from our list of candidates. The "best" are
        /// those have similar module size and form a shape closer to a isosceles right triangle.
        /// Can return null if 3 such patterns cannot be found.
        Pattern[]? SelectBestPatterns()
        {
            int startSize = possibleCenters.Count;
            if (startSize < 3)
            {
                // Couldn't find enough finder patterns
                return null;
            }

            for (int i = 0; i < possibleCenters.Count; i++)
            {
                if (possibleCenters[i].Count < CENTER_QUORUM)
                {
                    possibleCenters.RemoveAt(i);
                    i--;
                }
            }

            startSize = possibleCenters.Count;
            if (startSize < 3)
            {
                // Couldn't find enough finder patterns
                return null;
            }

            possibleCenters.Sort(new PatternComparer());

            double distortion = double.MaxValue;
            var bestPatterns = new Pattern[3];

            for (int i = 0; i < possibleCenters.Count - 2; i++)
            {
                Pattern fpi = possibleCenters[i];
                float minModuleSize = fpi.EstimatedModuleSize;

                for (int j = i + 1; j < possibleCenters.Count - 1; j++)
                {
                    Pattern fpj = possibleCenters[j];
                    double squares0 = Pattern.SquaredDistance(fpi, fpj);

                    for (int k = j + 1; k < possibleCenters.Count; k++)
                    {
                        Pattern fpk = possibleCenters[k];
                        float maxModuleSize = fpk.EstimatedModuleSize;
                        if (maxModuleSize > minModuleSize * 1.4f)
                        {
                            // module size is not similar
                            continue;
                        }

                        double a = squares0;
                        double b = Pattern.SquaredDistance(fpj, fpk);
                        double c = Pattern.SquaredDistance(fpi, fpk);

                        // sorts ascending - inlined
                        if (a < b)
                        {
                            if (b > c)
                            {
                                if (a < c)
                                {
                                    (c, b) = (b, c);
                                }
                                else
                                {
                                    double temp = a;
                                    a = c;
                                    c = b;
                                    b = temp;
                                }
                            }
                        }
                        else
                        {
                            if (b < c)
                            {
                                if (a < c)
                                {
                                    (b, a) = (a, b);
                                }
                                else
                                {
                                    double temp = a;
                                    a = b;
                                    b = c;
                                    c = temp;
                                }
                            }
                            else
                            {
                                (c, a) = (a, c);
                            }
                        }

                        // a^2 + b^2 = c^2 (Pythagorean theorem), and a = b (isosceles triangle).
                        // Since any right triangle satisfies the formula c^2 - b^2 - a^2 = 0,
                        // we need to check both two equal sides separately.
                        // The value of |c^2 - 2 * b^2| + |c^2 - 2 * a^2| increases as dissimilarity
                        // from isosceles right triangle.
                        // Heuristically it seems that the following formula works better (although it's
                        // not clear any more why...)
                        double d = Math.Abs(c - 2 * b) + Math.Abs(c - 2 * a);
                        if (d < distortion)
                        {
                            distortion = d;
                            bestPatterns[0] = fpi;
                            bestPatterns[1] = fpj;
                            bestPatterns[2] = fpk;
                        }
                    }
                }
            }

            if (distortion == double.MaxValue)
            {
                return null;
            }

            return bestPatterns;
        }

        var patternInfo = SelectBestPatterns();
        if (patternInfo is null)
        {
            return false;
        }

        // Order: Pattern bottomLeft, Pattern topLeft, Pattern topRight
        ResultPoint.OrderBestPatterns(patternInfo);
        Debug.WriteLine("patternInfo: " + patternInfo[0] + ", " + patternInfo[1] + ", " + patternInfo[2]);
        patterns = new Patterns(patternInfo[0], patternInfo[1], patternInfo[2], null);
        return true;
    }

    /// <summary> 
    /// This method attempts to find the bottom-right alignment pattern in the image. 
    /// It is a bit messy since it's pretty performance-critical and so is written to be 
    /// fast foremost.
    /// </summary>
    internal bool TryFindAlignmentPattern(
        int startX, int startY, int width, int height, // region origin and size of area to search
        float moduleSize,
        DetectorCallback? detectorCallback,
        [NotNullWhen(true)] out AlignmentPattern? pattern)
    {
        pattern = null;
        List<AlignmentPattern> possibleCenters = new(5);
        int[] crossCheckStateCount = new int[3];

        int maxJ = startX + width;
        int middleI = startY + (height >> 1);

        // We are looking for black/white/black modules in 1:1:1 ratio;
        // this tracks the number of black/white/black modules seen so far
        int[] stateCount = new int[3];

        /// <summary> Given a count of black/white/black pixels just seen and an end position,
        /// figures the location of the center of this black/white/black run.
        /// </summary>
        static float? CenterFromEnd(int[] stateCount, int end)
        {
            float result = (end - stateCount[2]) - stateCount[1] / 2.0f;
            if (float.IsNaN(result))
            {
                return null;
            }

            return result;
        }

        /// Returns true iff the proportions of the counts is close enough to the 1/1/1 ratios
        /// used by alignment patterns to be considered a match
        bool FoundPatternCross(int[] stateCount)
        {
            float maxVariance = moduleSize / 2.0f;
            for (int i = 0; i < 3; i++)
            {
                if (Math.Abs(moduleSize - stateCount[i]) >= maxVariance)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary> <p>This is called when a horizontal scan finds a possible alignment pattern. It will
        /// cross check with a vertical scan, and if successful, will see if this pattern had been
        /// found on a previous horizontal scan. If so, we consider it confirmed and conclude we have
        /// found the alignment pattern.</p>
        /// 
        /// </summary>
        /// <param name="stateCount">reading state module counts from horizontal scan
        /// </param>
        /// <param name="i">row where alignment pattern may be found
        /// </param>
        /// <param name="j">end of possible alignment pattern in row
        /// </param>
        /// <returns> {@link AlignmentPattern} if we have found the same pattern twice, or null if not
        /// </returns>
        AlignmentPattern? HandlePossibleCenter(int[] stateCount, int i, int j)
        {
            /// <summary>
            ///   <p>After a horizontal scan finds a potential alignment pattern, this method
            /// "cross-checks" by scanning down vertically through the center of the possible
            /// alignment pattern to see if the same proportion is detected.</p>
            /// </summary>
            /// <param name="startI">row where an alignment pattern was detected</param>
            /// <param name="centerJ">center of the section that appears to cross an alignment pattern</param>
            /// <param name="maxCount">maximum reasonable number of modules that should be
            /// observed in any reading state, based on the results of the horizontal scan</param>
            /// <param name="originalStateCountTotal">The original state count total.</param>
            /// <returns>
            /// vertical center of alignment pattern, or null if not found
            /// </returns>
            float? CrossCheckVertical(int startI, int centerJ, int maxCount, int originalStateCountTotal)
            {
                int maxI = this.Height;
                int[] stateCount = crossCheckStateCount;
                stateCount[0] = 0;
                stateCount[1] = 0;
                stateCount[2] = 0;

                // Start counting up from center
                int i = startI;
                while (i >= 0 && this[centerJ, i] && stateCount[1] <= maxCount)
                {
                    stateCount[1]++;
                    i--;
                }
                // If already too many modules in this state or ran off the edge:
                if (i < 0 || stateCount[1] > maxCount)
                {
                    return null;
                }
                while (i >= 0 && !this[centerJ, i] && stateCount[0] <= maxCount)
                {
                    stateCount[0]++;
                    i--;
                }
                if (stateCount[0] > maxCount)
                {
                    return null;
                }

                // Now also count down from center
                i = startI + 1;
                while (i < maxI && this[centerJ, i] && stateCount[1] <= maxCount)
                {
                    stateCount[1]++;
                    i++;
                }
                if (i == maxI || stateCount[1] > maxCount)
                {
                    return null;
                }
                while (i < maxI && !this[centerJ, i] && stateCount[2] <= maxCount)
                {
                    stateCount[2]++;
                    i++;
                }
                if (stateCount[2] > maxCount)
                {
                    return null;
                }

                int stateCountTotal = stateCount[0] + stateCount[1] + stateCount[2];
                if (5 * Math.Abs(stateCountTotal - originalStateCountTotal) >= 2 * originalStateCountTotal)
                {
                    return null;
                }

                return FoundPatternCross(stateCount) ? CenterFromEnd(stateCount, i) : null;
            }

            int stateCountTotal = stateCount[0] + stateCount[1] + stateCount[2];
            float? centerJ = CenterFromEnd(stateCount, j);
            if (centerJ == null)
            {
                return null;
            } 

            float? centerI = CrossCheckVertical(i, (int)centerJ, 2 * stateCount[1], stateCountTotal);
            if (centerI != null)
            {
                float estimatedModuleSize = (stateCount[0] + stateCount[1] + stateCount[2]) / 3.0f;
                foreach (var center in possibleCenters)
                {
                    // Look for about the same center and module size:
                    if (center.AboutEquals(estimatedModuleSize, centerI.Value, centerJ.Value))
                    {
                        return center.CombineEstimate(centerI.Value, centerJ.Value, estimatedModuleSize);
                    }
                }

                // Hadn't found this before; save it
                var point = new AlignmentPattern(centerJ.Value, centerI.Value, estimatedModuleSize);
                possibleCenters.Add(point);
                detectorCallback?.Invoke(point);
            }

            return null;
        }

        for (int iGen = 0; iGen < height; iGen++)
        {
            // Search from middle outwards
            int i = middleI + ((iGen & 0x01) == 0 ? ((iGen + 1) >> 1) : -((iGen + 1) >> 1));
            stateCount[0] = 0;
            stateCount[1] = 0;
            stateCount[2] = 0;
            int j = startX;

            // Burn off leading white pixels before anything else; if we start in the middle of
            // a white run, it doesn't make sense to count its length, since we don't know if the
            // white run continued to the left of the start point
            while (j < maxJ && !this[j, i])
            {
                j++;
            }

            int currentState = 0;
            while (j < maxJ)
            {
                if (this[j, i])
                {
                    // Black pixel
                    if (currentState == 1)
                    {
                        // Counting black pixels
                        stateCount[1]++;
                    }
                    else
                    {
                        // Counting white pixels
                        if (currentState == 2)
                        {
                            // A winner?
                            if (FoundPatternCross(stateCount))
                            {
                                // Yes
                                AlignmentPattern? confirmed = HandlePossibleCenter(stateCount, i, j);
                                if (confirmed is not null)
                                {
                                    pattern = confirmed;
                                    return true;
                                }
                            }

                            stateCount[0] = stateCount[2];
                            stateCount[1] = 1;
                            stateCount[2] = 0;
                            currentState = 1;
                        }
                        else
                        {
                            stateCount[++currentState]++;
                        }
                    }
                }
                else
                {
                    // White pixel
                    if (currentState == 1)
                    {
                        // Counting black pixels
                        currentState++;
                    }
                    stateCount[currentState]++;
                }
                j++;
            }

            if (FoundPatternCross(stateCount))
            {
                AlignmentPattern? confirmed = HandlePossibleCenter(stateCount, i, maxJ);
                if (confirmed is not null)
                {
                    pattern = confirmed;
                    return true;
                }
            }
        }

        // Nothing we saw was observed and confirmed twice.
        // If we had any guess at all, return it.
        if (possibleCenters.Count != 0)
        {
            pattern = possibleCenters[0];
            return true;
        }

        return false;
    }
}
