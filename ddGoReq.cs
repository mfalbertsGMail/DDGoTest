﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Diagnostics;


namespace DDGoTest
{
  public class  HTTPStats
  {
    public string URL { get; set; }
    public DateTime RequestStart { get; set; }
    public int Size { get; set; }
    public long ElapsedTimeMS { get; set; }
    public bool Success { get; set; }
  }

  public class DDGoReq
  {
    public DDGoReq()
    {
      return;
    }

    public string Crawl(string searchTerms, bool async)
    {
      Stopwatch sw = new Stopwatch();
      sw.Start();
      string searchTermsFmt =searchTerms.Replace(' ', '+'); // todo: need to encode this!
      string DDSearchURL = string.Format(@"http://api.duckduckgo.com/?q={0}&format=json&pretty=1", searchTermsFmt);
      HTTPStats httpStats = new HTTPStats();
      List<string> searchUrls = GetRelatedTopics(DDSearchURL, httpStats);
      string jsonResult = "";
      //searchUrls.ForEach(url => Console.Write("{0}\t", url));
      List<HTTPStats> stats = new List<HTTPStats>();
      if (async == true)
      {
        Task<HTTPStats>[] tasks = new Task<HTTPStats>[searchUrls.Count];
        int i = 0;
        searchUrls.ForEach(url => tasks[i++] = GetHTTPStatsAsync(url));
        Task.WaitAll(tasks);
      } else
      {
        searchUrls.ForEach(url => stats.Add(GetHTTPStatsAsync(url).Result));
      }
      sw.Stop();
      Console.WriteLine("Elapsed Time (ms) = " + sw.ElapsedMilliseconds);
      return null;
    }

    private List<string> GetRelatedTopics(string DDSearchURL, HTTPStats httpStats)
    {
      // embedded helper - the "ReleatedTopics" can contain
      // multiple levels of results - normal 'results' and then Topics with
      // their own 'results' (i.e. search for 'dogs')
      void WalkChildren(JToken head, List<string> urlList)
      {
        if (head.Children().Count() > 0)
          foreach(JToken token in head.Children())
              WalkChildren(token, urlList);
        if (head.SelectToken("FirstURL") != null)
          urlList.Add(head["FirstURL"].ToString());
      }

      httpStats.RequestStart = DateTime.Now;
      httpStats.URL = DDSearchURL;
      var watch = Stopwatch.StartNew();
      watch.Start();
      List<string> relatedTopicsUrls = new List<string>();
      try
      {
        var httpClient = new HttpClient();
        var response = httpClient.GetAsync(DDSearchURL).Result;
        var content = response.Content.ReadAsStringAsync().Result;
        httpStats.Size = content.Length;
        JObject relatedTopicsSearch = JObject.Parse(content);
        IList<JToken> relatedTopics = relatedTopicsSearch["RelatedTopics"].Children().ToList();
        foreach (JToken token in relatedTopics)
          WalkChildren(token, relatedTopicsUrls);
        httpStats.Success = true;
      } catch (Exception)
      {
        httpStats.Success = false;
      }
      finally
      {
        httpStats.ElapsedTimeMS = watch.ElapsedMilliseconds;
        watch.Stop();
      }
      return relatedTopicsUrls;
    }

    private async Task<HTTPStats> GetHTTPStatsAsync(string url)
    {
      HTTPStats httpStats = new HTTPStats() { RequestStart = DateTime.Now, URL = url };
      Stopwatch sw = new Stopwatch();
      sw.Start();
      try
      {
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        httpStats.Size = content.Length;
        httpStats.Success = true;
      } catch (Exception)
      {
        httpStats.Success = false;
      }
      finally
      {
        sw.Stop();
        httpStats.ElapsedTimeMS = sw.ElapsedMilliseconds;
      }
      return httpStats;

    }
  }
}


/*
 * 
 * 
 * 
 async Task Foo(){
    try{
        var res = await myObject.CallMethodReturningTaskOrAsyncMethod();
        doSomethingWithRes();
    } catch(e){
         // handle errors, this will be called if the async task errors
    } finally {
        // this is your .always
    }
}
*/