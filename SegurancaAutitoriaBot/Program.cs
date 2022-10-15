using HtmlAgilityPack;
using PuppeteerSharp;
using SegurancaAutitoriaBot;
using SegurancaAutitoriaBot.Modelos;
using System.Linq;
using System.Net;

//string arquivoCSV = Path.Combine(Environment.CurrentDirectory, "municipios.csv");

string caminhoChromium = null;

string sitesMunicipios = Path.Combine(Environment.CurrentDirectory,"sites-municipios.csv");


GerarCSVSitesMunicipiosPorUF(sitesMunicipios, "rj").Wait();
//LerSitesCSV(sitesMunicipios).Wait();

async Task GerarCSVSitesMunicipiosPorUF(string caminhoArquivo, string uf)
{
    IEnumerable<IEnumerable<string>> linhasCSV = LerLinhasCSV(caminhoArquivo);
    uf = uf.ToLower();

    var sitesUF = linhasCSV.Where(x => x.ElementAt(1).Replace("\"", "").ToLower().Equals(uf))
                           .ToList();

    await TestarHTTPsSites(sitesUF);
}

async Task LerSitesCSV(string caminhoArquivo)
{
    IEnumerable<IEnumerable<string>> linhasCSV = LerLinhasCSV(caminhoArquivo);

    //if (linhasCSV.Any() is false)
    //    return Enumerable.Empty<object>();

    var sitesParaVerificar = new List<string[]>();

    foreach (string[] linha in linhasCSV)
    {
        if (linha[3].Contains(linha[2].ToLower().Replace(" ", "").RemoveInvalidCharacter()) is false)
            sitesParaVerificar.Add(linha);
    }
}

async Task TestarHTTPsSites(IEnumerable<IEnumerable<string>> prefeituras)
{
    foreach (var prefeitura in prefeituras)
    {
        var uri = new Uri(prefeitura.ElementAt(3).Replace("\"", ""));
        var ip = Dns.GetHostAddresses(uri.Host);
        Console.WriteLine($"{ip[0]} - {prefeitura.ElementAt(3).Replace("\"", "")}");
    }

    if (string.IsNullOrWhiteSpace(caminhoChromium))
    {
        string diretorioBrowser = Environment.CurrentDirectory;
        var instanciaPuppeteer = new BrowserFetcher(new BrowserFetcherOptions
        {
            Path = diretorioBrowser
        });

        var result = await instanciaPuppeteer.DownloadAsync();

        caminhoChromium = result.ExecutablePath;
    }

    var browser = await Puppeteer.LaunchAsync(new LaunchOptions
    {
        DefaultViewport = null,
        Headless = false,
        ExecutablePath = caminhoChromium,
        IgnoreHTTPSErrors = true,
        Timeout = 1000 * 60 * 60,
        Args = new string[]
        {
            "--no-sandbox",
            //"--disable-gpu",
            "--disable-setuid-sandbox",
            "--ignore-certificate-errors"
        }
    });

    List<InformacaoSitePrefetura> informacoesSitePrefetura = new List<InformacaoSitePrefetura>();


    foreach (var prefeitura in prefeituras)
    {
        string site = prefeitura.ElementAt(3).Replace("\"", "");

        site = site.Split("gov.br").First().ToString() + "gov.br";

        bool redirecionouParaHttps = false;

        string siteSemHttps = site.Replace("https", "http");

        var pagina = await browser.NewPageAsync();
        await pagina.GoToAsync(siteSemHttps, WaitUntilNavigation.Load);

        Thread.Sleep(3000);

        redirecionouParaHttps = pagina.Url.ToLower().StartsWith("https");

        var html = await pagina.GetContentAsync();

        var possuipoliticaDeCookies = html.ToLower().Contains("cookies");
        var totalCookies = pagina.GetCookiesAsync().Result.Count();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        string[] nomesBotoesCookies = new string[] { "aceito", "concordo", "aceitar todos" };

        var botaoCookie = doc.DocumentNode.SelectNodes("//button")
                                          .Where(x =>
                                          {
                                              var innerText = x.InnerText.ToLower();

                                              return nomesBotoesCookies.Any(c => innerText.Contains(c));
                                          }).ToList();

        var ancoraCookie = doc.DocumentNode.SelectNodes("//a")
                                           .Where(x =>
                                           {
                                               var innerText = x.InnerText.ToLower();
                                           
                                               return nomesBotoesCookies.Any(c => innerText.Contains(c));
                                           }).ToList();

        if (botaoCookie.Any() || ancoraCookie.Any())
        {

        }

        await pagina.CloseAsync();

        bool? acessouComHttps = null;

        if (redirecionouParaHttps is false)
        {
            string siteComHttps = siteSemHttps.Replace("http", "https");

            pagina = await browser.NewPageAsync();
            await pagina.GoToAsync(siteComHttps, WaitUntilNavigation.Load);

            Thread.Sleep(3000);

            acessouComHttps = pagina.Url.ToLower().StartsWith("https");

            await pagina.CloseAsync();
        }

        informacoesSitePrefetura.Add(new InformacaoSitePrefetura
        {
            Site = site,
            RedirecionouParaHttps = redirecionouParaHttps,
            AcessouComHttps = acessouComHttps,
            NomePrefeitura = prefeitura.ElementAt(0),
            PossuiPoliticaDeCookies = possuipoliticaDeCookies,
            TotalCookies = totalCookies
        });
    }

    await browser.CloseAsync();

    File.WriteAllText($@".\resultado_prefeituras_rj.json", System.Text.Json.JsonSerializer.Serialize(informacoesSitePrefetura));
}

async Task PreencherSite(Municipio municipio, int tentativas)
{
    try
    {
        string html = PesquisarNoDuckDuckGoSitePrefeitura(municipio).Result;
        string link = ExtrairUrlSitePrefeitura(html);
        municipio.UrlSite = link;
    }
    catch (Exception e)
    {
        if (tentativas > 0)
        {
            Thread.Sleep(10 * 1000);

            await PreencherSite(municipio, tentativas - 1);
        }
    }
}

IEnumerable<Municipio> LerMunicipiosCSV(string caminhoArquivo)
{
    IEnumerable<IEnumerable<string>> linhasCSV = LerLinhasCSV(caminhoArquivo);

    if (linhasCSV.Any() is false)
        return Enumerable.Empty<Municipio>();

    var municipios = new List<Municipio>();

    foreach (string[] linha in linhasCSV)
    {
        var municipio = new Municipio
        {
            Nome = linha[4].Replace("\"", ""),
            Uf = linha[3].Replace("\"", "")
        };
    }

    return municipios;
}

IEnumerable<IEnumerable<string>> LerLinhasCSV(string caminhoArquivo)
{
    if (File.Exists(caminhoArquivo) is false)
        return Enumerable.Empty<string[]>();

    var linhas = File.ReadLines(caminhoArquivo)
                     .Skip(1)
                     .Select(linha => linha.Split(';'))
                     .ToList();

    return linhas;
}

async Task<string> PesquisarNoDuckDuckGoSitePrefeitura(Municipio municipio)
{
    if (string.IsNullOrWhiteSpace(caminhoChromium))
    {
        string diretorioBrowser = Environment.CurrentDirectory;
        var instanciaPuppeteer = new BrowserFetcher(new BrowserFetcherOptions
        {
            Path = diretorioBrowser
        });

        var result = await instanciaPuppeteer.DownloadAsync();

        caminhoChromium = result.ExecutablePath;
    }

    municipio.SearchUrl = $"https://duckduckgo.com/?q={municipio.Nome.Replace(" ", "+")}+{municipio.Uf}+gov.br";

    var browser = await Puppeteer.LaunchAsync(new LaunchOptions
    {
        Headless = false,
        ExecutablePath = caminhoChromium,
        IgnoreHTTPSErrors = true,
        Args = new string[]
        {
            "--no-sandbox",
            "--disable-gpu",
            "--disable-setuid-sandbox",
            "--ignore-certificate-errors"
        } 
    });

    var pagina = await browser.NewPageAsync();
    await pagina.GoToAsync(municipio.SearchUrl, WaitUntilNavigation.Load);

    Thread.Sleep(2000);

    var html = await pagina.GetContentAsync();

    await pagina.CloseAsync();
    await browser.CloseAsync();

    return html;
}

string ExtrairUrlSitePrefeitura(string html)
{
    try
    {
        var documentoHtml = new HtmlDocument();
        documentoHtml.LoadHtml(html);

        var link = documentoHtml.DocumentNode.SelectNodes("//div[@id='links']//div//article//div//div//a[1]")
                                             .FirstOrDefault(x => x.LastChild.Name.Equals("span"))
                                             ?.GetAttributeValue<string>("href", ""); // DuckDuckGo

        return link;
    }
    catch (Exception e)
    {
        throw e;
    }
}

