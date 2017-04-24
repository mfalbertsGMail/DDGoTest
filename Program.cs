using System;

namespace DDGo
{
  class Program
  {
    static void Main(string[] args)
    {
      DDGoService dd = new DDGoService();
      string search_string = "simpsons characters";
      //string search_string = "dogs";
      string jsonResults = dd.Crawl(search_string, false);
      Console.WriteLine(jsonResults);
    }
  }
}
