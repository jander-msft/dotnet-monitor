// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Tools.Monitor.LibrarySharing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class DefaultSharedLibraryInitializerTests : IDisposable
    {
        private const string ManagedTargetFramework = "net6.22";

        private readonly TestOutputLogger<DefaultSharedLibraryInitializer> _logger;
        private readonly TemporaryDirectory _sourceDir;
        private readonly TemporaryDirectory _targetDir;

        public DefaultSharedLibraryInitializerTests(ITestOutputHelper outputHelper)
        {
            _logger = new TestOutputLogger<DefaultSharedLibraryInitializer>(outputHelper);
            _sourceDir = new TemporaryDirectory(outputHelper);
            _targetDir = new TemporaryDirectory(outputHelper);
        }

        public void Dispose()
        {
            _targetDir.Dispose();
            _sourceDir.Dispose();
        }

        [Fact]
        public void DefaultSharedLibraryInitializer_SourceDoesNotExist_ThrowsException()
        {
            DefaultSharedLibraryPathProvider provider = new(
                Path.Combine(_sourceDir.FullName, "doesNotExist"),
                _targetDir.FullName);

            using DefaultSharedLibraryInitializer initializer = new(provider, _logger);

            Assert.Throws<DirectoryNotFoundException>(initializer.Initialize);
        }

        [Fact]
        public void DefaultSharedLibraryInitializer_WithoutTarget_ReturnsSourcePath()
        {
            // Arrange
            string SourceFileName = "source.txt";
            string ExpectedFilePath = CreateSourceManagedFile(SourceFileName);

            DefaultSharedLibraryPathProvider provider = new(_sourceDir.FullName, null);

            // Act
            using DefaultSharedLibraryInitializer initializer = new(provider, _logger);

            IFileProviderFactory factory = initializer.Initialize();

            // Assert
            Assert.NotNull(factory);

            IFileProvider managedProvider = factory.CreateManaged(ManagedTargetFramework);
            Assert.NotNull(managedProvider);

            IFileInfo managedDirInfo = managedProvider.GetFileInfo(SourceFileName);
            Assert.NotNull(managedDirInfo);

            Assert.Equal(ExpectedFilePath, managedDirInfo.PhysicalPath);
        }

        [Fact]
        public void DefaultSharedLibraryInitializer_WithTarget_SourceCopiedToTarget()
        {
            // Arrange
            string SourceFileName = "source.txt";
            string SourceFilePath = CreateSourceManagedFile(SourceFileName);
            string TargetFilePath = CreateTargetManagedFilePath(SourceFileName);

            DefaultSharedLibraryPathProvider provider = new(_sourceDir.FullName, _targetDir.FullName);

            // Act
            using DefaultSharedLibraryInitializer initializer = new(provider, _logger);

            IFileProviderFactory factory = initializer.Initialize();

            // Assert
            Assert.NotNull(factory);

            IFileProvider managedProvider = factory.CreateManaged(ManagedTargetFramework);
            Assert.NotNull(managedProvider);

            IFileInfo managedDirInfo = managedProvider.GetFileInfo(SourceFileName);
            Assert.NotNull(managedDirInfo);

            Assert.Equal(TargetFilePath, managedDirInfo.PhysicalPath);
            Assert.True(File.Exists(TargetFilePath));

            using FileStream targetStream = new(TargetFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader targetReader = new(targetStream);

            Assert.Equal(File.ReadAllText(SourceFilePath), targetReader.ReadToEnd());
        }

        [Fact]
        public void DefaultSharedLibraryInitializer_WithTarget_OnlyAllowTargetRead()
        {
            // Arrange
            string SourceFileName = "source.txt";
            CreateSourceManagedFile(SourceFileName);

            string ExpectedFilePath = CreateTargetManagedFilePath(SourceFileName);

            DefaultSharedLibraryPathProvider provider = new(_sourceDir.FullName, _targetDir.FullName);

            // Act
            using DefaultSharedLibraryInitializer initializer = new(provider, _logger);

            initializer.Initialize();

            // Assert

        }

        private string CreateSourceManagedFile(string fileName)
        {
            string path = Path.Combine(_sourceDir.FullName, "any", ManagedTargetFramework, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, fileName);
            return path;
        }

        private string CreateTargetManagedFilePath(string fileName)
        {
            return Path.Combine(_targetDir.FullName, "any", ManagedTargetFramework, fileName);
        }
    }
}
