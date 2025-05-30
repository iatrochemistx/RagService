using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;    
using RagService.Application.Interfaces;
using RagService.Infrastructure.VectorSearch;
using RagService.Domain.Models;

namespace RagService.Tests.VectorSearch
{
    public class VectorSearchServiceTests
    {
        [Fact]
        public async Task Constructor_LoadsAllDocuments()
        {
            // Arrange
            var mockEmbedder = new Mock<IEmbeddingService>();
            mockEmbedder
                .Setup(e => e.EmbedAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new float[384]);  // dummy vector

            // Prepare a temporary data folder under the test output
            var testOutput = Directory.GetCurrentDirectory();
            var dataPath = Path.Combine(testOutput, "data");
            if (Directory.Exists(dataPath))
                Directory.Delete(dataPath, true);
            Directory.CreateDirectory(dataPath);
            
            // Create dummy file
            var fileName = "test.txt";
            var filePath = Path.Combine(dataPath, fileName);
            File.WriteAllText(filePath, "hello");

            // Mock environment to point to test output folder
            var mockEnv = new Mock<IHostEnvironment>();
            mockEnv.Setup(e => e.ContentRootPath).Returns(testOutput);

            // Mock logger for VectorSearchService
            var mockLogger = new Mock<ILogger<VectorSearchService>>();

            // Act
            var service = new VectorSearchService(
                mockEmbedder.Object,
                mockEnv.Object,
                mockLogger.Object // <--- logger injected here
            );
            var results = await service.GetTopDocumentsAsync("anything");

            // Assert
            results.Should().ContainSingle(doc => doc.FileName == fileName);
        }
    }
}
