using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartEdu.Business.Interfaces;
using SmartEdu.Data.Repositories;
using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Entities;
using SmartEdu.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartEdu.Business.Services
{
    public class ModelBenchmarkService : IModelBenchmarkService
    {
        private readonly IRepository<DocumentChunk> _chunkRepo;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ModelBenchmarkService> _logger;

        public ModelBenchmarkService(
            IRepository<DocumentChunk> chunkRepo,
            IHttpClientFactory httpFactory,
            IConfiguration configuration,
            ILogger<ModelBenchmarkService> logger)
        {
            _chunkRepo = chunkRepo;
            _httpFactory = httpFactory;
            _configuration = configuration;
            _logger = logger;
        }

        // =====================================================
        // EMBEDDING BENCHMARK
        // =====================================================

        public async Task<IReadOnlyList<EmbeddingBenchmarkResultDto>>
            CompareEmbeddingModelsAsync(EmbeddingBenchmarkRequestDto request)
        {
            ValidateEmbeddingRequest(request);

            var chunks = await _chunkRepo.GetAllWithIncludeAsync(
                c => c.EmbeddingSet != null
                     && c.EmbeddingSet.Status == EmbeddingSetStatus.Ready
                     && c.EmbeddingSet.Documents.Any(
                         d => d.SubjectId == request.SubjectId
                              && !d.IsDeleted),
                c => c.EmbeddingSet,
                c => c.EmbeddingSet.Documents
            );

            var candidateChunks = chunks
                .Where(c => !string.IsNullOrWhiteSpace(c.Content))
                .OrderBy(c => c.Id)
                .Take(request.CandidateLimit)
                .ToList();

            if (candidateChunks.Count == 0)
            {
                throw new Exception(
                    "Không tìm thấy chunk nào thuộc môn học đã chọn.");
            }

            var results = new List<EmbeddingBenchmarkResultDto>();

            foreach (var model in request.Models.Distinct())
            {
                var result = await BenchmarkEmbeddingModelAsync(
                    model,
                    request,
                    candidateChunks);

                results.Add(result);
            }

            return results
                .OrderByDescending(r => r.RecallAtK ?? 0)
                .ThenByDescending(r => r.AverageTopKScore)
                .ThenBy(r => r.ElapsedMs)
                .ToList();
        }

        private async Task<EmbeddingBenchmarkResultDto>
            BenchmarkEmbeddingModelAsync(
                string model,
                EmbeddingBenchmarkRequestDto request,
                List<DocumentChunk> chunks)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var queryVector = await GetHuggingFaceEmbeddingAsync(
                    model,
                    request.Query,
                    isQuery: true);

                var scoredChunks = new List<BenchmarkChunkDto>();

                foreach (var chunk in chunks)
                {
                    var chunkVector = await GetHuggingFaceEmbeddingAsync(
                        model,
                        chunk.Content,
                        isQuery: false);

                    var score = CosineSimilarity(
                        queryVector,
                        chunkVector);

                    var documentTitle =
                        chunk.EmbeddingSet?.CanonicalTitle
                        ?? "Không xác định";

                    scoredChunks.Add(new BenchmarkChunkDto
                    {
                        ChunkId = chunk.Id,
                        ChunkIndex = chunk.ChunkIndex,
                        DocumentTitle = documentTitle,
                        ContentPreview = CreatePreview(
                            chunk.Content,
                            250),
                        Score = Math.Round(score, 6)
                    });
                }

                stopwatch.Stop();

                var topChunks = scoredChunks
                    .OrderByDescending(c => c.Score)
                    .Take(request.TopK)
                    .ToList();

                var recallAtK = CalculateRecallAtK(
                    topChunks,
                    request.ExpectedChunkIds);

                return new EmbeddingBenchmarkResultDto
                {
                    Model = model,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    VectorDimensions = queryVector.Length,
                    TopScore = topChunks.FirstOrDefault()?.Score ?? 0,
                    AverageTopKScore = topChunks.Count == 0
                        ? 0
                        : Math.Round(
                            topChunks.Average(c => c.Score),
                            6),
                    RecallAtK = recallAtK,
                    TopChunks = topChunks,
                    Error = null
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "Lỗi benchmark embedding model {Model}",
                    model);

                return new EmbeddingBenchmarkResultDto
                {
                    Model = model,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Error = ex.Message
                };
            }
        }

        private async Task<float[]> GetHuggingFaceEmbeddingAsync(
            string model,
            string text,
            bool isQuery)
        {
            var token = _configuration["HuggingFace:Token"];

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new Exception(
                    "Thiếu HuggingFace:Token trong cấu hình.");
            }

            var input = PrepareEmbeddingInput(
                model,
                text,
                isQuery);

            var url =
                $"https://router.huggingface.co/hf-inference/models/" +
                $"{model}/pipeline/feature-extraction";

            var client = _httpFactory.CreateClient();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    token);

            var payload = new
            {
                inputs = input,
                options = new
                {
                    wait_for_model = true
                }
            };

            using var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await client.PostAsync(
                url,
                content);

            var responseJson =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Hugging Face lỗi với model {model}: " +
                    responseJson);
            }

            return ExtractEmbeddingVector(responseJson);
        }

        private static string PrepareEmbeddingInput(
            string model,
            string text,
            bool isQuery)
        {
            var normalizedModel = model.ToLowerInvariant();

            // E5 yêu cầu prefix query/passsage để cho kết quả tốt.
            if (normalizedModel.Contains("e5"))
            {
                return isQuery
                    ? $"query: {text}"
                    : $"passage: {text}";
            }

            // MPNet không cần prefix.
            return text;
        }

        private static float[] ExtractEmbeddingVector(
            string json)
        {
            using var document = JsonDocument.Parse(json);

            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Array
                || root.GetArrayLength() == 0)
            {
                throw new Exception(
                    "Hugging Face không trả về embedding hợp lệ.");
            }

            var firstElement = root[0];

            // Trường hợp API trả thẳng vector:
            // [0.1, 0.2, 0.3]
            if (firstElement.ValueKind == JsonValueKind.Number)
            {
                return root
                    .EnumerateArray()
                    .Select(x => x.GetSingle())
                    .ToArray();
            }

            // Trường hợp:
            // [[0.1, 0.2, ...]]
            if (firstElement.ValueKind == JsonValueKind.Array
                && firstElement.GetArrayLength() > 0
                && firstElement[0].ValueKind == JsonValueKind.Number)
            {
                return firstElement
                    .EnumerateArray()
                    .Select(x => x.GetSingle())
                    .ToArray();
            }

            // Trường hợp API trả embedding theo từng token:
            // [[[...], [...], [...]]]
            if (firstElement.ValueKind == JsonValueKind.Array
                && firstElement.GetArrayLength() > 0
                && firstElement[0].ValueKind
                    == JsonValueKind.Array)
            {
                var tokenVectors = firstElement
                    .EnumerateArray()
                    .Select(token => token
                        .EnumerateArray()
                        .Select(x => x.GetSingle())
                        .ToArray())
                    .ToList();

                return MeanPooling(tokenVectors);
            }

            throw new Exception(
                "Không đọc được định dạng embedding.");
        }

        private static float[] MeanPooling(
            List<float[]> vectors)
        {
            if (vectors.Count == 0)
            {
                return Array.Empty<float>();
            }

            var dimension = vectors[0].Length;
            var result = new float[dimension];

            foreach (var vector in vectors)
            {
                if (vector.Length != dimension)
                {
                    continue;
                }

                for (var i = 0; i < dimension; i++)
                {
                    result[i] += vector[i];
                }
            }

            for (var i = 0; i < dimension; i++)
            {
                result[i] /= vectors.Count;
            }

            return result;
        }

        private static double CosineSimilarity(
            float[] first,
            float[] second)
        {
            if (first.Length == 0
                || second.Length == 0
                || first.Length != second.Length)
            {
                return 0;
            }

            double dotProduct = 0;
            double firstMagnitude = 0;
            double secondMagnitude = 0;

            for (var i = 0; i < first.Length; i++)
            {
                dotProduct += first[i] * second[i];

                firstMagnitude += first[i] * first[i];

                secondMagnitude += second[i] * second[i];
            }

            if (firstMagnitude == 0 || secondMagnitude == 0)
            {
                return 0;
            }

            return dotProduct /
                   (Math.Sqrt(firstMagnitude)
                    * Math.Sqrt(secondMagnitude));
        }

        private static double? CalculateRecallAtK(
            List<BenchmarkChunkDto> topChunks,
            List<int> expectedChunkIds)
        {
            if (expectedChunkIds == null
                || expectedChunkIds.Count == 0)
            {
                return null;
            }

            var expectedIds = expectedChunkIds
                .Distinct()
                .ToHashSet();

            var returnedIds = topChunks
                .Select(c => c.ChunkId)
                .ToHashSet();

            var matchedCount = expectedIds
                .Count(returnedIds.Contains);

            return Math.Round(
                (double)matchedCount / expectedIds.Count,
                4);
        }

        // =====================================================
        // CHAT BENCHMARK
        // =====================================================

        public async Task<IReadOnlyList<ChatModelBenchmarkResultDto>>
            CompareChatModelsAsync(ChatModelBenchmarkRequestDto request)
        {
            ValidateChatRequest(request);

            var retrievedData = await RetrieveContextAsync(
                request.SubjectId,
                request.Question,
                request.EmbeddingModel,
                request.TopK);

            if (string.IsNullOrWhiteSpace(retrievedData.Context))
            {
                throw new Exception(
                    "Không tìm thấy nội dung phù hợp trong dữ liệu đã lưu.");
            }

            var tasks = request.Targets
                .Select(target =>
                    BenchmarkChatTargetAsync(
                        target,
                        request,
                        retrievedData.Context,
                        retrievedData.Chunks))
                .ToList();

            var results = await Task.WhenAll(tasks);

            CalculateOverallScores(results);

            return results
                .OrderByDescending(x => x.OverallScore)
                .ThenBy(x => x.ElapsedMs)
                .ToList();
        }

        private async Task<ChatModelBenchmarkResultDto>
            BenchmarkChatTargetAsync(
                ChatBenchmarkTargetDto target,
                ChatModelBenchmarkRequestDto request,
                string retrievedContext,
                List<BenchmarkChunkDto> retrievedChunks)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var provider =
                    target.Provider.Trim().ToLowerInvariant();

                ChatProviderResponse response;

                switch (provider)
                {
                    case "gemini":
                    case "google":
                        response = await CallGeminiAsync(
                            target.Model,
                            request,
                            retrievedContext);
                        break;

                    case "groq":
                        response = await CallGroqAsync(
                            target.Model,
                            request,
                            retrievedContext);
                        break;

                    case "openrouter":
                    case "deepseek":
                        response = await CallOpenRouterAsync(
                            target.Model,
                            request,
                            retrievedContext);
                        break;

                    default:
                        throw new Exception(
                            $"Provider không được hỗ trợ: " +
                            $"{target.Provider}");
                }

                stopwatch.Stop();

                var groundedness = CalculateGroundedness(
                    response.Answer,
                    retrievedContext);
                var relevance = CalculateRelevance(
                    response.Answer,
                    request.Question);
                var keywordCoverage = CalculateKeywordCoverage(
                    response.Answer,
                    request.ExpectedKeywords);
                var completeness = request.ExpectedKeywords?.Any(
                    keyword => !string.IsNullOrWhiteSpace(keyword)) == true
                    ? keywordCoverage
                    : relevance;
                var completionTokenCount = response.CompletionTokens > 0
                    ? response.CompletionTokens
                    : Tokenize(response.Answer).Count;
                var tokensPerSecond = stopwatch.Elapsed.TotalSeconds > 0
                    ? completionTokenCount / stopwatch.Elapsed.TotalSeconds
                    : 0;

                return new ChatModelBenchmarkResultDto
                {
                    Provider = target.Provider,
                    Model = target.Model,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    PromptTokens = response.PromptTokens,
                    CompletionTokens = response.CompletionTokens,
                    KeywordCoveragePercent = keywordCoverage,
                    GroundednessPercent = groundedness,
                    RelevancePercent = relevance,
                    CompletenessPercent = completeness,
                    HallucinationPercent = Math.Round(
                        100 - groundedness,
                        2),
                    TokensPerSecond = Math.Round(tokensPerSecond, 2),
                    Answer = response.Answer,
                    RetrievedContext = retrievedContext,
                    RetrievedChunks = retrievedChunks,
                    Error = null
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "Lỗi benchmark {Provider}/{Model}",
                    target.Provider,
                    target.Model);

                return new ChatModelBenchmarkResultDto
                {
                    Provider = target.Provider,
                    Model = target.Model,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    RetrievedContext = retrievedContext,
                    RetrievedChunks = retrievedChunks,
                    Error = ex.Message
                };
            }
        }

        // =====================================================
        // OPENAI
        // =====================================================

        private async Task<ChatProviderResponse> CallOpenAiAsync(
            string model,
            ChatModelBenchmarkRequestDto request,
            string context)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception(
                    "Thiếu OpenAI:ApiKey.");
            }

            var client = _httpFactory.CreateClient();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    apiKey);

            var payload = new
            {
                model,
                messages = new object[]
                {
                new
                {
                    role = "system",
                    content = CreateSystemPrompt()
                },
                new
                {
                    role = "user",
                    content = CreateUserPrompt(request.Question, context)
                }
                },
                max_completion_tokens =
                    request.MaxOutputTokens
            };

            using var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await client.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                content);

            var json =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"OpenAI API lỗi: {json}");
            }

            using var document = JsonDocument.Parse(json);

            var root = document.RootElement;

            var answer = root
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()
                ?? "Không có phản hồi.";

            var promptTokens = 0;
            var completionTokens = 0;

            if (root.TryGetProperty(
                    "usage",
                    out var usage))
            {
                if (usage.TryGetProperty(
                        "prompt_tokens",
                        out var prompt))
                {
                    promptTokens = prompt.GetInt32();
                }

                if (usage.TryGetProperty(
                        "completion_tokens",
                        out var completion))
                {
                    completionTokens =
                        completion.GetInt32();
                }
            }

            return new ChatProviderResponse
            {
                Answer = answer.Trim(),
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens
            };
        }

        // =====================================================
        // GEMINI
        // =====================================================

        private async Task<ChatProviderResponse> CallGeminiAsync(
            string model,
            ChatModelBenchmarkRequestDto request,
            string context)
        {
            var apiKey = _configuration["Gemini:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception(
                    "Thiếu Gemini:ApiKey.");
            }

            var url =
                "https://generativelanguage.googleapis.com/" +
                $"v1beta/models/{model}:generateContent" +
                $"?key={apiKey.Trim()}";

            var fullPrompt =
                $"{CreateSystemPrompt()}\n\n" +
                CreateUserPrompt(request.Question, context);

            var payload = new
            {
                contents = new[]
                {
                new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = fullPrompt
                        }
                    }
                }
            },
                generationConfig = new
                {
                    temperature = request.Temperature,
                    maxOutputTokens =
                        request.MaxOutputTokens
                }
            };

            var client = _httpFactory.CreateClient();

            using var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await client.PostAsync(
                url,
                content);

            var json =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Gemini API lỗi: {json}");
            }

            using var document = JsonDocument.Parse(json);

            var root = document.RootElement;

            if (!root.TryGetProperty(
                    "candidates",
                    out var candidates)
                || candidates.GetArrayLength() == 0)
            {
                throw new Exception(
                    "Gemini không trả về câu trả lời.");
            }

            var answerBuilder = new StringBuilder();

            var parts = candidates[0]
                .GetProperty("content")
                .GetProperty("parts");

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty(
                        "text",
                        out var text))
                {
                    answerBuilder.Append(text.GetString());
                }
            }

            var promptTokens = 0;
            var completionTokens = 0;

            if (root.TryGetProperty(
                    "usageMetadata",
                    out var usage))
            {
                if (usage.TryGetProperty(
                        "promptTokenCount",
                        out var prompt))
                {
                    promptTokens = prompt.GetInt32();
                }

                if (usage.TryGetProperty(
                        "candidatesTokenCount",
                        out var completion))
                {
                    completionTokens =
                        completion.GetInt32();
                }
            }

            return new ChatProviderResponse
            {
                Answer = answerBuilder.ToString().Trim(),
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens
            };
        }

        // =====================================================
        // GROQ / OPENROUTER (OPENAI-COMPATIBLE APIS)
        // =====================================================

        private Task<ChatProviderResponse> CallGroqAsync(
            string model,
            ChatModelBenchmarkRequestDto request,
            string context)
        {
            return CallOpenAiCompatibleChatAsync(
                "https://api.groq.com/openai/v1/chat/completions",
                "Groq",
                _configuration["Groq:ApiKey"],
                model,
                request,
                context,
                addOpenRouterHeaders: false);
        }

        private Task<ChatProviderResponse> CallOpenRouterAsync(
            string model,
            ChatModelBenchmarkRequestDto request,
            string context)
        {
            return CallOpenAiCompatibleChatAsync(
                "https://openrouter.ai/api/v1/chat/completions",
                "OpenRouter",
                _configuration["OpenRouter:ApiKey"],
                model,
                request,
                context,
                addOpenRouterHeaders: true);
        }

        private async Task<ChatProviderResponse> CallOpenAiCompatibleChatAsync(
            string endpoint,
            string providerName,
            string? apiKey,
            string model,
            ChatModelBenchmarkRequestDto request,
            string context,
            bool addOpenRouterHeaders)
        {

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception(
                    $"Thiếu {providerName}:ApiKey.");
            }

            var client = _httpFactory.CreateClient();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    apiKey);

            if (addOpenRouterHeaders)
            {
                var siteUrl = _configuration["OpenRouter:SiteUrl"];
                var appName = _configuration["OpenRouter:AppName"];

                if (!string.IsNullOrWhiteSpace(siteUrl))
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation(
                        "HTTP-Referer",
                        siteUrl);
                }

                if (!string.IsNullOrWhiteSpace(appName))
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation(
                        "X-Title",
                        appName);
                }
            }

            var payload = new
            {
                model,
                messages = new object[]
                {
                new
                {
                    role = "system",
                    content = CreateSystemPrompt()
                },
                new
                {
                    role = "user",
                    content = CreateUserPrompt(
    request.Question,
    context)
                }
                },
                temperature = request.Temperature,
                max_tokens = request.MaxOutputTokens
            };

            using var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await client.PostAsync(
                endpoint,
                content);

            var json =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"{providerName} API lỗi: {json}");
            }

            using var document = JsonDocument.Parse(json);

            var root = document.RootElement;

            var answer = root
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()
                ?? "Không có phản hồi.";

            var promptTokens = 0;
            var completionTokens = 0;

            if (root.TryGetProperty(
                    "usage",
                    out var usage))
            {
                if (usage.TryGetProperty(
                        "prompt_tokens",
                        out var prompt))
                {
                    promptTokens = prompt.GetInt32();
                }

                if (usage.TryGetProperty(
                        "completion_tokens",
                        out var completion))
                {
                    completionTokens =
                        completion.GetInt32();
                }
            }

            return new ChatProviderResponse
            {
                Answer = answer.Trim(),
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens
            };
        }

        // =====================================================
        // COMMON HELPERS
        // =====================================================

        private static string CreateSystemPrompt()
        {
            return """
            Bạn là trợ lý học tập SmartEdu AI.

            Chỉ trả lời dựa trên ngữ cảnh được cung cấp.
            Trả lời bằng tiếng Việt rõ ràng và chính xác.
            Không tự tạo thông tin không có trong ngữ cảnh.

            Nếu ngữ cảnh không có câu trả lời, hãy nói:
            "Tôi không tìm thấy thông tin trong tài liệu."
            """;
        }

        private static string CreateUserPrompt(
      string question,
      string context)
        {
            return $"""
        DỮ LIỆU ĐƯỢC TRUY XUẤT TỪ HỆ THỐNG:

        {context}

        CÂU HỎI:

        {question}

        Hãy trả lời chỉ dựa trên dữ liệu được cung cấp.
        """;
        }

        private static double CalculateKeywordCoverage(
            string answer,
            List<string> expectedKeywords)
        {
            if (expectedKeywords == null
                || expectedKeywords.Count == 0)
            {
                return 0;
            }

            var validKeywords = expectedKeywords
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (validKeywords.Count == 0)
            {
                return 0;
            }

            var matchedCount = validKeywords.Count(
                keyword => answer.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase));

            return Math.Round(
                (double)matchedCount
                / validKeywords.Count
                * 100,
                2);
        }

        private static double CalculateGroundedness(
            string answer,
            string context)
        {
            var answerTerms = Tokenize(answer).Distinct().ToList();
            var contextTerms = Tokenize(context).ToHashSet();

            if (answerTerms.Count == 0)
            {
                return 0;
            }

            var supportedTerms = answerTerms.Count(contextTerms.Contains);
            return Math.Round(
                (double)supportedTerms / answerTerms.Count * 100,
                2);
        }

        private static double CalculateRelevance(
            string answer,
            string question)
        {
            var questionTerms = Tokenize(question).Distinct().ToList();
            var answerTerms = Tokenize(answer).ToHashSet();

            if (questionTerms.Count == 0)
            {
                return 0;
            }

            var matchedTerms = questionTerms.Count(answerTerms.Contains);
            return Math.Round(
                (double)matchedTerms / questionTerms.Count * 100,
                2);
        }

        private static List<string> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<string>();
            }

            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "và", "là", "của", "có", "được", "cho", "trong", "một",
                "những", "các", "với", "để", "từ", "theo", "này", "đó",
                "thì", "khi", "về", "như", "the", "and", "is", "are",
                "of", "to", "in", "a", "an", "for", "on", "with"
            };

            return Regex.Matches(
                    text.ToLowerInvariant(),
                    @"[\p{L}\p{N}]+")
                .Select(match => match.Value)
                .Where(term => term.Length > 1 && !stopWords.Contains(term))
                .ToList();
        }

        private static void CalculateOverallScores(
            IEnumerable<ChatModelBenchmarkResultDto> results)
        {
            var successfulResults = results
                .Where(result => string.IsNullOrWhiteSpace(result.Error))
                .ToList();
            var fastestRate = successfulResults.Count == 0
                ? 0
                : successfulResults.Max(result => result.TokensPerSecond);

            foreach (var result in successfulResults)
            {
                var speedScore = fastestRate > 0
                    ? result.TokensPerSecond / fastestRate * 100
                    : 0;

                result.OverallScore = Math.Round(
                    result.GroundednessPercent * 0.30
                    + result.RelevancePercent * 0.25
                    + result.CompletenessPercent * 0.25
                    + (100 - result.HallucinationPercent) * 0.10
                    + speedScore * 0.10,
                    2);
            }
        }

        private static string CreatePreview(
            string content,
            int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            var normalized = content
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();

            return normalized.Length <= maximumLength
                ? normalized
                : normalized[..maximumLength] + "...";
        }

        private static void ValidateEmbeddingRequest(
            EmbeddingBenchmarkRequestDto request)
        {
            if (request.SubjectId <= 0)
            {
                throw new ArgumentException(
                    "SubjectId phải lớn hơn 0.");
            }

            if (string.IsNullOrWhiteSpace(request.Query))
            {
                throw new ArgumentException(
                    "Query không được để trống.");
            }

            if (request.Models == null
                || request.Models.Count == 0)
            {
                throw new ArgumentException(
                    "Phải có ít nhất một embedding model.");
            }

            request.TopK = Math.Clamp(
                request.TopK,
                1,
                20);

            request.CandidateLimit = Math.Clamp(
                request.CandidateLimit,
                request.TopK,
                100);
        }

        private static void ValidateChatRequest(
            ChatModelBenchmarkRequestDto request)
        {
            if (request.SubjectId <= 0)
            {
                throw new ArgumentException(
                    "Vui lòng chọn môn học.");
            }

            if (string.IsNullOrWhiteSpace(request.Question))
            {
                throw new ArgumentException(
                    "Question không được để trống.");
            }

            if (request.Targets == null
                || request.Targets.Count == 0)
            {
                throw new ArgumentException(
                    "Phải có ít nhất một chat model.");
            }

            request.TopK = Math.Clamp(
                request.TopK,
                1,
                10);

            request.MaxOutputTokens = Math.Clamp(
                request.MaxOutputTokens,
                100,
                8192);
        }

        private async Task<RetrievedContextResult>
    RetrieveContextAsync(
        int subjectId,
        string question,
        string embeddingModel,
        int topK)
        {
            var chunks = await _chunkRepo.GetAllWithIncludeAsync(
                c => c.EmbeddingSet != null
                     && c.EmbeddingSet.Status == EmbeddingSetStatus.Ready
                     && c.EmbeddingSet.Documents.Any(
                         d => d.SubjectId == subjectId
                              && !d.IsDeleted),
                c => c.EmbeddingSet,
                c => c.EmbeddingSet.Documents
            );

            var availableChunks = chunks
                .Where(c => !string.IsNullOrWhiteSpace(c.Content))
                .ToList();

            if (availableChunks.Count == 0)
            {
                throw new Exception(
                    "Môn học này chưa có tài liệu hoặc chưa được tạo chunk.");
            }

            var queryVector =
                await GetHuggingFaceEmbeddingAsync(
                    embeddingModel,
                    question,
                    isQuery: true);

            var scoredChunks = new List<BenchmarkChunkDto>();

            foreach (var chunk in availableChunks)
            {
                var chunkVector =
                    await GetHuggingFaceEmbeddingAsync(
                        embeddingModel,
                        chunk.Content,
                        isQuery: false);

                var score = CosineSimilarity(
                    queryVector,
                    chunkVector);

                scoredChunks.Add(new BenchmarkChunkDto
                {
                    ChunkId = chunk.Id,
                    ChunkIndex = chunk.ChunkIndex,

                    DocumentTitle =
                        chunk.EmbeddingSet?.CanonicalTitle
                        ?? "Không xác định",

                    ContentPreview = chunk.Content,

                    Score = Math.Round(score, 6)
                });
            }

            var topChunks = scoredChunks
                .OrderByDescending(c => c.Score)
                .Take(topK)
                .ToList();

            var contextBuilder = new StringBuilder();

            for (var index = 0; index < topChunks.Count; index++)
            {
                var chunk = topChunks[index];

                contextBuilder.AppendLine(
                    $"[Tài liệu {index + 1}: {chunk.DocumentTitle}]");

                contextBuilder.AppendLine(
                    chunk.ContentPreview);

                contextBuilder.AppendLine();
            }

            return new RetrievedContextResult
            {
                Context = contextBuilder.ToString().Trim(),
                Chunks = topChunks
            };
        }
        private sealed class RetrievedContextResult
        {
            public string Context { get; set; } = string.Empty;

            public List<BenchmarkChunkDto> Chunks { get; set; } = new();
        }
        private sealed class ChatProviderResponse
        {
            public string Answer { get; set; }
                = string.Empty;

            public int PromptTokens { get; set; }

            public int CompletionTokens { get; set; }
        }
    }
}
