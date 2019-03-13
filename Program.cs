/*
 * Horrible program written in 2 minutes to get images/screenshots from Steam
 * Do whatever you want with it
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using HtmlAgilityPack;

namespace SteamLoader {
    internal class Program {
        private static void Main(string[] args) {
            if (args.Length <= 0) {
                Console.WriteLine("SteamLoader.exe AppId [images|screenshots] [Startpage] [EndPage]");
                Console.ReadLine();
                return;
            }
            var appId = args[0];
            var source = args.Length >= 2 ? args[1] : "images";
            if (source != "images" && source != "screenshots") {
                Console.WriteLine($"Invalid source {source}");
                Console.ReadLine();
                return;
            }
            var startPage = args.Length >= 3 ? int.Parse(args[2]) : 1;
            var endPage = args.Length >= 4 ? int.Parse(args[3]) : 9000;
            var outFile = $"{appId}-{source}.txt";
            if(File.Exists(outFile)) File.Delete(outFile);
            _ = new Program(appId, source, outFile, startPage, endPage);
        }

        private Program(string appId, string source, string outFile, int startPage, int endPage) {
            startPage = startPage <= 0 ? 1 : startPage;
            var userImages = new List<string>();
            var retries = 0;

            for (var i = startPage; i < endPage + 1; i++) {
                if(retries == 0) Console.WriteLine($"Getting page {i}");
                try {
                    var url = CreateUrl(source, appId, i);
                    var readApi = ReadApi(url);
                    if (readApi.Length <= 0) {
                        if (retries >= 3) {
                            Console.WriteLine($"Failed to get page {i} after 3 retries. Assuming final page");
                            break;
                        }
                        Console.WriteLine($"Failed to get page {i}. Retrying");
                        retries++;
                        i--;
                    }
                    var parse = GetImages(readApi);
                    var collection = parse.ToList();
                    userImages.AddRange(collection);

                    Console.WriteLine($"Writing page {i} to {outFile}");
                    File.AppendAllLines(outFile, collection);
                    retries = 0;
                } catch (Exception) {
                    if (retries >= 3) {
                        Console.WriteLine($"Failed to get page {i} after 3 retries. Assuming final page");
                        break;
                    }
                    Console.WriteLine($"Failed to get page {i}. Retrying");
                    retries++;
                    i--;
                }
            }

            Console.WriteLine($"Loaded {userImages.Count} images");
            Console.ReadLine();
        }

        private IEnumerable<string> GetImages(string html) {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var images = doc.DocumentNode.Descendants("img");
            var userImages = new List<string>();

            foreach (var htmlNode in images.ToList()) {
                var src = htmlNode.GetAttributeValue("src", "nope");
                if (!src.Contains("steamuserimages")) continue;
                var query = new Uri(src).Query;
                if (query.Length <= 0) {
                    userImages.Add(src);
                    continue;
                }
                userImages.Add(src.Replace(query, ""));
            }

            return userImages;
        }

        private static string ReadApi(string url) {
            using (var wc = new WebClient()) {
                try {
                    return wc.DownloadString(url);
                } catch (WebException e) {
                    return "";
                }
            }
        }

        private static string CreateUrl(string source, string appId, int page) {
            source = source == "images" ? "4" : "2";
            return $"https://steamcommunity.com/app/{appId}/homecontent/?userreviewsoffset=0&p={page}&workshopitemspage={page}&readytouseitemspage={page}&mtxitemspage={page}&itemspage={page}&screenshotspage={page}&videospage={page}&artpage={page}&allguidepage={page}&webguidepage={page}&integratedguidepage={page}&discussionspage={page}&numperpage=10&browsefilter=toprated&browsefilter=toprated&appid={appId}&appHubSubSection=4&appHubSubSection={source}&l=english&filterLanguage=default&searchText=&forceanon=1";
        }
    }
}
