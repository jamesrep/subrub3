// Be careful with this!!! it can potentially cause a lot of requests
// Only use this for your own domains or domains that you have permission to test.
// See License agreement!
// wallparse@gmail.com

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace subrub3
{
    public class DigStatus
    {
        public class DigAnswerRecord
        {
            public string strType; // CNAME, A
            public string strRecord; // ip-adress or domain.
            public string strStart;
        }

        public class DigInfo
        {
            public string strStatus;
            public List<DigAnswerRecord> lstRecord = new List<DigAnswerRecord>();
        }

        public string strDNSServer = "8.8.8.8";
        public string strDigPath = @"C:\bin\bind";
        string strRegexStatus = @"status\:\s{0,3}(?<status>\w+)";
        string strRegexAnswer = @"([\w|\.]+)";

        enum parseMode
        {
            status,
            waitanswer,
            answer
        };

        public DigInfo getDigResult(string strDomainToTest)
        {
            DigInfo dinf = new DigInfo();
            Process p = new Process();

            p.StartInfo = new ProcessStartInfo($"{strDigPath}\\dig.exe")
            {
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = strDigPath,
                LoadUserProfile = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = $"{strDomainToTest} @{strDNSServer}"
            };

            parseMode pm = parseMode.status;

            bool bStarted = p.Start();

            while (!p.StandardOutput.EndOfStream)
            {
                var strLine = p.StandardOutput.ReadLine();


                if(parseMode.status == pm && strLine.Length > 2)
                {
                    MatchCollection mc = Regex.Matches(strLine, strRegexStatus);

                    if (mc.Count > 0)
                    {
                        dinf.strStatus = mc[0].Groups[1].Value;
                        pm = parseMode.waitanswer;
                    }
                }
                else if (parseMode.waitanswer == pm)
                {
                    if(strLine.IndexOf("ANSWER SECTION") > 0)
                    {
                        pm = parseMode.answer;
                    }
                }
                else if(parseMode.answer == pm)
                {
                    if(strLine.Length <1 || strLine.Trim().Length < 1)
                    {
                        p.StandardOutput.ReadToEnd();
                        break;
                    }

                    MatchCollection mc = Regex.Matches(strLine, strRegexAnswer);

                    if (mc.Count > 4)
                    {
                        DigAnswerRecord da = new DigAnswerRecord()
                        {
                            strRecord = mc[4].Value.TrimEnd(new char[] { '.' }),
                            strType = mc[3].Value.TrimEnd(new char[] { '.' }),
                            strStart = mc[0].Value.TrimEnd(new char[] { '.' }),
                        };
                        dinf.lstRecord.Add(da);
                    }
                }
                
            }

            p.WaitForExit();

            Console.WriteLine("[+] Dig is done");



            return dinf;
        }

        
    }
}
