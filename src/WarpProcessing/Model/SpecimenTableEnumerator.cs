using System.Collections;
using System.Collections.Generic;

namespace Warp9.Model
{
    public class SpecimenTableEnumerator : IEnumerator, IEnumerator<SpecimenTableRow>
    {
        public SpecimenTableEnumerator(SpecimenTable table)
        {
            this.table = table;
            index = -1;
            numRows = table.Count;
        }

        private readonly SpecimenTable table;
        private int index, numRows;

        public object Current => table.MakeRow(index);
        SpecimenTableRow IEnumerator<SpecimenTableRow>.Current => table.MakeRow(index);

        public bool MoveNext()
        {
            if (index >= numRows - 1)
                return false;

            index++;
            return true;
        }

        public void Reset()
        {
            index = -1;
        }

        public void Dispose()
        {
        }
    }
}
