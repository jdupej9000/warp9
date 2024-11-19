namespace Warp9.Utils
{
    public interface IUntypedTableProvider
    {
        public IEnumerable<string[]> ParsedData { get; }
        public string WorkingDirectory { get; }
    }
}
