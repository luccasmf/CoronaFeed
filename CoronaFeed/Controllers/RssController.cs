using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MoreLinq;
using HtmlAgilityPack;
using System.Net.Http;

namespace CoronaFeed.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RssController : ControllerBase
    {
        private List<SyndicationItem> _postings;
        private readonly List<string> _urls;
        public RssController(List<string> urls) //recebe a lista de urls do appsettings, adicionada ao serviço da aplicaçao
        {
            _urls = urls;
        }

        [ResponseCache(Duration = 1800)] //cache de meia hora antes de buscar denovo novas notícias
        [HttpGet]
        public IActionResult GetRSS()
        {
            //configura o Feed
            _postings = new List<SyndicationItem>();
            var feed = new SyndicationFeed("CoronaFeed", "Feed para centralização de notícias sobre o Coronavirus", new Uri("https://github.com/luccasmf/CoronaFeed"), "RSSUrl", DateTime.Now);
            feed.Copyright = new TextSyndicationContent($"{DateTime.Now.Year} - Public Domain");

            var items = new List<SyndicationItem>();
            var tasks = new List<Task>();

            //enfileira tasks para rodar todas ao mesmo tempo
            foreach (string feedUrl in _urls)
            {
                tasks.Add(Task.Run(() => ReadRSS(feedUrl)));
            }
            Task t = Task.WhenAll(tasks);
            t.Wait();
            
            //ao terminar de adicionar todos os feeds, atribui ao feed criado
            feed.Items = _postings;

            //ordena feed e tira duplicados
            feed.Items = feed.Items.DistinctBy(x => x.Title.Text).OrderByDescending(x => x.PublishDate).ToList();

            //formata para XML
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                NewLineHandling = NewLineHandling.Entitize,
                NewLineOnAttributes = true,
                Indent = true
            };
            using (var stream = new MemoryStream())
            {
                using (var xmlWriter = XmlWriter.Create(stream, settings))
                {
                    var rssFormatter = new Rss20FeedFormatter(feed, false);
                    rssFormatter.WriteTo(xmlWriter);
                    xmlWriter.Flush();
                }
                return File(stream.ToArray(), "application/rss+xml; charset=utf-8");
            }
        }

        //le os RSS
        private async void ReadRSS(string url)
        {
            try
            {
                var reader = XmlReader.Create(url);
                var feed = SyndicationFeed.Load(reader);
               
                _postings.AddRange(feed.Items.Where(
                    x => x.Title.Text.HasKeywords() ||
                    x.Id.HasKeywords()));
            }
            //string[] filtro = new string[] { "Covid", "covid", "Corona", "corona" };
            catch
            {

            }
            
        }

        [HttpGet("Tracker")]
        public async Task<string> Tracker()
        {
            

            var date = DateTime.Now;
            var url = $"https://bnonews.com/index.php/{date.Year}/{date.Month.ToString("00")}/the-latest-coronavirus-cases/";
            var xpath = "//*[@id=\"0\"]/div/table/tbody";

            var client = new HttpClient();
            var response = await client.GetAsync(url);
            var pageContents = await response.Content.ReadAsStringAsync();

            var pageDocument = new HtmlDocument();
            pageDocument.LoadHtml(pageContents);

            var tableCases = pageDocument.DocumentNode.SelectSingleNode(xpath).InnerText;
            //Console.WriteLine(tableCases);
            return tableCases;
        }
    }
}