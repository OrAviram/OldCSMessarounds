using System;
using System.Runtime.InteropServices;
using SharpVulkan;

namespace LearningCSharp
{
    public unsafe class GraphicsPipeline : IDisposable
    {
        public Pipeline NativePipeline { get; private set; }
        public PipelineLayout Layout { get; private set; }
        public VulkanRenderPass RenderPass { get; private set; }
        private Device nativeDevice;

        public void Construct(LogicalDevice device, Shader[] shaders, VulkanSurface surface)
        {
            if (shaders == null)
                shaders = new Shader[0];

            nativeDevice = device.NativeDevice;
            Viewport viewport = new Viewport
            {
                X = 0,
                Y = 0,
                Height = surface.ImageExtents.Height,
                MaxDepth = 1,
                MinDepth = 0,
                Width = surface.ImageExtents.Width,
            };
            Rect2D scissor = new Rect2D { Offset = new Offset2D(0, 0), Extent = surface.ImageExtents };

            PipelineColorBlendAttachmentState colorBlendAttachment = new PipelineColorBlendAttachmentState
            {
                ColorWriteMask = ColorComponentFlags.R | ColorComponentFlags.G | ColorComponentFlags.B | ColorComponentFlags.A,
                BlendEnable = false,
                SourceColorBlendFactor = BlendFactor.One,
                DestinationColorBlendFactor = BlendFactor.Zero,
                ColorBlendOperation = BlendOperation.Add,
                SourceAlphaBlendFactor = BlendFactor.One,
                DestinationAlphaBlendFactor = BlendFactor.Zero,
                AlphaBlendOperation = BlendOperation.Add,
            };

            PipelineShaderStageCreateInfo* shaderStagesCreateInfos = stackalloc PipelineShaderStageCreateInfo[shaders.Length];
            SetShaderStageCreateInfos(shaderStagesCreateInfos, shaders);


            VertexInputBindingDescription bindingDescription = Vertex.BindingDescription;
            PipelineVertexInputStateCreateInfo vertexInputStateCreateInfo = CreateVertexInputCreateInfo(Vertex.AttributeDescriptions, ref bindingDescription);

            PipelineViewportStateCreateInfo viewportStateCreateInfo = CreateViewportStateCreateInfo(ref viewport, ref scissor);
            PipelineInputAssemblyStateCreateInfo inputAssemblyCreateInfo = CreateInputAssemblyCreateInfo();
            PipelineRasterizationStateCreateInfo rasterizerCreateInfo = CreateResterizationCreateInfo();
            PipelineMultisampleStateCreateInfo multisamplingCreateInfo = CreateMultisamplingCreateInfo();
            PipelineColorBlendStateCreateInfo colorBlendStateCreateInfo = CreateColorBlendCreateInfo(ref colorBlendAttachment);
            CreateLayout();

            RenderPass = new VulkanRenderPass(device, surface);

            GraphicsPipelineCreateInfo createInfo = new GraphicsPipelineCreateInfo
            {
                StructureType = StructureType.GraphicsPipelineCreateInfo,
                StageCount = (uint)shaders.Length,
                Stages = (IntPtr)shaderStagesCreateInfos,
                VertexInputState = new IntPtr(&vertexInputStateCreateInfo),
                InputAssemblyState = new IntPtr(&inputAssemblyCreateInfo),
                ViewportState = new IntPtr(&viewportStateCreateInfo),
                RasterizationState = new IntPtr(&rasterizerCreateInfo),
                MultisampleState = new IntPtr(&multisamplingCreateInfo),
                DepthStencilState = IntPtr.Zero,
                ColorBlendState = new IntPtr(&colorBlendStateCreateInfo),
                DynamicState = IntPtr.Zero,
                Layout = Layout,
                RenderPass = RenderPass.NativeRenderPass,
                Subpass = 0,
                BasePipelineHandle = Pipeline.Null,
                BasePipelineIndex = -1,
            };
            NativePipeline = nativeDevice.CreateGraphicsPipelines(PipelineCache.Null, 1, &createInfo);

            for (int i = 0; i < shaders.Length; i++)
                Marshal.FreeHGlobal(shaderStagesCreateInfos[i].Name);
        }

        public GraphicsPipeline(LogicalDevice device, Shader[] shaders, VulkanSurface surface)
        {
            Construct(device, shaders, surface);
        }

        PipelineViewportStateCreateInfo CreateViewportStateCreateInfo(ref Viewport viewport, ref Rect2D scissor)
        {
            fixed (void* viewportPtr = &viewport)
            fixed (void* scissorPtr = &scissor)
            {
                return new PipelineViewportStateCreateInfo
                {
                    StructureType = StructureType.PipelineViewportStateCreateInfo,
                    ViewportCount = 1,
                    Viewports = (IntPtr)viewportPtr,
                    ScissorCount = 1,
                    Scissors = (IntPtr)scissorPtr,
                };
            }
        }

        PipelineVertexInputStateCreateInfo CreateVertexInputCreateInfo(VertexInputAttributeDescription[] attributeDescriptions, ref VertexInputBindingDescription bindingDescription)
        {
            fixed (void* bindingDescriptionPtr = &bindingDescription)
            fixed (void* attributeDescriptionsPtr = &attributeDescriptions[0])
            {
                return new PipelineVertexInputStateCreateInfo
                {
                    StructureType = StructureType.PipelineVertexInputStateCreateInfo,
                    VertexBindingDescriptionCount = 1,
                    VertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
                    VertexBindingDescriptions = (IntPtr)bindingDescriptionPtr,
                    VertexAttributeDescriptions = (IntPtr)attributeDescriptionsPtr,
                };
            }
        }

        PipelineInputAssemblyStateCreateInfo CreateInputAssemblyCreateInfo()
        {
            return new PipelineInputAssemblyStateCreateInfo
            {
                StructureType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = PrimitiveTopology.TriangleList,
                PrimitiveRestartEnable = false,
            };
        }

        PipelineRasterizationStateCreateInfo CreateResterizationCreateInfo()
        {
            return new PipelineRasterizationStateCreateInfo
            {
                StructureType = StructureType.PipelineRasterizationStateCreateInfo,
                CullMode = CullModeFlags.Back,
                DepthBiasClamp = 0,
                DepthBiasConstantFactor = 0,
                DepthBiasEnable = false,
                DepthBiasSlopeFactor = 0,
                DepthClampEnable = false,
                FrontFace = FrontFace.Clockwise,
                LineWidth = 1,
                PolygonMode = PolygonMode.Fill,
                RasterizerDiscardEnable = false,
            };
        }

        PipelineMultisampleStateCreateInfo CreateMultisamplingCreateInfo()
        {
            return new PipelineMultisampleStateCreateInfo
            {
                StructureType = StructureType.PipelineMultisampleStateCreateInfo,
                AlphaToCoverageEnable = false,
                AlphaToOneEnable = false,
                MinSampleShading = 1,
                RasterizationSamples = SampleCountFlags.Sample1,
                SampleMask = IntPtr.Zero,
                SampleShadingEnable = false,
            };
        }

        PipelineColorBlendStateCreateInfo CreateColorBlendCreateInfo(ref PipelineColorBlendAttachmentState colorBlendAttachment)
        {
            fixed (void* colorBlendAttachmentPtr = &colorBlendAttachment)
            {
                return new PipelineColorBlendStateCreateInfo
                {
                    StructureType = StructureType.PipelineColorBlendStateCreateInfo,
                    LogicOperationEnable = false,
                    LogicOperation = LogicOperation.Copy,
                    AttachmentCount = 1,
                    Attachments = (IntPtr)colorBlendAttachmentPtr,
                    BlendConstants = new PipelineColorBlendStateCreateInfo.BlendConstantsArray { Value0 = 0, Value1 = 0, Value2 = 0, Value3 = 0 },
                };
            }
        }

        void CreateLayout()
        {
            PipelineLayoutCreateInfo createInfo = new PipelineLayoutCreateInfo
            {
                StructureType = StructureType.PipelineLayoutCreateInfo,
                PushConstantRangeCount = 0,
                PushConstantRanges = IntPtr.Zero,
                SetLayoutCount = 0,
                SetLayouts = IntPtr.Zero,
            };
            Layout = nativeDevice.CreatePipelineLayout(ref createInfo);
        }

        void SetShaderStageCreateInfos(PipelineShaderStageCreateInfo* createInfos, Shader[] shaders)
        {
            for (int i = 0; i < shaders.Length; i++)
            {
                createInfos[i] = new PipelineShaderStageCreateInfo
                {
                    StructureType = StructureType.PipelineShaderStageCreateInfo,
                    Module = shaders[i].NativeShaderModule,
                    Name = Marshal.StringToHGlobalAnsi("main"),
                    SpecializationInfo = IntPtr.Zero,
                    Stage = shaders[i].PipelineShaderStage,
                };
            }
        }

        void IDisposable.Dispose()
        {
            RenderPass.Dispose();
            nativeDevice.DestroyPipelineLayout(Layout);
            nativeDevice.DestroyPipeline(NativePipeline);
        }

        public void Dispose(bool supressFinalize = true)
        {
            (this as IDisposable).Dispose();
            if (supressFinalize)
                GC.SuppressFinalize(this);
        }

        ~GraphicsPipeline()
        {
            Dispose(false);
        }
    }
}