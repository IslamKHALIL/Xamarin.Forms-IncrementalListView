using System.Threading.Tasks;

namespace XF.IncrementalListView
{
    public interface IIncrementalSource
    {
        Task GetPagedItemsAsync(int pageIndex, int pageSize);
    }
}
