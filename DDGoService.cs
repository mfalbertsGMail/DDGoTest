using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Diagnostics;


namespace DDGoService
{
  class DDGoStats
  {
    public string[] SearchTerms { get; set; }
    public string RequestStart { get; set; }
    public int DocumentCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public long CrawlAsyncTimeMs { get; set;  }
    public long CrawlSyncTimeMs { get; set; }
    public long TotalElapsedTimeMs { get; set; }
    public bool SkipSynchronousTest { get; set; }
    public List<HTTPStat> DetailStatsAsync { get; set; }
    public List<HTTPStat> DetailStatsSync { get; set; }
  }

  class  HTTPStat
  {
    public string URL { get; set; }
    public DateTime RequestStart { get; set; }
    public int SizeBytes { get; set; }
    public long ElapsedTimeMs { get; set; }
    public bool Success { get; set; }
  }

  public class DDGoCrawlService
  {
    public DDGoCrawlService()
    {
      return;
    }
    public string Crawl(string searchTerms, bool skipSynchronousTest, bool saveDetailStats = false)
    {
      Stopwatch swAction = new Stopwatch();
      swAction.Start();
      DDGoStats ddGoStats = new DDGoStats() { RequestStart = DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") };
      ddGoStats.SearchTerms = searchTerms.Split(' ');
      ddGoStats.SkipSynchronousTest = skipSynchronousTest;
      // normal - asynchronous operation
      {
        Stopwatch swCrawl = new Stopwatch();
        swCrawl.Start();
        List<HTTPStat> httpStats = RunCrawlAction(searchTerms, true);
        swCrawl.Stop();
        ddGoStats.CrawlAsyncTimeMs = swCrawl.ElapsedMilliseconds;

        ddGoStats.DocumentCount = httpStats.Count;
        ddGoStats.TotalSizeBytes = httpStats.Sum(stat => stat.SizeBytes);
        if (saveDetailStats == true)
          ddGoStats.DetailStatsAsync = httpStats; // save individual statistics 
      }
      if (skipSynchronousTest == false)
      {
        Stopwatch swCrawl = new Stopwatch();
        swCrawl.Start();
        List<HTTPStat> httpStats = RunCrawlAction(searchTerms, false);
        swCrawl.Stop();
        ddGoStats.CrawlSyncTimeMs = swCrawl.ElapsedMilliseconds;
        if (saveDetailStats == true)
          ddGoStats.DetailStatsSync = httpStats; // save individual statistics
      }
      swAction.Stop();
      ddGoStats.TotalElapsedTimeMs = swAction.ElapsedMilliseconds;
      return JsonConvert.SerializeObject(ddGoStats, Formatting.Indented);
    }

    
    private List<HTTPStat> RunCrawlAction(string searchTerms, bool runAsync = false)
    {
      string searchTermsFmt = searchTerms.Replace(' ', '+'); // todo: need to encode this!
      string DDSearchURL = string.Format(@"http://api.duckduckgo.com/?q={0}&format=json&pretty=1", searchTermsFmt);
      HTTPStat httpStatsGetRelatedTopics = new HTTPStat();
      List<string> searchUrls = GetRelatedTopics(DDSearchURL, httpStatsGetRelatedTopics);

      // create list to hold stats for all HTTP requests
      List<HTTPStat> httpStats = new List<HTTPStat>
      {
        httpStatsGetRelatedTopics
      };
      if (runAsync == true)
      {
        List<Task<HTTPStat>> tasks = new List<Task<HTTPStat>>();
        searchUrls.ForEach(url => tasks.Add(QueryURLAsync(url)));
        Task.WaitAll(tasks.ToArray());
        // move the stats into the return list
        tasks.ForEach(task => httpStats.Add(task.Result));
      }
      else
      {
        searchUrls.ForEach(url => httpStats.Add(QueryURLAsync(url).Result));
      }
      return httpStats;
    }

    private List<string> GetRelatedTopics(string DDSearchURL, HTTPStat httpStats)
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

      // start of method 
      httpStats.RequestStart = DateTime.Now;
      httpStats.URL = DDSearchURL;
      Stopwatch watch = Stopwatch.StartNew();
      watch.Start();
      List<string> relatedTopicsUrls = new List<string>();
      try
      {
        var httpClient = new HttpClient();
        var response = httpClient.GetAsync(DDSearchURL).Result;
        var content = response.Content.ReadAsStringAsync().Result;
        httpStats.SizeBytes = content.Length;
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
        httpStats.ElapsedTimeMs = watch.ElapsedMilliseconds;
        watch.Stop();
      }
      return relatedTopicsUrls;
    }

    private async Task<HTTPStat> QueryURLAsync(string url)
    {
      HTTPStat httpStats = new HTTPStat() { RequestStart = DateTime.Now, URL = url };
      Stopwatch sw = new Stopwatch();
      sw.Start();
      try
      {
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        httpStats.SizeBytes = content.Length;
        httpStats.Success = true;
      } catch (Exception)
      {
        httpStats.Success = false;
      }
      finally
      {
        sw.Stop();
        httpStats.ElapsedTimeMs = sw.ElapsedMilliseconds;
      }
      return httpStats;

    }
  }
}


