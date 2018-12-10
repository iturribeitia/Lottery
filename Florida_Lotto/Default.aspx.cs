using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using MAI.Lottery.FloridaLotto;

namespace Florida_Lotto
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            FloridaLotto OFloridaLotto = new FloridaLotto();

            if (!IsPostBack)
            {
                //Lotto OLotto = new Lotto("http://www.flalottery.com/exptkt/l6.htm", "http://phxpsce.aexp.com:8080", "bdave0", "js5077", DateTime.Today.AddMonths(-8), DateTime.Today);
                //Lotto OLotto = new Lotto("http://www.flalottery.com/exptkt/l6.htm", "http://phxpsce.aexp.com:8080", "bdave0", "js5077", DateTime.Today.AddMonths(-8), DateTime.Today);


                // Only for test.
                // OLotto.GetNextDrawingDate(DateTime.Now);

                // end test.
                /*
                //Lotto.SourceURI = "http://www.flalottery.com/exptkt/l6.htm";
                OLotto.ProxyURI = "http://phxpsce.aexp.com:8080";
                OLotto.UserName = "gtorr6";
                OLotto.Password = "bh1446";
                */
                OFloridaLotto.FilterDateTo = DateTime.Today;
                OFloridaLotto.FilterDateFrom = OFloridaLotto.FilterDateTo.AddMonths(-9);
                //OLotto.FilterDateFrom = OLotto.FilterDateTo.AddDays(-8);
                //OLotto.DrawingsDays = new DayOfWeek[4] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Saturday, DayOfWeek.Wednesday }; //http://www.flalottery.com/inet/media-tvdrawingsMain.do
                OFloridaLotto.LoadDataFromXML = false;
                OFloridaLotto.Process();
                // Save the object in a cache variable for State Management in the server side.
                Cache["Olotto"] = OFloridaLotto;
                // Save the object in a application variable for State Management in the server side.
                Application["Olotto"] = OFloridaLotto;
                // Save the object in a session variable for State Management in the client side.
                Session["Olotto"] = OFloridaLotto;
            }
            else
            {

                // Recover the object from the Cache
                OFloridaLotto = (FloridaLotto)Cache["Olotto"];
                OFloridaLotto = (FloridaLotto)Cache.Get("Olotto");

                // Recover the object from the Session
                OFloridaLotto = (FloridaLotto)Session["Olotto"];
                OFloridaLotto = (FloridaLotto)Session[0];

                // Recover the object from the Application
                OFloridaLotto = (FloridaLotto)Application["Olotto"];
                OFloridaLotto = (FloridaLotto)Application[0];

            }
            // Bind the datatable to the grid.
            this.GridView1.DataSource = OFloridaLotto.WinningStatistics;
            this.DataBind();

        }
    }
}
