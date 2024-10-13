namespace Pooshit.Scripting.Extensions {
    public static class ArrayExtensions {

		/// <summary>
        /// Gets the hash code for the contents of the array since the default hash code
        /// for an array is unique even if the contents are the same.
        /// </summary>
        /// <remarks>
        /// See Jon Skeet (C# MVP) response in the StackOverflow thread 
        /// http://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode
        /// </remarks>
        /// <param name="array">The array to generate a hash code for.</param>
        /// <returns>The hash code for the values in the array.</returns>
        public static int GetHashCode<T>(this T[] array)
        {
            // if non-null array then go into unchecked block to avoid overflow
            if (array != null)
            {
                unchecked
                {
                    int hash = 17;

                    // get hash code for all items in array
                    foreach (T item in array)
                    {
                        hash = hash * 23 + (item != null ? item.GetHashCode() : 0);
                    }

                    return hash;
                }
            }

            // if null, hash code is zero
            return 0;
        }

        /// <summary>
        /// Compares the contents of both arrays to see if they are equal. This depends on 
        /// typeparameter T having a valid override for Equals().
        /// </summary>
        /// <param name="firstArray">The first array to compare.</param>
        /// <param name="secondArray">The second array to compare.</param>
        /// <returns>True if firstArray and secondArray have equal contents.</returns>
        public static bool Equals<T>(T[] firstArray, T[] secondArray)
        {
            // if same reference or both null, then equality is true
            if (ReferenceEquals(firstArray, secondArray))
                return true;

            // otherwise, if both arrays have same length, compare all elements
            if (firstArray != null && secondArray != null &&
                (firstArray.Length == secondArray.Length))
            {
                for (int i = 0; i < firstArray.Length; i++)
                {
                    // if any mismatch, not equal
                    if (!Equals(firstArray[i], secondArray[i]))
                        return false;
                }

                // if no mismatches, equal
                return true;
            }

            // if we get here, they are not equal
            return false;
        }
	}
}