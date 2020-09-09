using System.Threading.Tasks;

namespace PromDapterDeclarations
{
    public delegate Task<DataItem[]> GetDataItems(object[] parameters);
    public delegate Task Open(params object[] parameters);
    public delegate Task Close(params object[] parameters);

    public interface IPromDapterService
    {
        Open Open { get; }
        GetDataItems GetDataItems { get; }
        Close Close { get; }
    }
}