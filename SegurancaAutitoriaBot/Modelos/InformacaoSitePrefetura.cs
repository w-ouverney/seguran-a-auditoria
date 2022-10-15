namespace SegurancaAutitoriaBot.Modelos
{
    public class InformacaoSitePrefetura
    {
        public string Site { get; set; }

        public bool RedirecionouParaHttps { get; set; }

        public bool? AcessouComHttps { get; set; }

        public bool PossuiPoliticaDeCookies { get; set; }

        public int TotalCookies { get; set; }

        public string NomePrefeitura { get; set; }
    }
}
