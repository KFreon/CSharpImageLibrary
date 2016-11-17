using Cudafy;
using Cudafy.Host;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsefulThings;

namespace CSharpImageLibrary
{
    /// <summary>
    /// Provides information about the GPU and hardware acceleration it could provide.
    /// </summary>
    public static class GPU
    {
        /// <summary>
        /// Indicates whether there is a GPU that can be used for hardware acceleration.
        /// </summary>
        public static bool IsGPUAvailable { get; private set; } = false;

        /// <summary>
        /// Number of threads per block.
        /// </summary>
        public static int MaxNumThreadsPerBlock { get; private set; } = -1;

        /// <summary>
        /// Type of GPU detected (nVidia CUDA, or anything else OpenCL)
        /// </summary>
        public static Cudafy.eGPUType GPUType { get; private set; } = eGPUType.Emulator;

        /// <summary>
        /// Number of processors in the GPU for processing.
        /// </summary>
        public static int NumProcessorsAvailable { get; private set; } = -1;

        static GPU()
        {
            IsGPUAvailable = false;

            // Determine GPU Type
            IEnumerable<GPGPUProperties> properties = null;

            var cudaProperties = CudafyHost.GetDeviceProperties(eGPUType.Cuda, true);
            bool isCuda = true;

            try
            {
                isCuda = cudaProperties.Any();
            }
            catch
            {
                isCuda = false;
            }

            if (isCuda)
            {
                GPUType = eGPUType.Cuda;
                properties = cudaProperties;
            }
            else
            {
                var openCLProperties = CudafyHost.GetDeviceProperties(eGPUType.OpenCL, true);
                if (openCLProperties.Any())
                {
                    GPUType = eGPUType.OpenCL;
                    properties = openCLProperties;
                }
            }

            if (GPUType != eGPUType.Emulator)
            {
                IsGPUAvailable = true;

                // Filter out CPU graphics.
                var devProp = properties.Where(p => !p.Name.Contains("CPU", StringComparison.OrdinalIgnoreCase)).First();  // TODO: Better GPU Filtering methods.
                NumProcessorsAvailable = devProp.MultiProcessorCount;
                MaxNumThreadsPerBlock = devProp.MaxThreadsPerBlock;
            }
        }
    }
}
