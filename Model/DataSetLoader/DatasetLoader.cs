
using Newtonsoft.Json;

namespace Model;

public class DatasetLoader(ILoggerFactory loggerFactory, ISongAnalyzer songAnalyzer) : IDatasetLoader
{
    public async Task LoadAllDataset(string filePath)
    {
        try
        {
            using var streamReader = new StreamReader(filePath);
            await using var jsonReader = new JsonTextReader(streamReader);

            var serializer = new JsonSerializer();

            while (await jsonReader.ReadAsync())
            {
                if (jsonReader.TokenType == JsonToken.StartObject)
                {
                    var songContent = serializer.Deserialize<SongContent>(jsonReader);

                    if (songContent is null)
                        continue;
                    if (songContent.Song is null)
                        continue;
                    if (songContent.Artist is null)
                        continue;
                    if (songContent.Text is null)
                        continue;

                    songContent.Song = songContent.Song.TrimToMaxLength(45);
                    songContent.Artist = songContent.Artist.TrimToMaxLength(45);
                    songContent.Text = songContent.Text.ToLower();

                    songAnalyzer.LoadSong(Path.Combine(filePath, songContent.Song), songContent.Text);
                    var processSong = await songAnalyzer.ProcessSong();
                    if (processSong == ProcessingResult.Succeeded)
                    {
                        var artistName = songContent.Artist.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var names = new HashSet<Name>
                        {
                            new Name(artistName.ElementAtOrDefault(0)?.ToLower() ?? "unknown", artistName.ElementAtOrDefault(1)?.ToLower() ?? "unknown")
                        };
                        await songAnalyzer.InsertContributorsIfMissing(names, ContributorType.Performer, songAnalyzer.Song);    
                    }
                }
            }
        }
        catch (Exception e)
        {
            loggerFactory.CreateLogger<DatasetLoader>().LogError(e, "Failed To Load DataSet");
        }
    }
}