﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DDGoTest
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("Hello World!");
      Console.WriteLine(System.Diagnostics.Process.GetCurrentProcess().Threads.Count);
      DDGoReq dd = new DDGoReq();
      //string search_string = "simpsons characters";
      string search_string = "beer";
      dd.Crawl(search_string, true);
      //List<string> urls = dd.Crawl("c# string.format", false); // fix special characters!
      //List<string> urls = dd.Crawl("dogs", false); // fix special characters!
      dd.Crawl(search_string, false);
    }
  }
}