using System;

namespace DDGo
{
  class Program
  {
    static void Main(string[] args)
    {
      DDGoReq dd = new DDGoReq();
      string search_string = "simpsons characters";
      //string search_string = "dogs";
      string jsonResults = dd.Crawl(search_string, true);
      Console.WriteLine(jsonResults);
    }
  }
}
