using CodeExecutor.Config;
using CodeExecutor.Interfaces;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;
using System.Text;
using CodeExecutor.DTO;
using CodeExecutor.DockerPool;

namespace CodeExecutor.Services
{
    public class CSharpExecutionService : ICodeExecutionService
    {
        public readonly string DockerImageName = "csharp-executor:latest";
        private readonly DockerClient _dockerClient;
        private readonly CSharpCodeExecutionConfig _codeExecutionConfig;
        private readonly ContainerPool _containerPool;

        public CSharpExecutionService(
            DockerClient dockerClient,
            IOptions<CSharpCodeExecutionConfig> codeExecutionConfig,
            ContainerPool containerPool) 
        {
            _dockerClient = dockerClient;
            _codeExecutionConfig = codeExecutionConfig.Value;
            _containerPool = containerPool;
        }

        public async Task<ExecutionResult> ExecuteCodeAsync(string code)
        {
            var containerId = await CreateContainerAsync();
            try
            {
                await StartContainerAsync(containerId);

                // 1. Create temp folder and project
                var projectPath = await CreateProjectAsync(containerId, code);

                // 2. Build the project
                var buildResult = await ExecuteInContainerAsync(containerId, $"dotnet build {projectPath} -c Release");
                if (!buildResult.Success)
                    return buildResult;

                // 3. Execute the compiled DLL
                var dllPath = $"{projectPath}/bin/Release/net8.0/TempApp.dll";
                var runResult = await ExecuteInContainerAsync(containerId, $"dotnet exec {dllPath}");

                return runResult;
            }
            finally
            {
                await CleanupContainerAsync(containerId);
            }
        }

        public async Task<ExecutionResult> ExecuteCodeOptimizedAsync(string code)
        {
            var containerId = await _containerPool.AcquireAsync();

            try
            {
                // Use unique folder per execution
                string tempFolder = $"/tmp/code_{Guid.NewGuid():N}";

                // Create folder and write code in ONE command
                string base64Code = Convert.ToBase64String(Encoding.UTF8.GetBytes(code));

                string setupCommand = string.Join(" && ", [
                    $"mkdir -p {tempFolder}", 
                    $"cd {tempFolder}",
                    "dotnet new console -n TempApp --output . --force --no-restore", 
                    $"echo '{base64Code}' | base64 -d > Program.cs",
                ]);

                ExecutionResult setupResult = await ExecuteInContainerAsync(containerId, setupCommand);
                
                if (!setupResult.Success)
                {
                    return new ExecutionResult
                    {
                        Success = false,
                        Error = "Failed to setup project",
                        Stderr = setupResult.Stderr
                    };
                }

                // Build and run in one command
                var runCommand = string.Join(" && ",
                [
                    $"cd {tempFolder}",
                    "dotnet build -c Release --nologo -v quiet",
                    "dotnet run -c Release --no-build --nologo"
                ]);

                var result = await ExecuteInContainerAsync(containerId, runCommand);
                return result;
            }
            finally
            {
                await _containerPool.ReleaseAsync(containerId);
            }
        }

        private async Task CleanupContainerAsync(string containerId)
        {
            await _dockerClient.Containers.StopContainerAsync(
                containerId,
                new ContainerStopParameters { WaitBeforeKillSeconds = 2 }
            );

            await _dockerClient.Containers.RemoveContainerAsync(
                containerId,
                new ContainerRemoveParameters { Force = true }
            );
        }
        private async Task<string> CreateContainerAsync()
        {
            var createParams = new CreateContainerParameters
            {
                Image = DockerImageName,
                HostConfig = new HostConfig
                {
                    Memory = 512 * 1024 * 1024, // 512MB
                    NanoCPUs = 500_000_000, // 0.5 CPU
                    NetworkMode = "none", // Disable network
                    ReadonlyRootfs = false, //
                    Tmpfs = new Dictionary<string, string>
                    {
                        { "/tmp", "rw,noexec,nosuid,size=65536k" }
                    }
                },
                Cmd = new[] { "sleep", "infinity" }, // Keep alive
                WorkingDir = "/tmp",
                User = "nobody" // Non-root user
            };

            createParams.Env = new List<string>
            {
                "DOTNET_CLI_HOME=/tmp",
                "HOME=/tmp"
            };
            var response = await _dockerClient.Containers.CreateContainerAsync(createParams);

            return response.ID;
        }

        private async Task StartContainerAsync(string containerId)
        {
            await _dockerClient.Containers.StartContainerAsync(
                containerId,
                new ContainerStartParameters()
            );
        }

        private async Task<string> CreateProjectAsync(string containerId, string code)
        {
            var tempFolder = $"/tmp/code_{Guid.NewGuid():N}";

            // 1. Create directory
            await ExecuteInContainerAsync(containerId, $"mkdir -p {tempFolder}");

            // 2. Initialize project
            await ExecuteInContainerAsync(
                containerId,
                $"dotnet new console -n TempApp --output {tempFolder}"
            );

            // 3. Write code directly using echo (escape special characters)
            var escapedCode = code
                .Replace("\\", "\\\\")
                .Replace("$", "\\$")
                .Replace("`", "\\`")
                .Replace("\"", "\\\"");

            var writeResult = await ExecuteInContainerAsync(
                containerId,
                $"echo \"{escapedCode}\" > {tempFolder}/Program.cs"
            );

            if (!writeResult.Success)
            {
                throw new Exception($"Failed to write Program.cs: {writeResult.Stderr}");
            }

            return tempFolder;
        }

        private Stream CreateTarArchive(string fileName, string content)
        {
            var memoryStream = new MemoryStream();
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var header = new byte[512];

            // File name
            var nameBytes = Encoding.ASCII.GetBytes(fileName);
            Array.Copy(nameBytes, header, Math.Min(nameBytes.Length, 100));

            // File mode
            var mode = "0000644";
            Array.Copy(Encoding.ASCII.GetBytes(mode), 0, header, 100, 7);

            // File size in octal
            var size = Convert.ToString(contentBytes.Length, 8).PadLeft(11, '0');
            Array.Copy(Encoding.ASCII.GetBytes(size), 0, header, 124, 11);

            // Checksum
            Array.Fill<byte>(header, (byte)' ', 148, 8);
            var checksum = header.Sum(b => (int)b);
            var checksumStr = Convert.ToString(checksum, 8).PadLeft(6, '0') + "\0 ";
            Array.Copy(Encoding.ASCII.GetBytes(checksumStr), 0, header, 148, 8);

            // Write header and content
            memoryStream.Write(header, 0, 512);
            memoryStream.Write(contentBytes, 0, contentBytes.Length);

            // Pad to 512 bytes
            var padding = 512 - (contentBytes.Length % 512);
            if (padding < 512)
                memoryStream.Write(new byte[padding], 0, padding);

            // Write two empty 512-byte blocks to finish TAR
            memoryStream.Write(new byte[512], 0, 512);
            memoryStream.Write(new byte[512], 0, 512);

            memoryStream.Position = 0;
            return memoryStream;
        }

        private async Task<ExecutionResult> ExecuteInContainerAsync(
            string containerId,
            string command)
        {
            var execConfig = new ContainerExecCreateParameters
            {
                AttachStdout = true,
                AttachStderr = true,
                Cmd = new[] { "sh", "-c", command }
            };

            var execResponse = await _dockerClient.Exec
                .ExecCreateContainerAsync(containerId, execConfig);

            using var stdoutStream = new MemoryStream();
            using var stderrStream = new MemoryStream();
            using var cts = new CancellationTokenSource(
                TimeSpan.FromSeconds(_codeExecutionConfig.Timeout)
            );

            try
            {
                var stream = await _dockerClient.Exec.StartAndAttachContainerExecAsync(
                    execResponse.ID,
                    false,
                    cts.Token
                );

                await stream.CopyOutputToAsync(
                    null,
                    stdoutStream,
                    stderrStream,
                    cts.Token
                );

                stdoutStream.Position = 0;
                stderrStream.Position = 0;

                var stdout = new StreamReader(stdoutStream).ReadToEnd();
                var stderr = new StreamReader(stderrStream).ReadToEnd();

                var inspectResponse = await _dockerClient.Exec
                    .InspectContainerExecAsync(execResponse.ID);

                return new ExecutionResult
                {
                    Stdout = stdout,
                    Stderr = stderr,
                    ExitCode = inspectResponse.ExitCode,
                    Success = inspectResponse.ExitCode == 0
                };
            }
            catch (OperationCanceledException)
            {
                return new ExecutionResult
                {
                    Error = "Time Limit Exceeded",
                    Success = false
                };
            }
        }
    }
}
