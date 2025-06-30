using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Themes;
using System.Windows.Documents;
using System.Windows;
using System.Diagnostics;

namespace Warp9.Utils
{
    internal enum FdbState
    {
        Document,
        Paragraph,
        CellParagraph,
        Table,
        TableRow
    };

    public class FlowDocumentBuilder
    {
        public FlowDocumentBuilder()
        {
            Background = ThemesController.GetBrush("Brush.Background");
            BackgroundLight = ThemesController.GetBrush("Brush.BackgroundLight");
            BackgroundHot = ThemesController.GetBrush("Brush.BackgroundHot");
            Border = ThemesController.GetBrush("Brush.Border");
            ForegroundDim = ThemesController.GetBrush("Brush.ForegroundDark");
            ForegroundHot = ThemesController.GetBrush("Brush.BorderHot");
            Foreground = ThemesController.GetBrush("Brush.Foreground");

            fd.FontFamily = new FontFamily("Segoe UI");
            fd.FontSize = 12;
        }

        Brush Background { get; }
        Brush BackgroundLight { get; }
        Brush BackgroundHot { get; }
        Brush Border { get; }
        Brush ForegroundDim { get; }
        Brush ForegroundHot { get; }
        Brush Foreground { get; }

        FlowDocument fd = new FlowDocument();
        FdbState state = FdbState.Document;
        Paragraph? par = null;
        Table? table = null;
        TableRow? tableRow = null;

        public FlowDocument Document => fd;

        public void StartTable(int width = -1)
        {
            if (state != FdbState.Document)
                throw new InvalidOperationException();

            table = new Table();
            state = FdbState.Table;
        }

        public void EndTable()
        {
            if(state != FdbState.Table || table is null)
                throw new InvalidOperationException();

            Document.Blocks.Add(table);
            table = null;
            state = FdbState.Document;
        }

        public void AddColumn(int width = -1)
        {
            if (state != FdbState.Table || table is null)
                throw new InvalidOperationException();

            TableColumn col = new TableColumn();
            if (width != -1) col.Width = new GridLength(width, GridUnitType.Pixel);

            table.Columns.Add(col);
        }

        public void StartRow()
        {
            if(state != FdbState.Table || table is null)
                throw new InvalidOperationException();

            tableRow = new TableRow();
            state = FdbState.TableRow;
            table.RowGroups.Add(new TableRowGroup());
        }

        public void EndRow()
        {
            if (state != FdbState.TableRow || tableRow is null || table is null)
                throw new InvalidOperationException();

            table.RowGroups[0].Rows.Add(tableRow);
            tableRow = null;
            state = FdbState.Table;
        }

        public void StartParagraph()
        {
            if (state == FdbState.Document )
            {
                state = FdbState.Paragraph;
                par = new Paragraph();
                par.Foreground = ForegroundDim;
            }
            else if (state == FdbState.TableRow)
            {
                state = FdbState.CellParagraph;
                par = new Paragraph();
                par.Foreground = ForegroundDim;

                if (tableRow is null)
                    throw new InvalidOperationException();

                TableCell cell = new TableCell(par);
                cell.Padding = new Thickness(2);
                tableRow.Cells.Add(cell);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void EndParagraph()
        {
            if (state == FdbState.Paragraph)
            {
                Document.Blocks.Add(par);
                par = null;
                state = FdbState.Document;
            }
            else if (state == FdbState.CellParagraph)
            {
                par = null;
                state = FdbState.TableRow;
            }
            else
                throw new InvalidOperationException();
        }

        public void AddTitle(string title)
        {
            StartParagraph();
            SpanTitle(title);
            EndParagraph();
        }

        public void AddText(string text)
        {
            StartParagraph();
            SpanNormal(text);
            EndParagraph();
        }

        public void AddEmphText(string text)
        {
            StartParagraph();
            SpanEmph(text);
            EndParagraph();
        }

        public void AddCode(string text)
        {
            StartParagraph();
            SpanCode(text);
            EndParagraph();
        }
            

        public void SpanNormal(string text)
        {
            Run run = new Run(text);
            AddSpan(run);
        }

        public void SpanEmph(string text)
        {
            Run run = new Run(text);
            //run.FontWeight = FontWeights.Bold;
            run.Foreground = Foreground;
            AddSpan(run);
        }

        public void SpanCode(string text)
        {
            Run run = new Run(text);
            run.FontFamily = new FontFamily("Cascadia Mono Light");
            AddSpan(run);
        }

        public void SpanTitle(string text)
        {
            Run run = new Run(text);
            run.FontWeight = FontWeights.Bold;
            run.Foreground = ForegroundDim;
            run.FontSize = 16;
            AddSpan(run);
        }

        public void SpanLink(string text, string uri)
        {
            Hyperlink link = new Hyperlink();
            link.Inlines.Add(text);
            link.NavigateUri = new Uri(uri);
            AddSpan(link);
        }

        private void AddSpan(Inline span)
        {
            if (state == FdbState.Paragraph && par is not null)
            {
                par.Inlines.Add(span);
            }
            else if (state == FdbState.CellParagraph && par is not null)
            {
                par.Inlines.Add(span);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

    }
}
