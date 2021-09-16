// Be careful with this!!! it can potentially cause a lot of requests
// Only use this for your own domains or domains that you have permission to test.
// See License agreement!
// wallparse@gmail.com

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace subrub3
{
    class Program
    {
        static void Main(string[] args)
        {
            int threadCount = 5;
            List<string> strDomain = new List<string>();
            string strDomainList = null;
            string strOutput = null;
            bool bVerbose = true;
            bool bFull = false;
            bool bAlwaysDig = false;
            string strFullDigPath = null;

            if (args.Length < 2)
            {
                Console.WriteLine("[+] Searches for subdomains (specify at least --domain and --domainlist)");
                Console.WriteLine("--threads <number of threads to use>");
                Console.WriteLine("--domain <domain to test if not domainfile is used>");
                Console.WriteLine("--domainfile <path to list of domains to test>");
                Console.WriteLine("--domainlist <path to list of subdomains to test>");
                Console.WriteLine("--output <output file>");
                Console.WriteLine("--alwaysdig dig domain even if it could not be resolved");

                return;
            }

            List<string> lstAvoid = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--threads")
                {
                    threadCount = Convert.ToInt32(args[++i]);
                }
                else if (args[i] == "--domain")
                {
                    string strDomainArg = args[++i];

                    if (strDomain.Contains(",")) // yea yea, no commas in domains
                    {
                        strDomain.AddRange(strDomainArg.Split(new char[] { ',' }));
                    }
                    else
                    {
                        strDomain.Add(strDomainArg);
                    }
                }
                else if (args[i] == "--domainfile")
                {
                    string strDomainArg = args[++i];

                    string[] strAllDomains = File.ReadAllLines(strDomainArg);

                    strDomain.AddRange(strAllDomains);
                }
                else if (args[i] == "--domainlist")
                {
                    strDomainList = args[++i];
                }
                else if (args[i] == "--fulldigpath")
                {                    
                    strFullDigPath = args[++i];

                    Console.WriteLine($"[+] Dig-path set to {strFullDigPath}");
                }
                else if (args[i] == "--avoid")
                {
                    lstAvoid.Add(args[++i]);
                }
                else if (args[i] == "--output")
                {
                    strOutput = args[++i];
                }
                else if (args[i] == "--full")
                {
                    bFull = true;
                }
                else if (args[i] == "--alwaysdig")
                {
                    bAlwaysDig = true;
                }
            }



            SubFind[] sfList = new SubFind[threadCount];
            string[] strDomains = File.ReadAllLines(strDomainList);

            int divider = strDomains.Length / threadCount;
            int rest = strDomains.Length % threadCount;
            int currentLine = 0;

            for (int i = 0; i < sfList.Length; i++)
            {
                sfList[i] = new SubFind
                {
                    strPostfixDomain = strDomain,
                    bFull = bFull,
                    bAlwaysDig = bAlwaysDig,
                    strFullDigPath = strFullDigPath
                };

                foreach (string str in lstAvoid) { sfList[i].addAvoidIP(str); }

                for (int x = 0; x < divider; x++)
                {
                    sfList[i].lstSubdomains.Add(strDomains[currentLine++]);
                }
            }

            if (rest > 0)
            {
                for (int x = currentLine; x < strDomains.Length; x++)
                {
                    sfList[sfList.Length - 1].lstSubdomains.Add(strDomains[x]);
                }
            }

            Console.WriteLine("[+] Starting threads...");

            List<Thread> allThreads = new List<Thread>();

            DateTime dtStart = DateTime.Now;

            for (int i = 0; i < sfList.Length; i++)
            {
                Thread th = new Thread(new ThreadStart(sfList[i].run));
                allThreads.Add(th);
                th.Start();
            }

            bool bContinue = true;

            while (bContinue)
            {
                int nrDone = allThreads.Select(e => e.IsAlive).Where(a => a == false).Count();

                bContinue = nrDone < allThreads.Count;

                if (bVerbose)
                {
                    TimeSpan ts = DateTime.Now - dtStart;

                    if (ts.TotalSeconds > 5)
                    {
                        sfList.ToList().ForEach(e => Console.WriteLine($"{e.strLabel} -> Testcount: {e.getTestsCount()} - Results: {e.getResultCount()}"));

                        dtStart = DateTime.Now;
                    }
                }

                System.Threading.Thread.Sleep(250);
            }

            Console.WriteLine("[+] All done");

            //allThreads.Select(e => e.ThreadState == ThreadState.Running || e.ThreadState == ThreadState.Suspended || e.ThreadState == ThreadState.WaitSleepJoin
            //    || e.ThreadState == ThreadState.SuspendRequested).ToList().ForEach(a => Console.WriteLine(a));

            if (strOutput != null)
            {
                Console.WriteLine($"[+] Writing to {strOutput} ");

                StreamWriter sw = new StreamWriter(strOutput);

                foreach(var s in sfList)
                {
                    foreach (var result in s.lstResult)
                    {
                        string strOut = $"{result.strDomain}|{string.Join(", ", result.strIP)}";

                        if(result?.digInfo?.strStatus != null)
                        {
                            strOut += $"|http:{result.strHttpResult}|https:{result.strHttpsResult}|{result.digInfo.strStatus}|{string.Join(",", result.digInfo.lstRecord.Select(e => $"{e.strType}:{e.strStart}:{e.strRecord}" ).ToArray())}";
                        }

                        sw.WriteLine(strOut);
                        sw.Flush();

                        if(bVerbose)
                        {
                            Console.WriteLine(strOut);
                        }
                    }
                }

                sw.Close();
            }
            
        }
    }
}
