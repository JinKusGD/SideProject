using System.Collections.Generic;
using System.Text;

public static class CsvParser
{
    public static List<List<string>> Parse(string csv)
    {
        List<List<string>> rows = new List<List<string>>();
        List<string> row = new List<string>();
        StringBuilder cell = new StringBuilder();
        bool isInQuotes = false;

        for (int i = 0; i < csv.Length; i++)
        {
            char c = csv[i];

            if (isInQuotes)
            {
                if (c != '"')
                {
                    cell.Append(c);
                    continue;
                }

                bool isEscapedQuote = i + 1 < csv.Length && csv[i + 1] == '"';

                if (isEscapedQuote)
                {
                    cell.Append('"');
                    i++;
                }
                else
                {
                    isInQuotes = false;
                }

                continue;
            }

            switch (c)
            {
                case '"':
                    isInQuotes = true;
                    break;
                case ',':
                    row.Add(cell.ToString());
                    cell.Clear();
                    break;
                case '\r':
                    break;
                case '\n':
                    row.Add(cell.ToString());
                    cell.Clear();
                    rows.Add(row);
                    row = new List<string>();
                    break;
                default:
                    cell.Append(c);
                    break;
            }
        }

        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            rows.Add(row);
        }

        return rows;
    }
}
