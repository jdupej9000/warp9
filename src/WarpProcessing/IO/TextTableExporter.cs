using System;
using System.CodeDom;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Warp9.Data;
using Warp9.Model;

namespace Warp9.IO
{
    public class TextTableExporter
    {
        public TextTableExporter(ITable table)
        {
            this.table = table;
        }

        ITable table;

        public bool WriteColumnHeaders { get; set; } = false;
        public bool UseLocalCulture { get; set; } = false;
        public char ColumnDelimiter { get; set; } = ',';
        public bool WindowsLineEnding { get; set; } = false;
        public bool OmitSpecialColumns { get; set; } = true;
        public string NullText { get; set; } = string.Empty;
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public void ExportAs(string filename)
        {
            List<Action<StreamWriter, object>?> serializers = MakeSerializers();

            using FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
            using StreamWriter sw = new StreamWriter(fs, Encoding);

            if (WriteColumnHeaders)
                WriteColumns(sw);

            string eof = WindowsLineEnding ? "\r\n" : "\n";
            for (int i = 0; i < table.Rows; i++)
            {
                WriteRow(sw, i, serializers);
                sw.Write(eof);
            }
        }

        private List<Action<StreamWriter, object>?> MakeSerializers()
        {
            List<Action<StreamWriter, object>?> ret = new List<Action<StreamWriter, object>?>();

            for (int i = 0; i < table.Columns; i++)
            {
                if (table.ColumnType(i) == typeof(double))
                {
                    if (UseLocalCulture)
                        ret.Add(_Write_Double);
                    else
                        ret.Add(_Write_Double_Invariant);
                }
                else if (table.ColumnType(i) == typeof(float))
                {
                    if (UseLocalCulture)
                        ret.Add(_Write_Float);
                    else
                        ret.Add(_Write_Float_Invariant);
                }
                else if (MustOmitColumn(i))
                {
                    ret.Add(null);
                }
                else
                {
                    ret.Add(_Write_Direct);
                }                
            }

            return ret;
        }

        private void WriteColumns(StreamWriter sw)
        {
            int nc = table.Columns;
            if (table.HasColumnNames)
            {
                for (int i = 0; i < nc; i++)
                {
                    if (!MustOmitColumn(i))
                        sw.Write(table.ColumnName(i));

                    // This is not correct if the last column must be omittedd.
                    if (i < nc - 1)
                        sw.Write(ColumnDelimiter);
                }
            }
            else
            {
                for (int i = 0; i < nc; i++)
                {
                    if (!MustOmitColumn(i))
                        sw.Write($"col{i+1}");

                    // This is not correct if the last column must be omittedd.
                    if (i < nc - 1)
                        sw.Write(ColumnDelimiter);
                }
            }
        }

        private bool MustOmitColumn(int idx)
        {
            return OmitSpecialColumns && table.ColumnType(idx) == typeof(ProjectReferenceLink);
        }

        private void WriteRow(StreamWriter sw, int row, List<Action<StreamWriter, object>?> serializers)
        {
            int nc = table.Columns;

            for (int i = 0; i < nc; i++)
            {
                Action<StreamWriter, object>? ser = serializers[i];
                if (ser == null)
                    continue;

                object? val = table.GetAt(i, row);

                if (val is null)
                {
                    sw.Write(NullText);
                }
                else
                {
                    ser.Invoke(sw, val);
                }

                // This is not correct if the last column must be omittedd.
                if(i < nc - 1)
                    sw.Write(ColumnDelimiter);
            }
        }



        private static void _Write_Float(StreamWriter wr, object x)
        {
            wr.Write(((float)x).ToString());
        }

        private static void _Write_Float_Invariant(StreamWriter wr, object x)
        {
            wr.Write(((float)x).ToString(CultureInfo.InvariantCulture));
        }

        private static void _Write_Double(StreamWriter wr, object x)
        {
            wr.Write(((double)x).ToString());
        }

        private static void _Write_Double_Invariant(StreamWriter wr, object x)
        {
            wr.Write(((double)x).ToString(CultureInfo.InvariantCulture));
        }

        private static void _Write_Direct(StreamWriter wr, object x)
        {
            wr.Write(x.ToString());
        }
    }
}
