using SegurancaAutitoriaBot.Modelos;

string arquivoCSV = Path.Combine(Environment.CurrentDirectory, "municipios.csv");


var municipios = LerMunicipiosCSV(arquivoCSV);

foreach (var municipio in municipios)
{
    Console.WriteLine($"{municipio.Uf} - {municipio.Nome}");
}

Console.WriteLine(arquivoCSV);

IEnumerable<Municipio> LerMunicipiosCSV(string caminhoArquivo)
{
	if (File.Exists(caminhoArquivo) is false)
        return Enumerable.Empty<Municipio>();

    var stream = new StreamReader(caminhoArquivo);

    bool isHeader = true;
    var municipios = new List<Municipio>();

    while (stream.EndOfStream is false)
    {
        var linha = stream.ReadLine();

        if (isHeader)
        {
            isHeader = false;
            continue;
        }

        var colunasLinha = linha.Split(',');

        var municipio = new Municipio
        {
            Nome = colunasLinha[4].Replace("\"", ""),
            Uf = colunasLinha[3].Replace("\"", "")
        };

        municipios.Add(municipio);
    }

    return municipios;
}