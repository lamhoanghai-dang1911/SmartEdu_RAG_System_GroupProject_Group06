namespace SmartEdu.Shared.Enums
{
    public enum DocumentStatus
    {
        Pending = 0,     // vừa upload, chưa xử lý
        Processing = 1,  // đang chunking/embedding
        Ready = 2,       // đã embed xong, sẵn sàng RAG
        Failed = 3       // lỗi trong quá trình xử lý
    }
}
