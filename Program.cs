using System;

namespace DDGo
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("Hello World!");
      Console.WriteLine(System.Diagnostics.Process.GetCurrentProcess().Threads.Count);
      DDGoReq dd = new DDGoReq();
      string search_string = "simpsons characters";
      //string search_string = "dogs";
      string jsonResults = dd.Crawl(search_string, true);
      Console.WriteLine(jsonResults);
      //List<string> urls = dd.Crawl("c# string.format", false); // fix special characters!
      //List<string> urls = dd.Crawl("dogs", false); // fix special characters!
      jsonResults = dd.Crawl(search_string, false);
      Console.WriteLine(jsonResults);
    }
  }
}
