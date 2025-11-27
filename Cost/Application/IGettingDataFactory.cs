namespace Cost.Application
{
    public interface IGettingDataFactory
    {
        IGettingData Create(string type);
    }
}
