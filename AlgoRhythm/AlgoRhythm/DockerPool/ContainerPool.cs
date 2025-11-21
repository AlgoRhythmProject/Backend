using Docker.DotNet;
using Docker.DotNet.Models;
using System.Collections.Concurrent;

namespace AlgoRhythm.DockerPool
{
    public class ContainerPool
    {
        private readonly ConcurrentQueue<string> _availableContainers = new();
        private readonly SemaphoreSlim _semaphore;
        private readonly DockerClient _dockerClient;
        private readonly int _poolSize;
        private readonly string _imageName;

        public ContainerPool(DockerClient dockerClient, string imageName, int poolSize = 5)
        {
            _dockerClient = dockerClient;
            _imageName = imageName;
            _poolSize = poolSize;
            _semaphore = new SemaphoreSlim(poolSize, poolSize);
        }

        public async Task InitializeAsync()
        {
            var tasks = Enumerable.Range(0, _poolSize)
                .Select(async i => CreateAndAddContainerAsync());
            await Task.WhenAll(tasks);
        }

        private async Task<string> CreateContainerAsync()
        {
            var createParams = new CreateContainerParameters
            {
                Image = _imageName,
                HostConfig = new HostConfig
                {
                    Memory = 512 * 1024 * 1024,
                    NanoCPUs = 500_000_000,
                    NetworkMode = "none",
                    ReadonlyRootfs = false,
                    Tmpfs = new Dictionary<string, string>
                    {
                        { "/tmp", "rw,nosuid,size=100m" }
                    }
                },
                Cmd = ["sleep", "infinity"],
                WorkingDir = "/tmp",
                User = "nobody",
                Env =
                [
                    "DOTNET_CLI_HOME=/tmp",
                    "HOME=/tmp"
                ]
            };

            var response = await _dockerClient.Containers.CreateContainerAsync(createParams);
            await _dockerClient.Containers.StartContainerAsync(
                response.ID,
                new ContainerStartParameters()
            );

            return response.ID;
        }

        private async Task CreateAndAddContainerAsync()
        {
            var containerId = await CreateContainerAsync();
            _availableContainers.Enqueue(containerId);
        }

        public async Task<string> AcquireAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            if (_availableContainers.TryDequeue(out var containerId))
            {
                return containerId;
            }

            // Fallback: create new container if pool is empty
            return await CreateContainerAsync();
        }

        public async Task ReleaseAsync(string containerId)
        {
            try
            {
                // Clean up temporary files
                await CleanupContainerAsync(containerId);
                _availableContainers.Enqueue(containerId);
            }
            catch
            {
                // If cleanup fails, create a new container
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _dockerClient.Containers.RemoveContainerAsync(
                            containerId,
                            new ContainerRemoveParameters { Force = true }
                        );
                        await CreateAndAddContainerAsync();
                    }
                    catch { }
                });
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task CleanupContainerAsync(string containerId)
        {
            var execConfig = new ContainerExecCreateParameters
            {
                Cmd = ["sh", "-c", "rm -rf /tmp/code_*"],
                AttachStdout = false,
                AttachStderr = false
            };

            var execResponse = await _dockerClient.Exec
                .ExecCreateContainerAsync(containerId, execConfig);
            await _dockerClient.Exec.StartContainerExecAsync(execResponse.ID);
        }

        public async Task DisposeAsync()
        {
            while (_availableContainers.TryDequeue(out var containerId))
            {
                try
                {
                    await _dockerClient.Containers.RemoveContainerAsync(
                        containerId,
                        new ContainerRemoveParameters { Force = true }
                    );
                }
                catch { }
            }
        }

        public async Task<(string, string)> ExecuteInContainerAsync(string containerId, string base64Payload)
        {
            var exec = await _dockerClient.Exec.ExecCreateContainerAsync(containerId, new ContainerExecCreateParameters
            {
                AttachStdout = true,
                AttachStderr = true,
                Cmd = new[] { "dotnet", "CodeExecutor.dll", base64Payload }
            });

            var stream = await _dockerClient.Exec.StartAndAttachContainerExecAsync(exec.ID, false);
            return await stream.ReadOutputToEndAsync(CancellationToken.None);
        }

    }
}
