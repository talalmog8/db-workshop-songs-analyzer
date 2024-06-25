namespace Model;

public interface IDatasetLoader
{
    Task LoadAllDataset(string filePath);
}