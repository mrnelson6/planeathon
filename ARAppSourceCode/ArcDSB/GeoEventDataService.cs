using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using QuickType;

namespace ArcDSB
{
    public partial class GeoEventDataService : ServiceBase
    {
        public GeoEventDataService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            string json_raw = "";
            try
            {
                json_raw = new System.Net.WebClient().DownloadString("https://opensky-network.org/api/states/all?lamin=33.87845&lomin=-117.81135&lamax=34.28221&lomax=-116.94345");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            State[] states = JSONData.FromJson(json_raw).States;
            foreach (var state in states)
            {

            }
        }

        protected override void OnStop()
        {
        }
    }
}
