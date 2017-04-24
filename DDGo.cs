using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Diagnostics;


namespace DDGo
{
  class DDGoStats
  {
    public string[] SearchTerms { get; set; }
    public string RequestStart { get; set; }
    public int DocumentCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public long CrawlAsyncTimeMs { get; set;  }
    public long CrawlSyncTimeMs { get; set; }
    public bool SkipSyncTest { get; set; }
    public long TotalElapsedTimeMs { get; set; }
  }

  class  HTTPStats
  {
    public string URL { get; set; }
    public DateTime RequestStart { get; set; }
    public int SizeBytes { get; set; }
    public long ElapsedTimeMs { get; set; }
    public bool Success { get; set; }
  }

  public class DDGoReq
  {
    public DDGoReq()
    {
      return;
    }

    public string Crawl(string searchTerms, bool skipSyncTest = false)
    {
      DDGoStats ddGoStats = new DDGoStats() { RequestStart = DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") };
      ddGoStats.SearchTerms = searchTerms.Split(' ');
      ddGoStats.SkipSyncTest = skipSyncTest;
      Stopwatch sw = new Stopwatch();
      sw.Start();
      string searchTermsFmt =searchTerms.Replace(' ', '+'); // todo: need to encode this!
      string DDSearchURL = string.Format(@"http://api.duckduckgo.com/?q={0}&format=json&pretty=1", searchTermsFmt);
      HTTPStats httpStatsGetRelatedTopics = new HTTPStats();
      List<string> searchUrls = GetRelatedTopics(DDSearchURL, httpStatsGetRelatedTopics);
      ddGoStats.DocumentCount = searchUrls.Count;
      // do Async version
      {
        Stopwatch swAsync = new Stopwatch();
        swAsync.Start();
        List<Task<HTTPStats>> tasks = new List<Task<HTTPStats>>();
        searchUrls.ForEach(url => tasks.Add(GetHTTPStatsAsync(url)));
        Task.WaitAll(tasks.ToArray());
        swAsync.Stop();
        ddGoStats.TotalSizeBytes = tasks.Sum(task => task.Result.SizeBytes) + httpStatsGetRelatedTopics.SizeBytes;
        ddGoStats.CrawlAsyncTimeMs = swAsync.ElapsedMilliseconds;
      } 
      // do Sync verion
      if (skipSyncTest == false)
      {
        Stopwatch swAsync = new Stopwatch();
        swAsync.Start();
        List<HTTPStats> stats = new List<HTTPStats>(); // don't really need this for this test
        searchUrls.ForEach(url => stats.Add(GetHTTPStatsAsync(url).Result));
        swAsync.Stop();
        ddGoStats.CrawlSyncTimeMs = swAsync.ElapsedMilliseconds;
      }
      sw.Stop();
      ddGoStats.TotalElapsedTimeMs = sw.ElapsedMilliseconds;
      Console.WriteLine("Elapsed Time (ms) = " + sw.ElapsedMilliseconds);
      return JsonConvert.SerializeObject(ddGoStats, Formatting.Indented);
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


