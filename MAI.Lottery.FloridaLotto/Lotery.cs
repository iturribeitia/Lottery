using System;
using System.Net;
using System.IO;
using System.Data;
using System.Xml;
using System.Text;
using System.Web;
using MAI.Lottery.FloridaLotto;
namespace MAI.Lottery.FloridaLotto
{
    // todo: restructure this project to use more object oriented technique like interface, parent class and child clas implementhing hineritance.

    /// <summary>
    /// Class for extract Florida lotto results ,analyze results and produce Trend analysis.
    /// </summary>
    public class FloridaLotto
    {
        #region enumeration definitions
        enum GameParameters
        {
            TotalNumbers = 53,
            WinningNumbers = 6
        }
        #endregion

        #region Fields
        private Uri _SourceURI = new Uri("http://www.flalottery.com/exptkt/l6.htm");
        private Uri _ProxyURI;
        private string _UserName;
        private string _Password;
        private DateFilter _MyDateFilter;
        //private DateTime  _FilterDateFrom;
        //private DateTime  _FilterDateTo;
        private string[] _Myrecords;
        private Boolean _LoadDataFromXML;
        private DayOfWeek[] _DrawingsDays = new DayOfWeek[2] { DayOfWeek.Saturday, DayOfWeek.Wednesday }; //http://www.flalottery.com/inet/media-tvdrawingsMain.do
        private int _TotalDrawings;
        private String _XMLDataPath = AppDomain.CurrentDomain.BaseDirectory + "App_Data\\XML\\";
        #endregion

        #region Private DataTables
        private DataTable WinningNumbers = new DataTable("WinningNumbers");
        public DataTable WinningStatistics = new DataTable("WinningStatistics");
        private DataTable Frequencies = new DataTable("Frequencies");
        #endregion

        #region readonly variables
        /// <summary>
        /// Path for the data in this class.
        /// </summary>
        //private readonly String XMLDataPath = AppDomain.CurrentDomain.BaseDirectory.ToString() + "Data\\";
        #endregion

        #region Constants
        /// <summary>
        /// Path for the data in this class.
        /// </summary>
        //private const string DataPath = @"C:\_Marcos\Lottery\Lottery\Data\";
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks></remarks>
        public FloridaLotto()
        {
            // set the default value because it is needed.
            _MyDateFilter.From = DateTime.MinValue;
            _MyDateFilter.To = DateTime.MaxValue;
            //_SourceURI = new Uri("http://www.flalottery.com/exptkt/l6.htm");
            //ProxyURI  = "http://phxpsce.aexp.com:8080";
            //UserName  = "bdave0";
            //Password  = "js5077";
            //OLotto.FilterDateTo = DateTime.Today;
            //OLotto.FilterDateFrom = OLotto.FilterDateTo.AddMonths(-6);
            // Call the process
            //Process();
        }


        /// <summary>
        /// Get and process all the information from the results web page.
        /// </summary>
        public Boolean Process()
        {
            if (_SourceURI == null || _SourceURI.OriginalString.Length == 0)
            {
                throw new ApplicationException("Specify the URI of the resource to retrieve.");
            }
            if (this._LoadDataFromXML)
            {
                GetWinningNumbersFromXML();
            }
            else
            {
                GetWinningNumbersFromWebSite();
            }
            WriteResultsToXML(_XMLDataPath + @"Results.XML");
            CreateTablesStructures();
            // LoadResultsIntoDataTable Load Result In fields and properties.
            LoadResultsIntoDataTable();
            ComputeResults();
            WriteDataTableToXml(WinningNumbers, _XMLDataPath);
            WriteDataTableToXml(WinningStatistics, _XMLDataPath);
            WriteDataTableToXml(Frequencies, _XMLDataPath);
            ReadXmlToDataTable(ref WinningNumbers, _XMLDataPath + @"WinningNumbers.XML", _XMLDataPath + @"WinningNumbers.XSD");
            return true;
        }

        /// <summary>
        /// Read HTML content from the SourceURI property.
        /// </summary>
        private void GetWinningNumbersFromWebSite()
        {
            WebClient MyWebClient = new WebClient();
            //MyWebClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            if (_ProxyURI == null || _ProxyURI.OriginalString.Length == 0)
            {
                // No proxy server.
            }
            else
            {
                // Set the proxy server.
                WebProxy MyWebProxy = new WebProxy(_ProxyURI);
                // check the credentials.
                if (_UserName == null || _UserName.Length == 0)
                {
                    // no credential where passed.
                }
                else
                {
                    // set the credentials.
                    NetworkCredential MyNetworkCredential = new NetworkCredential(_UserName, _Password);
                    MyWebProxy.Credentials = MyNetworkCredential;
                }

                MyWebClient.Proxy = MyWebProxy;
                //throw new ApplicationException("Specify the URI of the resource to retrieve.");
            }
            // Read the html content and put in string s.
            string s;
            try
            {
                Stream data = MyWebClient.OpenRead(_SourceURI);
                StreamReader reader = new StreamReader(data);
                s = reader.ReadToEnd();
                data.Close();
                reader.Close();
            }
            catch (Exception e)
            {
                throw (e);    // Rethrowing exception e
            }
            // strip HTML as TEXT.
            s = StripHTML(s);
            // Format text as needed for process.
            FormatText(ref s);
            _Myrecords = s.Split("~".ToCharArray());
        }
      
        /// <summary>
        /// Read the results from the table and store results in the array and perform some math operations.
        /// </summary>
        private void ComputeResults()
        {
            try
            {
                for (int Number = 1; Number <= (int)GameParameters.TotalNumbers; Number++)
                {
                    string MyFilter = "Number = '" + Number.ToString() + "'";
                    string MySort = "Date ASC";

                    DataRow[] MyDataRows = WinningNumbers.Select(MyFilter, MySort);

                    if (MyDataRows.Length == 0)
                    {
                        // no winning Statistics for this number.
                        DataRow MyDataRowToUptate = WinningStatistics.NewRow();
                        MyDataRowToUptate["Number"] = Number;
                        MyDataRowToUptate["TotalWins"] = 0;
                        MyDataRowToUptate["LastWin"] = DBNull.Value;
                        MyDataRowToUptate["AverageDays"] = DBNull.Value;
                        WinningStatistics.Rows.Add(MyDataRowToUptate);
                        continue;
                    }
                    foreach (DataRow MyDataRow in MyDataRows)
                    {
                        // Find the record in WinningStatistics
                        DataRow MyDataRowToUptate = WinningStatistics.Rows.Find(MyDataRow["Number"]);
                        // if record does not exist return null.
                        if (MyDataRowToUptate == null) // Insert WinningStatistics
                        {
                            MyDataRowToUptate = WinningStatistics.NewRow();
                            MyDataRowToUptate["Number"] = Number;
                            MyDataRowToUptate["TotalWins"] = 1;
                            MyDataRowToUptate["LastWin"] = (DateTime)MyDataRow["Date"];
                            MyDataRowToUptate["AverageDays"] = DBNull.Value;
                            WinningStatistics.Rows.Add(MyDataRowToUptate);
                        }
                        else // Update WinningStatistics.
                        {
                            // Calculate day difference.
                            DateTime MyFrom = (DateTime)MyDataRowToUptate["LastWin"];
                            DateTime MyTo = (DateTime)MyDataRow["Date"];
                            TimeSpan MyTimeSpan = MyTo.Subtract(MyFrom);
                            DataRow FrequenciesRow = Frequencies.NewRow();

                            FrequenciesRow["Number"] = Number;
                            FrequenciesRow["From"] = MyFrom;
                            FrequenciesRow["To"] = MyTo;
                            FrequenciesRow["Days"] = MyTimeSpan.Days;

                            Frequencies.Rows.Add(FrequenciesRow);
                            Frequencies.AcceptChanges();

                            MyDataRowToUptate["TotalWins"] = (int)MyDataRowToUptate["TotalWins"] + 1;
                            MyDataRowToUptate["LastWin"] = (DateTime)MyDataRow["Date"];

                        }
                        //Acept changes.
                        WinningStatistics.AcceptChanges();
                    }
                    //calculate the average in WinningStatistics
                    string MyExpression = @"AVG(Days)";
                    MyFilter = @"Number=" + Number.ToString();
                    // find the record in WinningStatistics
                    DataRow MyWinningStatisticsRow = WinningStatistics.Rows.Find(Number);
                    MyWinningStatisticsRow["AverageDays"] = Frequencies.Compute(MyExpression, MyFilter);
                    WinningStatistics.AcceptChanges();
                }
                // Update WinningStatistics Days without win, and probable win.
                DateTime NextDrawingDate = GetNextDrawingDate(DateTime.Today);
                // check for the next
                TimeSpan MyTimeSpam;
                foreach (DataRow MyDataRow in WinningStatistics.Rows)
                {
                    if (MyDataRow["LastWin"] != System.DBNull.Value)
                    {
                        MyTimeSpam = NextDrawingDate.Subtract((DateTime)MyDataRow["LastWin"]);
                        MyDataRow["DaysWithoutWin"] = MyTimeSpam.Days;
                    }

                    //ProbableWinIn
                    if (MyDataRow["AverageDays"] != System.DBNull.Value && MyDataRow["LastWin"] != System.DBNull.Value)
                    {

                        MyDataRow["ProbableWinIn"] = Convert.ToDateTime(MyDataRow["LastWin"]).AddDays(Math.Abs((int)MyDataRow["AverageDays"]));
                        // Compare if is lessthan Today then Today.
                        if (Convert.ToDateTime(MyDataRow["ProbableWinIn"]).CompareTo(NextDrawingDate) < 0)
                        {
                            MyDataRow["ProbableWinIn"] = NextDrawingDate;
                        }
                    }
                    // If ProbableWinIn is null use today.
                    if (MyDataRow["ProbableWinIn"] == System.DBNull.Value)
                    {
                        MyDataRow["ProbableWinIn"] = NextDrawingDate;
                    }
                    else // Verify if MyDataRow["ProbableWinIn"] is a DrawingDate.
                    {
                        MyDataRow["ProbableWinIn"] = GetNextDrawingDate(Convert.ToDateTime(MyDataRow["ProbableWinIn"].ToString()));
                    }
                    // Calculate Winning average = Wining / Sorteos
                    MyDataRow["Winning average"] = GetWinningAverage(Convert.ToDecimal(MyDataRow["TotalWins"]), Convert.ToDecimal(_TotalDrawings));
                }
                WinningStatistics.AcceptChanges();
            }
            catch (Exception e)
            {

                throw e;
            }
        }
     
        /// <summary>
        /// Strip HTML Tags keeping the format.
        /// </summary>
        /// <returns>String Striped from html tags.</returns>
        private string StripHTML(string source)
        {
            try
            {

                string result;
                

                // Remove HTML Development formatting
                // Replace line breaks with space
                // because browsers inserts space
                result = source.Replace("\r", " ");
                // Replace line breaks with space
                // because browsers inserts space
                result = result.Replace("\n", " ");
                // Remove step-formatting
                result = result.Replace("\t", string.Empty);
                // Remove repeating speces becuase browsers ignore them
                result = System.Text.RegularExpressions.Regex.Replace(result, @"( )+", " ");
                // Remove the header (prepare first by clearing attributes)
                result = System.Text.RegularExpressions.Regex.Replace(result, @"<( )*head([^>])*>", "<head>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, @"(<( )*(/)( )*head( )*>)", "</head>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, "(<head>).*(</head>)", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // remove all scripts (prepare first by clearing attributes)
                result = System.Text.RegularExpressions.Regex.Replace(result, @"<( )*script([^>])*>", "<script>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, @"(<( )*(/)( )*script( )*>)", "</script>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                //result = System.Text.RegularExpressions.Regex.Replace(result, @"(<script>)([^(<script>\.</script>)])*(</script>)", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, @"(<script>).*(</script>)", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // remove all styles (prepare first by clearing attributes)
                result = System.Text.RegularExpressions.Regex.Replace(result, @"<( )*style([^>])*>", "<style>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, @"(<( )*(/)( )*style( )*>)", "</style>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, "(<style>).*(</style>)", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // insert tabs in spaces of <td> tags
                result = System.Text.RegularExpressions.Regex.Replace(result, @"<( )*td([^>])*>", "\t", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // insert line breaks in places of <BR> and <LI> tags
                result = System.Text.RegularExpressions.Regex.Replace(result, @"<( )*br( )*>", "\r", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, @"<( )*li( )*>", "\r", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // insert line paragraphs (double line breaks) in place
                // if <P>, <DIV> and <TR> tags
                result = System.Text.RegularExpressions.Regex.Replace(result, @"<( )*div([^>])*>", "\r\r", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, @"<( )*tr([^>])*>", "\r\r", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, @"<( )*p([^>])*>", "\r\r", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // Remove remaining tags like <a>, links, images,
                // comments etc - anything thats enclosed inside < >
                result = System.Text.RegularExpressions.Regex.Replace(result, @"<[^>]*>", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // replace special characters:
                result = System.Text.RegularExpressions.Regex.Replace(result, @"&nbsp;", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, @"&bull;", " * ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, @"&lsaquo;", "<", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, @"&rsaquo;", ">", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, @"&trade;", "(tm)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, @"&frasl;", "/", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, @"<", "<", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, @">", ">", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, @"&copy;", "(c)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, @"&reg;", "(r)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // Remove all others. More can be added, see
                // http://hotwired.lycos.com/webmonkey/reference/special_characters/
                result = System.Text.RegularExpressions.Regex.Replace(result, @"&(.{2,6});", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // for testng
                //System.Text.RegularExpressions.Regex.Replace(result, this.txtRegex.Text,string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // make line breaking consistent
                result = result.Replace("\n", "\r");
                // Remove extra line breaks and tabs:
                // replace over 2 breaks with 2 and over 4 tabs with 4. 
                // Prepare first to remove any whitespaces inbetween
                // the escaped characters and remove redundant tabs inbetween linebreaks
                result = System.Text.RegularExpressions.Regex.Replace(result, "(\r)( )+(\r)", "\r\r", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, "(\t)( )+(\t)", "\t\t", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, "(\t)( )+(\r)", "\t\r", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result, "(\r)( )+(\t)", "\r\t", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // Remove redundant tabs
                result = System.Text.RegularExpressions.Regex.Replace(result, "(\r)(\t)+(\r)", "\r\r", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // Remove multible tabs followind a linebreak with just one tab
                result = System.Text.RegularExpressions.Regex.Replace(result, "(\r)(\t)+", "\r\t", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                /* Disables because I don't need keep tabs and breaks so now It is faster only for this Lottery Class.
                // Initial replacement target string for linebreaks
                string breaks = "\r\r\r";
                // Initial replacement target string for tabs
                string tabs = "\t\t\t\t\t";
                for (int index = 0; index < result.Length; index++)
                {
                    result = result.Replace(breaks, "\r\r");
                    result = result.Replace(tabs,   "\t\t\t\t");
                    breaks = breaks + "\r";
                    tabs = tabs + "\t";
                }
                // Thats it.
                */
                return result;
            }
            catch
            {
                throw new ApplicationException("Error striping HTML");
            }
        }
      
        /// <summary>
        /// Format the string to a needed format for to split latter in to array
        /// the format is MM/DD/YYYY%n-n-n-n-n-n~
        /// ~ is used to delimite each record
        /// % is used to separate date of results
        /// - is used to separete results.
        /// </summary>
        /// <returns>None it works by reference.</returns>
        private void FormatText(ref string Text)
        {
            // Format Document
            // Please see this websites to understand Regular expresions & patters
            // http://javascript.about.com/library/blre24.htm
            //  http://www.devarticles.com/c/a/VB.Net/Regular-Expressions-in-.NET/
            // Delete all spaces, Carriage return, Horizontal tabs.
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"\r", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"\t", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // \d\d-[A-Z][A-Z][A-Z]-[21][09]\d\d
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"\d\d-[A-Z][A-Z][A-Z]-[21][09]\d\d", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            // Page \d of \d
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"Page \d\d of \d\d", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"Page \d of \d\d", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"Page \d of \d", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"LOTTERY", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"FLORIDA", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"Winning", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"Numbers", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"History", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"LOTTO", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            /*
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"Please", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"note", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"every", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"effort", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"has", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"been", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"made", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"to", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"ensure", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"that", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"the", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"enclosed", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"information", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"is", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"accurate", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"however", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"however", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            Text = System.Text.RegularExpressions.Regex.Replace(Text, @";", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @",", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            */


            Text = System.Text.RegularExpressions.Regex.Replace(Text, @" - ", "-", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            //Text = Text.Replace("  -  ", "-");
            // Prepare for split
            // find the pattern "/yyyy" and replace to "/yyyy%"
            if (System.Text.RegularExpressions.Regex.IsMatch(Text, @"(\/\d\d\d\d\s)"))
            {
                Text = System.Text.RegularExpressions.Regex.Replace(Text, @"(\/\d\d\d\d)", "$1%", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            else
            {
                // find the pattern "mm/dd/yy " and replace to "mm/dd/yy %"

                Text = System.Text.RegularExpressions.Regex.Replace(Text, @"(\d\d\/\d\d\/\d\d)", "$1%", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            // find the pattern "d " and replace to "d ~"
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"(\d\s)", "$1~", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            //Text = System.Text.RegularExpressions.Regex.Replace(Text, @"\s~", "~", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            //Text = System.Text.RegularExpressions.Regex.Replace(Text, @"~\s", "~", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"\s", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            //08/25/2007%-----
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"(\d\d\/\d\d\/\d\d\d\d%-----)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            //05/12/2011
            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"([X]\d\~)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            Text = System.Text.RegularExpressions.Regex.Replace(Text, @"([A-Za-z;,.])", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);




            Text = Text.TrimEnd("~".ToCharArray());
            //Text.Remove(Text.Length - 1);
        }

        /// <summary>
        /// Precess the information.
        /// </summary>
        /// <remarks>This HTMLParse Process the information
        /// And load values of fiels and properties to supply information
        /// to the consumers of this class.</remarks>
        /// <returns>Do not return nothing.</returns>
        private void LoadResultsIntoDataTable()
        {
            // Load the results in the table It use the split for read each lottery record.
            // split the results in a array
            _TotalDrawings = 0;

            try
            {
                foreach (string Myrecord in _Myrecords) // Iterate through elements.
                {
                    string[] MyFields = Myrecord.Split("%".ToCharArray());
                    DateTime Mydate = Convert.ToDateTime(MyFields[0]);
                    //filter by date if it was setted.
                    if (_MyDateFilter.From == DateTime.MinValue || _MyDateFilter.To == DateTime.MinValue)
                    {
                        // No filter by date.

                    }
                    else if (Mydate < _MyDateFilter.From || Mydate > _MyDateFilter.To)
                    {
                        // date is not between from to.
                        continue;
                    }
                    _TotalDrawings++;
                    //Create the Row.
                    string[] MyNumbers = MyFields[1].Split("-".ToCharArray());
                    foreach (string Mynumber in MyNumbers)
                    {
                        DataRow Myrow = WinningNumbers.NewRow();
                        Myrow["Date"] = Mydate;
                        Myrow["Number"] = int.Parse(Mynumber);
                        // Add the record
                        WinningNumbers.Rows.Add(Myrow);
                    }
                }
            }
            catch (Exception e)
            {
                throw (e);    // Rethrowing exception e
            }
        }

        /// <summary>
        /// Create the structure of the table with the results.
        /// </summary>
        /// <returns>void</returns>
        private void CreateTablesStructures()
        {
            try
            {
                //WinningNumbers
                WinningNumbers.Reset();

                //Create the table structure and constraints.
                DataColumn workCol = WinningNumbers.Columns.Add("Date", typeof(DateTime));
                workCol.AllowDBNull = false;
                workCol = WinningNumbers.Columns.Add("Number", typeof(int));
                workCol.AllowDBNull = false;

                //Set the Primary key
                DataColumn[] keys = new DataColumn[2];
                keys[0] = WinningNumbers.Columns["Date"];
                keys[1] = WinningNumbers.Columns["Number"];
                WinningNumbers.PrimaryKey = keys;

                //WinningStatistics
                WinningStatistics.Reset();

                //Create the table structure and constraints.
                workCol = WinningStatistics.Columns.Add("Number", typeof(int));
                workCol.AllowDBNull = false;
                workCol = WinningStatistics.Columns.Add("TotalWins", typeof(int));
                workCol.AllowDBNull = false;
                workCol = WinningStatistics.Columns.Add("Winning average", typeof(int));
                workCol.AllowDBNull = true;
                workCol = WinningStatistics.Columns.Add("LastWin", typeof(DateTime));
                workCol.AllowDBNull = true;
                workCol = WinningStatistics.Columns.Add("AverageDays", typeof(int));
                workCol.AllowDBNull = true;
                workCol = WinningStatistics.Columns.Add("DaysWithoutWin", typeof(int));
                workCol.AllowDBNull = true;
                workCol = WinningStatistics.Columns.Add("ProbableWinIn", typeof(DateTime));
                workCol.AllowDBNull = true;

                //Set the Primary key
                keys = new DataColumn[1];
                keys[0] = WinningStatistics.Columns["Number"];
                WinningStatistics.PrimaryKey = keys;

                // Frequencies
                Frequencies.Reset();

                //Create the table structure and constraints.
                workCol = Frequencies.Columns.Add("Number", typeof(int));
                workCol.AllowDBNull = false;
                workCol = Frequencies.Columns.Add("From", typeof(DateTime));
                workCol.AllowDBNull = false;
                workCol = Frequencies.Columns.Add("To", typeof(DateTime));
                workCol.AllowDBNull = false;
                workCol = Frequencies.Columns.Add("Days", typeof(int));
                workCol.AllowDBNull = false;

                //Set the Primary key
                keys = new DataColumn[3];
                keys[0] = Frequencies.Columns["Number"];
                keys[1] = Frequencies.Columns["From"];
                keys[2] = Frequencies.Columns["To"];
                Frequencies.PrimaryKey = keys;

            }
            catch (Exception e)
            {

                throw (e);    // Rethrowing exception e

            }

        }

        /// <summary>
        /// Get the next Drawing date
        /// </summary>
        /// <remarks>Search for the next Drawing day and return the date.</remarks>
        /// <returns>Datetime</returns>
        private DateTime GetNextDrawingDate(DateTime Date)
        {
            // Sort the Array.
            Array.Sort(_DrawingsDays);
            // Search the date in the Array 
            int RetVal = Array.BinarySearch(_DrawingsDays, Date.DayOfWeek);
            if (RetVal < 0) // is not a Drawing day.
            {
                // move date to the next drawin day.
                do
                {
                    Date = Date.AddDays(1);
                } while (Array.BinarySearch(_DrawingsDays, Date.DayOfWeek) < 0);
            }
            return Date;
        }

        /// <summary>
        /// Get the Average as batting Average
        /// </summary>
        private int GetWinningAverage(decimal TotalWins, decimal TotalDrawings)
        {
            Decimal WinningAverage = 0;
            if (TotalDrawings > 0)
            {
                WinningAverage = (TotalWins / TotalDrawings);
                WinningAverage = Decimal.Truncate((WinningAverage - Decimal.Truncate(WinningAverage)) * 1000);
            }
            return (int)WinningAverage;

        }

        /// <summary>
        /// Export results to a XML file.
        /// </summary>
        /// <param name="DataTable">DataTable to export to XML.</param>
        /// <param name="Path">Path where to write the XML File.</param>
        private void WriteDataTableToXml(DataTable DataTable, string Path)
        {
            DataTable.WriteXml(Path + DataTable.TableName + @".XML", XmlWriteMode.WriteSchema);
            DataTable.WriteXmlSchema(Path + DataTable.TableName + @".XSD");
        }

        /// <summary>
        /// Load Information from XML file to A DataTable
        /// </summary>
        /// <param name="DataTable">Datatable to Fill.</param>
        /// <param name="XmlFile">XML File to import.</param>
        /// <param name="XSDFile">XSD File to import</param>
        private void ReadXmlToDataTable(ref DataTable DataTable, string XmlFile, string XSDFile)
        {
            DataTable.Reset();
            DataTable.ReadXmlSchema(XSDFile);
            DataTable.ReadXml(XmlFile);

        }

        /// <summary>
        /// Write the Results to a XML File.
        /// </summary>
        /// <param name="FileName">Path and File Name of the new XML File.</param>
        private void WriteResultsToXML(string FileName)
        {
            string Mystring = string.Join("~", _Myrecords);

            XmlTextWriter MyXmlTextWriter = new XmlTextWriter(FileName, Encoding.UTF8);

            MyXmlTextWriter.Formatting = Formatting.Indented;
            MyXmlTextWriter.Indentation = 5;

            MyXmlTextWriter.WriteStartDocument();
            MyXmlTextWriter.WriteComment("This is the result formated not filtered.");
            //MyXmlTextWriter.WriteStartElement("Results");
            MyXmlTextWriter.WriteElementString("Results", Mystring);
            //MyXmlTextWriter.WriteEndElement();
            MyXmlTextWriter.WriteEndDocument();
            MyXmlTextWriter.Flush();
            MyXmlTextWriter.Close();
            MyXmlTextWriter = null;
        }

        /// <summary>
        /// Load the _Myrecords Array from a XML File.
        /// </summary>
        private void GetWinningNumbersFromXML()
        {
            // create the document.
            XmlDataDocument MyXmlDataDocument = new XmlDataDocument();
            MyXmlDataDocument.Load(_XMLDataPath + "Results.XML");

            _Myrecords = (MyXmlDataDocument.SelectSingleNode("Results").FirstChild.Value.Split("~".ToCharArray()));

            MyXmlDataDocument = null;
        }

        /// <summary>
        /// Get or Set the http web page source to feed the results.
        /// </summary>
        /// <remarks></remarks>
        /// <value></value>
        public string SourceURI
        {
            get
            {
                return _SourceURI.OriginalString;
            }
            set
            {
                _SourceURI = new Uri(value);
            }

        }
     
        /// <summary>
        /// Get or Set the proxy server URI if it is needed.
        /// </summary>
        public string ProxyURI
        {
            get
            {
                return _ProxyURI.OriginalString;
            }
            set
            {
                _ProxyURI = new Uri(value);
            }

        }

        /// <summary>
        /// User Name needed for Credential when use proxy server.
        /// </summary>
        public string UserName
        {
            get
            {
                return _UserName.ToString();
            }
            set
            {
                _UserName = (value);
            }
        }

        /// <summary>
        /// Password needed for credential when use proxy server.
        /// </summary>
        public string Password
        {
            get
            {
                return _Password.ToString();
            }
            set
            {
                _Password = (value);
            }
        }

        /// <summary>
        /// Get Set the Filter Date From
        /// </summary>
        public DateTime FilterDateFrom
        {
            get
            {
                return _MyDateFilter.From;
            }
            set
            {
                _MyDateFilter.From = (value);
            }
        }

        /// <summary>
        /// Get Set the Filter Date To
        /// </summary>
        public DateTime FilterDateTo
        {
            get
            {
                return _MyDateFilter.To;
            }
            set
            {
                _MyDateFilter.To = (value);
            }
        }

        /// <summary>
        /// Get Set Drawing Days.
        /// </summary>
        public DayOfWeek[] DrawingsDays
        {
            get
            {
                return _DrawingsDays;
            }
            set
            {
                _DrawingsDays = value;
            }
        }

        /// <summary>
        /// Get the total of drawings in the date range.
        /// </summary>
        public int TotalDrawings
        {
            get
            {
                return _TotalDrawings;
            }
        }

        /// <summary>
        /// Get Set if the data comes from the Data.XML File.
        /// </summary>
        public Boolean LoadDataFromXML
        {
            get
            {
                return _LoadDataFromXML;
            }
            set
            {
                _LoadDataFromXML = value;
            }
        }

        public String XMLDataPath
        {
            get
            {
                return _XMLDataPath;
            }
            set
            {
                _XMLDataPath = value;
            }
        }

    }
}
