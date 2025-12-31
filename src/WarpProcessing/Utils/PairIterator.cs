using System;
using System.Collections.Generic;
using System.Text;

namespace Warp9.Utils
{
    public class PairIterator : IEnumerable<(int, int)>
    {
        public PairIterator(int num, bool includeSelf)
        {
            numItems = num;
            this.includeSelf = includeSelf;
        }

        int numItems, itemA = 0, itemB = 1;
        bool includeSelf;


        public IEnumerator<(int, int)> GetEnumerator()
        {
            int offs = includeSelf ? 0 : 1;
            for (int j = 0; j < numItems - 1; j++)
            {
                for (int i = j + offs; i < numItems; i++)
                    yield return (i, j);
            }

            if (includeSelf)
                yield return (numItems - 1, numItems - 1);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
