// https://mfalbertsGMail:deloitte2016!@github.com/mfalbertsGMail/DDGoTest.git

using System;
using DDGoService;

namespace DDGo
{
  class Program
  {
    static void Main(string[] args)
    {
      DDGoCrawlService dd = new DDGoCrawlService();
      string search_string = "simpsons characters";
      //string search_string = "dogs";
      string jsonResults = dd.Crawl(searchTerms: search_string, skipSynchronousTest: true, saveDetailStats: true);
      Console.WriteLine(jsonResults);
    }
  }
}
// https://stormpath.com/blog/routing-in-asp-net-core