using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using System.Net.Http;
using System.Threading;
using System.Diagnostics;

namespace DomainTesting
{
    class Program
    {
        private static string[] headers;
        private static HttpClient client;

        static async Task Main(string[] args)
        {
            var sw = new Stopwatch();
            sw.Start();

            client = new HttpClient()
            {
                Timeout = new TimeSpan(0,0,15)
            };

            var path = "C:\\temp\\WM5145-BR_ResellerTextFile_0823201938424.csv";
            DataTable dt = ImportCSV(path);


            List<Task<Domain>> results = new List<Task<Domain>>();
            var count = 0;
            double i = 0;

            try
            {
                var progress = new Progress<HttpResponseMessage>();

                progress.ProgressChanged += (_, result) =>
                {
                    count++;
                    i = Math.Round(((double)count / results.Count * 100), 2);

                    Console.Clear();
                    Console.WriteLine($"{i}% Complete | Status: {result.RequestMessage.RequestUri}");
                };

                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        var domainName = (string)row["DomainName"];
                        DateTime creationDate = new DateTime();

                        if(DateTime.TryParse((string)row["CreationDate"], out creationDate))
                        {}
                        else
                        {
                            creationDate = DateTime.Now;
                        }

                        var experationDate = DateTime.Parse((string)row["ExpDate"]);

                        var domain = new Domain(domainName, creationDate, experationDate);
                        results.Add(GetDomainAsync(domain, progress));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                var data = new List<Domain>(await Task.WhenAll(results));
                var sb = new StringBuilder();
                sb.AppendLine("DomainName, CreationDate, ExperationDate, OwnedTimespan(Days), Response");
                foreach (var item in data)
                {
                    sb.AppendLine(item.ToString());
                }
                File.WriteAllText("C:\\temp\\test.csv", sb.ToString()); 
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                sw.Stop();
                Console.WriteLine($"Finished in {sw.ElapsedMilliseconds}ms");
                dt.Dispose();
            }

        }
        public static DataTable ImportCSV(string filePath)
        {
            List<string> lines = new List<string>();
            FileStream file = new FileStream(filePath, FileMode.Open);

            using (var parser = new TextFieldParser(new StreamReader(file)))
            {
                parser.HasFieldsEnclosedInQuotes = true;
                parser.SetDelimiters(",");

                do
                {
                    lines.Add(parser.ReadLine());
                } while (!parser.EndOfData);
            }


            using (var headerParser = new TextFieldParser(new StringReader(lines[0])))
            {
                headerParser.HasFieldsEnclosedInQuotes = true;
                headerParser.SetDelimiters(",");

                headers = headerParser.ReadFields();
            };

            DataTable dt = new DataTable();

            foreach (var header in headers)
            {
                dt.Columns.Add(new DataColumn(header));
            }


            foreach (var line in lines.Skip(1))
            {
                using (var dataParser = new TextFieldParser(new StringReader(line)))
                {
                    dataParser.HasFieldsEnclosedInQuotes = true;
                    dataParser.SetDelimiters(",");

                    var data = dataParser.ReadFields();

                    if (data.Length == dt.Columns.Count)
                    {
                        DataRow row = dt.NewRow();
                        row.ItemArray = data;
                        dt.Rows.Add(row);
                    }
                };
            }

            return dt;
        }
        public static async Task<Domain> GetDomainAsync(Domain domain, IProgress<HttpResponseMessage> progress)
        {
            {
                var uri = new Uri($"http://www.{domain.DomainName}");

                try
                { 
                    var response = await client.GetAsync(uri);
                    progress?.Report(response);
                    domain.Response = response.ReasonPhrase;
                    return domain;
                }
                catch (HttpRequestException)
                {
                    var response = new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
                    {
                        RequestMessage = new HttpRequestMessage(HttpMethod.Get, uri)
                    };
                    progress?.Report(response);
                    domain.Response = response.ReasonPhrase;
                    return domain;
                }
                catch (Exception)
                {
                    var response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                    {
                        RequestMessage = new HttpRequestMessage(HttpMethod.Get, uri)
                    };

                    progress?.Report(response);
                    domain.Response = response.ReasonPhrase;
                    return domain;
                }
            }
        } 
    }
}
