# SmartEdu RAG System 🎓🤖

Hệ thống hỗ trợ học tập thông minh dựa trên kiến thuật **RAG (Retrieval-Augmented Generation)**. Dự án được phát triển nhằm giúp sinh viên truy vấn kiến thức từ tài liệu môn học một cách nhanh chóng và chính xác.

## 🌟 Tính năng chính
- **Quản lý tài liệu:** Hỗ trợ upload PDF, DOCX. Tự động xử lý Text Extraction và Chunking.
- **Hỏi đáp thông minh:** Chatbot trả lời dựa trên ngữ cảnh tài liệu, có trích dẫn nguồn gốc.
- **Nghiên cứu RBL (Research-Based Learning):** Module thực nghiệm so sánh hiệu quả giữa các Embedding Model (PhoBERT, e5, OpenAI) và các chiến lược Chunking khác nhau.
- **Real-time Interaction:** Tích hợp SignalR cho trải nghiệm chat phản hồi tức thì (Dự kiến hoàn thiện trong bản Final).

## 🏗 Kiến trúc hệ thống (3-Layers Architecture)
Dự án tuân thủ nghiêm ngặt kiến trúc 3 lớp để đảm bảo tính mở rộng và dễ bảo trì:
- **SmartEdu.Web:** Presentation Layer - Sử dụng ASP.NET Core MVC (ASM1) và Razor Pages (ASM2).
- **SmartEdu.Business:** Business Logic Layer (BLL) - Xử lý nghiệp vụ AI, RAG logic và điều phối dữ liệu.
- **SmartEdu.Data:** Data Access Layer (DAL) - Quản lý cơ sở dữ liệu PostgreSQL và các thao tác với Entity Framework Core.
- **SmartEdu.Shared:** Common Layer - Chứa các Entities, DTOs và cấu hình dùng chung cho toàn bộ Solution.

## 🛠 Công nghệ sử dụng
- **Framework:** .NET 8.0 (LTS)
- **Database:** MySQL (Hỗ trợ Vector storage cho RAG)
- **AI Integration:** Semantic Kernel / LangChain (.NET), OpenAI API, PhoBERT cho tiếng Việt.
- **Frontend:** Bootstrap 5, AJAX, SignalR.

## 🚀 Hướng dẫn cài đặt nhanh
1. **Clone dự án:**
   ```bash
   git clone [https://github.com/lamhoanghai-dang1911/SmartEdu_RAG_System.git](https://github.com/lamhoanghai-dang1911/SmartEdu_RAG_System.git)