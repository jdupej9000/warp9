using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Utils
{
    public class CsvImporter : INotifyPropertyChanged, IUntypedTableProvider
    {
        private CsvImporter(string[] lines)
        {
            this.lines = lines;
        }

        private string[] lines;

        public event PropertyChangedEventHandler? PropertyChanged;
        private bool delimComma = true, delimTab = false, delimSpace = false, delimSemicolon = false;
        private bool ignoreFirst = false, commaDecimal = false, ignoreEmpty = true;

        public bool UseCommaCellDelimiter
        {
            get { return delimComma; }
            set { delimComma = value; Update(); }
        }

        public bool UseTabCellDelimiter
        {
            get { return delimTab; }
            set { delimTab = value; Update(); }
        }

        public bool UseSemicolonCellDelimiter
        {
            get { return delimSemicolon; }
            set { delimSemicolon = value; Update(); }
        }

        public bool UseSpaceCellDelimiter
        {
            get { return delimSpace; }
            set { delimSpace = value; Update(); }
        }

        public bool IgnoreFirstRow
        {
            get { return ignoreFirst; }
            set { ignoreFirst = value; Update(); }
        }

        public bool UseCommaAsDecimalDelimiter
        {
            get { return commaDecimal; }
            set { commaDecimal = value; Update(); }
        }

        public bool IgnoreEmptyRows
        {
            get { return ignoreEmpty; }
            set { ignoreEmpty = value; Update(); }
        }

        public IEnumerable<string[]> ParsedData => Parse();

        private void Update()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ParsedData)));
        }

        private IEnumerable<string[]> Parse()
        {
            List<char> separators = new List<char>();
            if (delimComma) separators.Add(',');
            if (delimTab) separators.Add('\t');
            if (delimSpace) separators.Add(' ');
            if (delimSemicolon) separators.Add(';');

            if (separators.Count == 0)
                yield break;

            char[] seps = separators.ToArray();
            bool isFirst = true;

            List<string> lineElements = new List<string>();
            foreach (string line in lines)
            {
                if (ignoreFirst && isFirst)
                {
                    isFirst = false;
                    continue;
                }
                
                if (ignoreEmpty && line.Length == 0)
                    continue;

                int pos = 0;

                while (pos < line.Length)
                {
                    if (line[pos] == '\"')
                    {
                        pos++;
                        int quoteEnd = line.IndexOf('\"', pos);
                        if (quoteEnd == -1) quoteEnd = line.Length;

                        lineElements.Add(line.Substring(pos, quoteEnd - pos));
                        pos = quoteEnd + 1;
                    }
                    else
                    {
                        int cellEnd = line.IndexOfAny(seps, pos);
                        if (cellEnd == -1) cellEnd = line.Length;
                        lineElements.Add(line.Substring(pos, cellEnd - pos));
                        pos = cellEnd + 1;
                    }
                }

                yield return lineElements.ToArray();
                lineElements.Clear();
            }
        }

        public static CsvImporter Create(string path)
        {
            return new CsvImporter(File.ReadAllLines(path));
        }

    }
}
