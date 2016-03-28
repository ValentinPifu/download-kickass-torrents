using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace KickassTorrents
{
    class MyWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            return request;
        }
    }

    class Program
    {
        //Input torrent name
        private static string InputName()
        {
            while (true)
            {
                Console.WriteLine(Environment.NewLine + " Search torrents on kickass.torrents!");
                Console.Write(" Enter a torrent name (min. 3 char) >> ");
                Console.ForegroundColor = ConsoleColor.Green;
                var name = Console.ReadLine().ToLower();
                Console.ResetColor();
                if (name.Length < 4)
                {
                    Console.Clear();
                }
                else
                {
                    Console.WriteLine();
                    return name;
                }
            }
        }

        //Getting data from kickass.torrents
        private static IEnumerable<Tuple<int, string, string>> DownloadData(string name, int page)
        {
            try
            {
                //Downloading data
                var url = "https://kat.cr/usearch/" + name.Replace(" ", "%20") + "/" + page + "/";
                var data = new MyWebClient().DownloadString(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(data);
                var namesList = new List<string>();
                var linksList = new List<string>();
                var sizesList = new List<string>();
                var seedersList = new List<string>();
                var leechersList = new List<string>();

                //Creating names list
                foreach (HtmlNode x in doc.DocumentNode.SelectNodes("//*[contains(@class,'cellMainLink')]"))
                {
                    namesList.Add(x.InnerText);
                }

                //Creating links list
                foreach (
                    HtmlNode x in
                        doc.DocumentNode.SelectNodes("//*[contains(@href, 'magnet')]")
                            .Where(x => x.GetAttributeValue("href", string.Empty).Contains("magnet")))
                {
                    linksList.Add(x.GetAttributeValue("href", string.Empty));
                }

                //Creating sizes list
                foreach (HtmlNode x in doc.DocumentNode.SelectNodes("//*[contains(@class, 'nobr')]").Where(x => x.InnerText != "leech"))
                {
                    sizesList.Add(x.InnerText);
                }
                
                //Creating seeders list
                foreach (HtmlNode x in doc.DocumentNode.SelectNodes("//*[contains(@class, 'green center')]"))
                {
                    seedersList.Add(x.InnerText);
                }
                
                //Creating leechers list
                foreach (HtmlNode x in doc.DocumentNode.SelectNodes("//*[contains(@class, 'red lasttd center')]"))
                {
                    leechersList.Add(x.InnerText);
                }

                //Creating tuple from lists
                return namesList.Zip(linksList, Tuple.Create)
                    .Select((twoTuple, index) => Tuple.Create(index + 1, twoTuple.Item1, twoTuple.Item2));
            }
            catch (Exception)
            {
                Console.Write(" Nothing found! Your search ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(name);
                Console.ResetColor();
                Console.WriteLine(" did not match any documents");
                return null;
            }
        }

        //Input menu
        private static string InputMenu(IEnumerable<Tuple<int, string, string>> tuple, int page)
        {
            if (tuple == null) return "nothing";
            foreach (var line in tuple)
            {
                Console.WriteLine(" {0}. {1}", line.Item1,
                    line.Item2.Length <= 120 ? line.Item2 : line.Item2.Substring(0, 120));
            }
            var tupleLenght = tuple.Count();
            Console.Write(
                Environment.NewLine + " Select torrent [1-{0}] or [M] for menu or [0] to exit or [-][+] to change Page {1} >> ",
                tupleLenght, page);
            var curPos = Console.CursorTop;
            while (true)
            {
                var selIndex = Console.ReadLine();
                switch (selIndex.ToLower())
                {
                    case "+":
                        return "+";
                    case "-":
                        return "-";
                    case "m":
                        return "m";
                    case "0":
                        return "0";
                    default:
                        var selIndexSplit = selIndex.Split(' ');
                        foreach (var i in selIndexSplit)
                        {
                            int ignoreMe;
                            var tryParse = int.TryParse(i, out ignoreMe);
                            if (!tryParse) continue;
                            var selIndexInt = int.Parse(i);
                            if (selIndexInt > 0 && selIndexInt <= tupleLenght)
                            {
                                return selIndex;
                            }
                        }
                        if (tupleLenght < 10)
                        {
                            Console.SetCursorPosition(83, curPos);
                            Console.Write("                    ");
                            Console.SetCursorPosition(83, curPos);
                        }
                        else
                        {
                            Console.SetCursorPosition(84, curPos);
                            Console.Write("                    ");
                            Console.SetCursorPosition(84, curPos);
                        }
                        continue;
                }
            }
        }

        //Download torrent
        private static void DownloadTorrent(IEnumerable<Tuple<int, string, string>> tuple, string selIndex)
        {
            var tupleLenght = tuple.Count();
            var selIndexSplit = selIndex.Split(' ');
            foreach (var i in selIndexSplit)
            {
                int ignoreMe;
                var tryParse = int.TryParse(i, out ignoreMe);
                if (!tryParse) continue;
                var selIndexInt = int.Parse(i);
                if (selIndexInt < 0 || selIndexInt > tupleLenght) continue;
                foreach (var line in tuple.Where(line => line.Item1 == selIndexInt))
                {
                    //Process.Start(line.Item3);
                    Console.WriteLine(" Downloading >> {0}. {1}", line.Item1, line.Item2);
                }
            }
                
        }

        //Search again
        private static ConsoleKey SearchAgain()
        {
            Console.Write(Environment.NewLine + " Press [Y] to search again or [0]/[ENTER] to exit >> ");
            return Console.ReadKey().Key;
        }

        //Main method
        private static void Main(string[] args)
        {
            var page = 1;
            while (true)
            {
                var name = InputName();
                IEnumerable<Tuple<int, string, string>> tuple;
                string input;
                while (true)
                {
                    tuple = DownloadData(name, page);
                    input = InputMenu(tuple, page);
                    if (input != "+" && input != "-")
                        break;
                    if (input == "+") page++;
                    if ((input == "-") && (page > 1)) page--;
                    Console.SetCursorPosition(0, 4);
                    for (var i = 0; i < 27; i++)
                    {
                        Console.Write(new string(' ', 130));
                    }
                    Console.SetCursorPosition(0, 4);
                }
                switch (input)
                {
                    case "m":
                        Console.Clear();
                        continue;
                    case "nothing":
                        goto exitLoop;
                    case "0":
                        return;
                    default:
                        DownloadTorrent(tuple, input);
                        goto exitLoop;
                }
                exitLoop:
                {
                    var retry = SearchAgain();
                    if (retry == ConsoleKey.Y)
                    {
                        Console.Clear();
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
}