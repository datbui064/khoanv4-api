namespace KhoaNVCB_API.Dtos // Cập nhật namespace tương ứng bên Client
{
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}