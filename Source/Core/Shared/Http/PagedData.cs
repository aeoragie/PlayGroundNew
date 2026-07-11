namespace PlayGround.Shared.Http
{
    public class PagedData<T>
    {
        public List<T> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int Size { get; set; }
        public int TotalPages => Size <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)Size);

        public PagedData() { }

        public PagedData(List<T> items, int totalCount, int page, int size)
        {
            Items = items;
            TotalCount = totalCount;
            Page = page;
            Size = size;
        }
    }
}
