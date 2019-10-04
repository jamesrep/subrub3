// Be careful with this!!! it can potentially cause a lot of requests
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
        }

        public List<string> lstSubdomains = new List<string>();
        public List <string> strPostfixDomain = new List<string>();
        public List<SubResult> lstResult = new List<SubResult>();
        private HashSet<string> lstAvoidIP = new HashSet<string>();
        public bool bVerbose = true;
        public string strLabel = string.Empty;
        int testCount = 0;

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
                    }
                }

            }
        }
    }
}
