namespace Lyt.QrCode.Image;

internal sealed partial class BitMatrixImage
{
    internal bool TryDetect(
        DetectorCallback? detectorCallback, [NotNullWhen(true)] out DetectorResult? detectorResult)
    {
        detectorResult = null; 
        return this.TryFindPatterns(detectorCallback, out var patterns);
    }

    // private static readonly EstimatedModuleComparator moduleComparator = new EstimatedModuleComparator();

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

        void DoClearCrossCheckStateCounts() => Array.Clear(crossCheckStateCount, 0, crossCheckStateCount.Length);

        /// Returns true iff the proportions of the counts is close enough to the 1/1/3/1/1 ratios
        /// used by finder patterns to be considered a match
        bool FoundPatternCross()
        {
            int totalModuleSize = 0;
            for (int i = 0; i < 5; i++)
            {
                int count = stateCount[i];
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

            int moduleSize = (totalModuleSize << INTEGER_MATH_SHIFT) / 7;
            int maxVariance = moduleSize / 2;

            // Allow less than 50% variance from 1-1-3-1-1 proportions
            return Math.Abs(moduleSize - (stateCount[0] << INTEGER_MATH_SHIFT)) < maxVariance &&
                   Math.Abs(moduleSize - (stateCount[1] << INTEGER_MATH_SHIFT)) < maxVariance &&
                   Math.Abs(3 * moduleSize - (stateCount[2] << INTEGER_MATH_SHIFT)) < 3 * maxVariance &&
                   Math.Abs(moduleSize - (stateCount[3] << INTEGER_MATH_SHIFT)) < maxVariance &&
                   Math.Abs(moduleSize - (stateCount[4] << INTEGER_MATH_SHIFT)) < maxVariance;
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

            int stateCountTotal =
                stateCount[0] + stateCount[1] + stateCount[2] + stateCount[3] + stateCount[4];
            float? centerJ = CenterFromEndState(j);
            if (centerJ == null)
            {
                return false;
            }

            // TODO : CONTINUE HERE ! 

            //float? centerI = CrossCheckVertical(i, (int)centerJ.Value, stateCount[2], stateCountTotal);
            //if (centerI != null)
            //{
            //    // Re-cross check
            //    centerJ = CrossCheckHorizontal(
            //        (int)centerJ.Value, (int)centerI.Value, stateCount[2], stateCountTotal);
            //    if (centerJ != null && CrossCheckDiagonal((int)centerI, (int)centerJ))
            //    {
            //        float estimatedModuleSize = stateCountTotal / 7.0f;
            //        bool found = false;
            //        for (int index = 0; index < possibleCenters.Count; index++)
            //        {
            //            var center = possibleCenters[index];
            //            // Look for about the same center and module size:
            //            if (center.AboutEquals(estimatedModuleSize, centerI.Value, centerJ.Value))
            //            {
            //                possibleCenters.RemoveAt(index);
            //                possibleCenters.Insert(index, center.CombineEstimate(centerI.Value, centerJ.Value, estimatedModuleSize));

            //                found = true;
            //                break;
            //            }
            //        }
            //        if (!found)
            //        {
            //            var point = new Pattern(centerJ.Value, centerI.Value, estimatedModuleSize);

            //            possibleCenters.Add(point);

                        detectorCallback?.Invoke(new ResultPoint(0,0) /*  point */ );
            //            //if (resultPointCallback != null)
            //            //{
            //            //    resultPointCallback(point);
            //            //}
            //        }

            //        return true;
            //    }
            //}

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
                bool pixelIsBlack = this[i, j];
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
                            if (FoundPatternCross())
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

            if (FoundPatternCross())
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

        // TODO 
        var patternInfo = new Pattern[3]; //  SelectBestPatterns();
        if (patternInfo == null)
        {
            return false;
        }

        // TODO : Why In REsult Point ? 
        // ResultPoint.OrderBestPatterns(patternInfo);

        patterns = new Patterns(patternInfo[0], patternInfo[1], patternInfo[2]);
        return true;
    }
}
