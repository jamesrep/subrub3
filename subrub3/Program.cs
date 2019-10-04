// Be careful with this!!! it can potentially cause a lot of requests
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
            List <string> strDomain = new List<string>();
            string strDomainList = null;
            string strOutput = null;
            bool bVerbose = true;

            if(args.Length < 2)
            {
                Console.WriteLine("[+] Searches for subdomains (specify at least --domain and --domainlist)");
                Console.WriteLine("--threads <number of threads to use>");
                Console.WriteLine("--domain <domain to test if not domainfile is used>");
                Console.WriteLine("--domainfile <path to list of domains to test>");
                Console.WriteLine("--domainlist <path to list of subdomains to test>");
                Console.WriteLine("--output <output file>");

                return;
            }

            for(int i=0; i < args.Length; i++)
            {
                if(args[i] == "--threads")
                {
                    threadCount = Convert.ToInt32(args[++i]);
                }
                else if(args[i] == "--domain")
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
                else if (args[i] == "--output")
                {
                    strOutput = args[++i];
                }
            }

            SubFind[] sfList = new SubFind[threadCount];
            string[] strDomains = File.ReadAllLines(strDomainList);

            int divider = strDomains.Length / threadCount;
            int rest = strDomains.Length % threadCount;

            int currentLine = 0;

            for(int i=0; i < sfList.Length; i++)
            {
                sfList[i] = new SubFind
                {
                    strPostfixDomain = strDomain
                };

                for (int x=0; x < divider; x++)
                {
                    sfList[i].lstSubdomains.Add(strDomains[currentLine++]);
                }
            }

            if(rest > 0)
            {
                for(int x=currentLine; x < strDomains.Length; x++)
                {
                    sfList[sfList.Length-1].lstSubdomains.Add(strDomains[x]);
                }
            }

            Console.WriteLine("[+] Starting threads...");

            List<Thread> allThreads = new List<Thread>();

            DateTime dtStart = DateTime.Now;

            for(int i=0; i < sfList.Length; i++)
            {               
                Thread th = new Thread(new ThreadStart(sfList[i].run));
                allThreads.Add(th);
                th.Start();
            }

            bool bContinue = true;

            while(bContinue)
            {

                bContinue = allThreads.FirstOrDefault(e => e.ThreadState == ThreadState.Running) != null;

                if (bVerbose)
                {
                    TimeSpan ts = DateTime.Now - dtStart;

                    if (ts.TotalSeconds > 5)
                    {
                        sfList.ToList().ForEach(e => Console.WriteLine($"{e.strLabel} -> Testcount: {e.getTestsCount()} - Results: {e.getResultCount()}"));

                        dtStart = DateTime.Now;
                    }

                    
                }
            }

            Console.WriteLine("[+] All done");

            if(strOutput != null)
            {
                Console.WriteLine($"[+] Writing to {strOutput} ");

                StreamWriter sw = new StreamWriter(strOutput);

                foreach(var s in sfList)
                {
                    foreach (var result in s.lstResult)
                    {
                        string strOut = $"{result.strDomain}|{string.Join(", ", result.strIP)}";
                        sw.WriteLine(strOut);

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
