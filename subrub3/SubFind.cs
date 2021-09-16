// Be careful with this!!! it can potentially cause a lot of requests
// Only use this for your own domains or domains that you have permission to test.
// See License agreement!
// wallparse@gmail.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace subrub3
{
    public class SubFind
    {
        public class SubResult
        {
            public string strDomain = null;
            public List <string> strIP = null;
            public DigStatus.DigInfo digInfo = null;
            public string strHttpsResult = null;
            public string strHttpResult = null;
        }

        public List<string> lstSubdomains = new List<string>();
        public List <string> strPostfixDomain = new List<string>();
        public List<SubResult> lstResult = new List<SubResult>();
        private HashSet<string> lstAvoidIP = new HashSet<string>();
        public bool bVerbose = true;
        public string strLabel = string.Empty;
        int testCount = 0;
        public bool bAlwaysDig = false;
        public bool bFull = false;
        public string strFullDigPath = null;
        public bool bDigResult = false;

        static int objectCount = 0;

        public void addAvoidIP(string strIP)
        {
            this.lstAvoidIP.Add(strIP);
        }

        public SubFind()
        {
            objectCount++;
            this.strLabel = objectCount.ToString();
        }

        public int getTestsCount()
        {
            return testCount;
        }

        public int getResultCount()
        {
            return lstResult.Count;
        }

        bool shouldAvoid(string strIP)
        {
            return lstAvoidIP.Contains(strIP);
        }

        public void run()
        {
            testCount = 0;

            foreach (string strDomain in lstSubdomains)
            {
                for (int i = 0; i < strPostfixDomain.Count; i++)
                {
                    if (strPostfixDomain[i].Length < 2) continue;

                    string strTestDomain = $"{strDomain}.{strPostfixDomain[i]}";
                    IPHostEntry ip = null;

                    try
                    {
                        testCount++;
                        ip = Dns.GetHostEntry(strTestDomain);
                    }
                    catch(Exception ex)
                    {
                        if(ex is System.Net.Sockets.SocketException && bAlwaysDig)
                        {
                            SubResult sr = new SubResult
                            {
                                strDomain = $"{strDomain}.{strPostfixDomain[i]}",
                                strIP = ip?.AddressList?.ToList().Select(a => a.ToString()).ToList()
                            };

                            DigStatus ds = new DigStatus();
                            ds.strFullDigPath = this.strFullDigPath;
                            ds.bDigResult = this.bDigResult;
                            

                            Console.WriteLine($"[+] Domain was not resolved executing dig anyway {sr.strDomain}");
                            DigStatus.DigInfo di = ds.getDigResult(sr.strDomain);

                            if (di != null && di.lstRecord != null && (di.lstRecord.Count > 0 || di.strStatus != null))
                            {
                                Console.WriteLine($"[+] Dig successful: {di.strStatus} with {di.lstRecord.Count} records");
                                sr.digInfo = di;
                                lstResult.Add(sr);
                            }
                        }

                        continue;
                    }

                    if (ip?.AddressList?.Length > 0)
                    {
                        bool bAvoid = ip.AddressList.FirstOrDefault(e => this.shouldAvoid(e.ToString())) != null;

                        if (bAvoid) continue;

                        SubResult sr = new SubResult
                        {
                            strDomain = $"{strDomain}.{strPostfixDomain[i]}",
                            strIP = ip?.AddressList?.ToList().Select(a => a.ToString()).ToList()
                        };

                        lstResult.Add(sr);

                        Console.WriteLine($"[+] Found : {sr.strDomain} with ip: {string.Join(", ", sr.strIP)}");

                        if(bFull)
                        {
                            DigStatus ds = new DigStatus();
                            ds.strFullDigPath = strFullDigPath;
                            ds.bDigResult = bDigResult;
                            
                            Console.WriteLine("[+] Executing dig: " + sr.strDomain);
                            DigStatus.DigInfo di = ds.getDigResult(sr.strDomain);

                            sr.digInfo = di;

                            
                            WebRequest wr = WebRequest.Create($"https://{sr.strDomain}/");


                            if (wr != null)
                            {
                                wr.Timeout = 4000;

                                try
                                {
                                    HttpWebResponse response = (HttpWebResponse)wr.GetResponse();

                                    sr.strHttpsResult = response?.StatusCode.ToString();
                                }
                                catch(Exception ex)
                                {
                                    sr.strHttpsResult = ex.Message;
                                }
                            }

                            HttpWebRequest wr2 = (HttpWebRequest) WebRequest.Create($"http://{sr.strDomain}/");


                            if (wr2 != null)
                            {
                                wr2.Timeout = 5000;

                                try
                                {
                                    HttpWebResponse response2 = (HttpWebResponse)wr2.GetResponse();

                                    sr.strHttpResult = response2?.StatusCode.ToString();
                                }
                                catch(Exception ex2)
                                {
                                    sr.strHttpResult = ex2.Message;
                                }
                            }



                        }
                    }

                }

            }
        }
    }
}
