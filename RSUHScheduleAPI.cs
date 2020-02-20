using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Alfie_Host
{
    static class RSUHScheduleAPI
    {
        const string RSUHURL = "https://www.rsuh.ru/";
        private static readonly HttpClient client = new HttpClient();
        public class Period
        {
            private byte Number;
            private byte Group;
            private byte Subgroup;
            private string Classroom;
            private string Subject;
            private string Type;
            private string Lector;
            private byte CurrentWritePosition;

            public string GetField(int index)
            {
                switch (index)
                {
                    case 0:
                        if (Number == 0)
                            return "-";
                        else
                            return Number.ToString();
                    case 1:
                        if (Group == 0)
                            return "-";
                        else
                            return Group.ToString();
                    case 2:
                        if (Subgroup == 0)
                            return "-";
                        else
                            return Subgroup.ToString();
                    case 3:
                        return Classroom;
                    case 4:
                        return Subject;
                    case 5:
                        return Type;
                    case 6:
                        return Lector;
                    default:
                        return null;
                }
            }

            public void WritePeriodField(string value)
            {
                switch (CurrentWritePosition)
                {
                    case 0:
                        if (value == "-")
                            Number = 0;
                        else
                            Number = byte.Parse(value);
                        break;
                    case 1:
                        if (value == "-")
                            Group = 0;
                        else
                            Group = byte.Parse(value);
                        break;
                    case 2:
                        if (value == "-")
                            Subgroup = 0;
                        else
                            Subgroup = byte.Parse(value);
                        break;
                    case 3:
                        Classroom = value;
                        break;
                    case 4:
                        Subject = value;
                        break;
                    case 5:
                        Type = value;
                        break;
                    case 6:
                        Lector = value;
                        break;
                    default:
                        return;
                }
                CurrentWritePosition++;
            }
        }



        public class Day
        {
            public DateTime Date;
            public List<Period> Periods;
        }

        public static async Task<List<Day>> GetSchedule(int group, DateTime start, DateTime end)
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    #region Sensless, but required to work
                    { "formob", "" },
                    { "kyrs", "" },
                    { "cafzn", "" },
                    #endregion
                    { "caf", group.ToString() },
                    { "srok", "interval" },
                    { "sdate_year", start.Year.ToString() },
                    { "sdate_month", start.Month.ToString("D2") },
                    { "sdate_day", start.Day.ToString("D2") },
                    { "fdate_year", end.Year.ToString() },
                    { "fdate_month", end.Month.ToString("D2") },
                    { "fdate_day", end.Day.ToString("D2") }
                });
            var response = await client.PostAsync(RSUHURL + "rasp/3.php", content);

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml("<html><body>" + await response.Content.ReadAsStringAsync() + "</body></html>");
            var table = document.DocumentNode.SelectSingleNode("/html/body/table");
            if (table == null)
                return null;
            var rows = table.SelectNodes("tr");
            rows.Remove(0);

            int rowsTillDayEnds = 0;
            List<Day> result = new List<Day>();
            foreach (var row in rows)
            {
                var columns = row.SelectNodes("td");
                if (rowsTillDayEnds == 0)
                {
                    result.Add(new Day
                    {
                        Date = DateTime.ParseExact(columns[0].InnerText.Substring(0, 10), "dd.MM.yyyy", CultureInfo.InvariantCulture),
                        Periods = new List<Period>()
                    });
                    for (int i = 0; i < int.Parse(columns[0].GetAttributeValue("rowspan", "1")); i++)
                        result.Last().Periods.Add(new Period());
                    rowsTillDayEnds = int.Parse(columns[0].GetAttributeValue("rowspan", "1"));
                    columns.Remove(0);
                }
                for (int i = 0; i < columns.Count; i++)
                {
                    int g = result.Last().Periods.Count - rowsTillDayEnds;
                    int span = int.Parse(columns[i].GetAttributeValue("rowspan", "1"));
                    for (int j = 0; j < span; j++)
                        result.Last().Periods[g + j].WritePeriodField(columns[i].InnerText);
                }
                rowsTillDayEnds--;
            }
            return result;
        }

        public static async Task<Dictionary<string, int>> GetGroups()
        {
            var tasks = new List<Task<List<KeyValuePair<string, int>>>>();
            var result = new Dictionary<string, int>();
            byte[] grades = { 5, 5, 4, 4, 2 };
            char[] forms = { 'Д', 'В', 'З', '2', 'М' };
            for (byte i = 0; i < 5; i++)
                for (byte j = 1; j <= grades[i]; j++)
                    tasks.Add(_getGroups(j, forms[i]));
            await Task.WhenAll(tasks);
            foreach (var task in tasks)
                foreach(var group in task.Result)
                    result.Add(group.Key, group.Value);
            return result;
        }

        private static async Task<List<KeyValuePair<string, int>>> _getGroups(byte grade, char form)
        {
            List<KeyValuePair<string, int>> result = new List<KeyValuePair<string, int>>();
            string formstring = "";
            switch (form)
            {
                case 'Д':
                    formstring = "дневн.";
                    break;
                case 'В':
                    formstring = "вечерн.";
                    break;
                case 'З':
                    formstring = "заочн.";
                    break;
                case '2':
                    formstring = "второе об.";
                    break;
                case 'М':
                    formstring = "магистр.";
                    break;
            }
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "formob", form.ToString() },
                    { "kyrs", grade.ToString() }
                });
            var response = await client.PostAsync(RSUHURL + "rasp/2.php", content);
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml("<html><body>" + await response.Content.ReadAsStringAsync() + "</body></html>");
            var options = document.DocumentNode.SelectNodes("//option");
            if (options == null)
                return null;
            foreach (var node in options)
                result.Add(new KeyValuePair<string, int>($"{node.InnerText}, {grade} курс, {formstring}", int.Parse(node.GetAttributeValue("value", ""))));
            return result;
        }

        private static string NormalizeLength(string str, int length)
        {
            if (str.Length > length)
                return str.Substring(0, length - 1).TrimEnd(' ') + '…';
            else
                return str;
        }

        public static Image ScheduleToImage(List<Day> schedule, Color textColor, Color backColor)
        {
            const int LongStringsMaxLength = 30;
            Font headersFont = new Font("Helvetica", 32, FontStyle.Underline);
            Font subHeadersFont = new Font("Helvetica", 32, FontStyle.Bold);
            Font textFont = new Font("Helvetica", 32, FontStyle.Regular);
            string[] subHeaders = new string[] { "#", "Гр.", "П/Гр.", "Ауд.", "Предмет", "Тип", "Лектор" };
            float tmp1 = 0;

            Image img = new Bitmap(1, 1);
            Graphics drawing = Graphics.FromImage(img);

            SizeF tableSize = new SizeF(0, 0);
            foreach (var day in schedule)
            {
                tableSize.Height += drawing.MeasureString(day.Date.ToShortDateString(), headersFont).Height;
                tableSize.Height += drawing.MeasureString(day.Periods[0].GetField(0), textFont).Height * day.Periods.Count;
                tableSize.Height += drawing.MeasureString(subHeaders[0], subHeadersFont).Height;
            }
            for (int i = 0; i < 7; i++)
            {
                if (tmp1 < drawing.MeasureString(subHeaders[i], subHeadersFont).Width)
                    tmp1 = drawing.MeasureString(subHeaders[i], subHeadersFont).Width;
                foreach (var day in schedule)
                    foreach (var period in day.Periods)
                    {
                        if (tmp1 < drawing.MeasureString(NormalizeLength(period.GetField(i), LongStringsMaxLength), textFont).Width)
                            tmp1 = drawing.MeasureString(NormalizeLength(period.GetField(i), LongStringsMaxLength), textFont).Width;
                    }
                tableSize.Width += tmp1;
                tmp1 = 0;
            }
            img.Dispose();
            drawing.Dispose();

            img = new Bitmap((int)tableSize.Width, (int)tableSize.Height);
            drawing = Graphics.FromImage(img);
            drawing.Clear(backColor);
            Brush textBrush = new SolidBrush(textColor);
            //drawing.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            float x = 0;
            float y = 0;
            for (int i = 0; i < 7; i++)
            {
                foreach (var day in schedule)
                {
                    if (i == 0)
                        drawing.DrawString(day.Date.ToShortDateString() + ", " + day.Date.DayOfWeek, headersFont, textBrush, x, y);
                    y += drawing.MeasureString(day.Date.ToShortDateString() + ", " + day.Date.DayOfWeek, headersFont).Height;
                    drawing.DrawString(subHeaders[i], subHeadersFont, textBrush, x, y);
                    y += drawing.MeasureString(subHeaders[i], subHeadersFont).Height;
                    if (tmp1 < drawing.MeasureString(subHeaders[i], subHeadersFont).Width)
                        tmp1 = drawing.MeasureString(subHeaders[i], subHeadersFont).Width;
                    foreach (var period in day.Periods)
                    {
                        drawing.DrawString(NormalizeLength(period.GetField(i), LongStringsMaxLength), textFont, textBrush, x, y);
                        y += drawing.MeasureString(NormalizeLength(period.GetField(i), LongStringsMaxLength), textFont).Height;
                        if (tmp1 < drawing.MeasureString(NormalizeLength(period.GetField(i), LongStringsMaxLength), textFont).Width)
                            tmp1 = drawing.MeasureString(NormalizeLength(period.GetField(i), LongStringsMaxLength), textFont).Width;
                    }
                }
                y = 0;
                x += tmp1;
                tmp1 = 0;
            }
            drawing.Save();

            textBrush.Dispose();
            drawing.Dispose();

            return img;
        }
    }
}
