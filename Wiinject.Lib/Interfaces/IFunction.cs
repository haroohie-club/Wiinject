namespace Wiinject.Interfaces
{
    public interface IFunction
    {
        public string Name { get; set; }
        public uint EntryPoint { get; set; }
        bool Existing { get; }
    }
}
