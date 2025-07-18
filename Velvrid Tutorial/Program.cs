﻿using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;

namespace Velvrid_Tutorial;

class Program
{
    private static GraphicsDevice _graphicsDevice;
    private static CommandList _commandList;
    private static DeviceBuffer _vertexBuffer;
    private static DeviceBuffer _indexBuffer;
    private static Shader[] _shaders;
    private static Pipeline _pipeline;

    private const string VertexCode = @"
#version 450

layout(location = 0) in vec2 Position;
layout(location = 1) in vec4 Color;

layout(location = 0) out vec4 fsin_Color;

void main()
{
    gl_Position = vec4(Position, 0, 1);
    fsin_Color = Color;
}";

    private const string FragmentCode = @"
#version 450

layout(location = 0) in vec4 fsin_Color;
layout(location = 0) out vec4 fsout_Color;

void main()
{
    fsout_Color = fsin_Color;
}";

    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        var window = CreateWindow();
        GraphicsDeviceOptions options = new GraphicsDeviceOptions
        {
            PreferStandardClipSpaceYDirection = true,
            PreferDepthRangeZeroToOne = true
        };
        _graphicsDevice = VeldridStartup.CreateGraphicsDevice(window, options);
        CreateResources();

        while (window.Exists)
        {
            window.PumpEvents();
            Draw();
        }
        
        DisposeResources();
        
    }
    
    private static void DisposeResources()
    {
        _pipeline.Dispose();
        _commandList.Dispose();
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _graphicsDevice.Dispose();
    }

    static void Draw()
    {
        _commandList.Begin();
        _commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
        _commandList.ClearColorTarget(0, RgbaFloat.Black);
        _commandList.SetVertexBuffer(0, _vertexBuffer);
        _commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
        _commandList.SetPipeline(_pipeline);
        _commandList.DrawIndexed(
            indexCount: 4,
            instanceCount: 1,
            indexStart: 0,
            vertexOffset: 0,
            instanceStart: 0);
        
        _commandList.End();
        _graphicsDevice.SubmitCommands(_commandList);
        _graphicsDevice.SwapBuffers();
    }

    static void CreateResources()
    {
        ResourceFactory factory = _graphicsDevice.ResourceFactory;
        var quadVertices = new[]
        { new VertexPositionColor(new Vector2(-0.75f, 0.75f), RgbaFloat.Red),
        new VertexPositionColor(new Vector2(0.75f, 0.75f), RgbaFloat.Green),
        new VertexPositionColor(new Vector2(-0.75f, -0.75f), RgbaFloat.Blue),
        new VertexPositionColor(new Vector2(0.75f, -0.75f), RgbaFloat.Yellow) };

        ushort[] quadIndices = { 0, 1, 2, 3 };
        _vertexBuffer = factory.CreateBuffer(new BufferDescription(4* VertexPositionColor.SizeInBytes, BufferUsage.VertexBuffer));
        _indexBuffer = factory.CreateBuffer(new BufferDescription(4 * sizeof(ushort), BufferUsage.IndexBuffer));
        
        _graphicsDevice.UpdateBuffer(_vertexBuffer, 0, quadVertices);
        _graphicsDevice.UpdateBuffer(_indexBuffer, 0, quadIndices);

        VertexLayoutDescription vertexlayout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float2),
            new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float4));
        
        ShaderDescription vertexShaderDesc = new ShaderDescription(
            ShaderStages.Vertex,
            Encoding.UTF8.GetBytes(VertexCode),
            "main");
        ShaderDescription fragmentShaderDesc = new ShaderDescription(
            ShaderStages.Fragment,
            Encoding.UTF8.GetBytes(FragmentCode),
            "main");

        _shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
        
        GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
        pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;
        pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
            depthTestEnabled: true,
            depthWriteEnabled: true,
            comparisonKind: ComparisonKind.LessEqual);
        pipelineDescription.RasterizerState = new RasterizerStateDescription(
            cullMode: FaceCullMode.Back,
            fillMode: PolygonFillMode.Solid,
            frontFace: FrontFace.Clockwise,
            depthClipEnabled: true,
            scissorTestEnabled: false);
        pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
        pipelineDescription.ResourceLayouts = System.Array.Empty<ResourceLayout>();
        pipelineDescription.ShaderSet = new ShaderSetDescription(
            vertexLayouts: new VertexLayoutDescription[] { vertexlayout },
            shaders: _shaders);
        pipelineDescription.Outputs = _graphicsDevice.SwapchainFramebuffer.OutputDescription;
        _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
        _commandList = factory.CreateCommandList();

    }

    static Sdl2Window CreateWindow()
    {
        var wCi = new WindowCreateInfo()
        {
            X = 100,
            Y = 100,
            WindowWidth = 800,
            WindowHeight = 600,
            WindowTitle = "Velvrid Tutorial",
        };
        
        return VeldridStartup.CreateWindow(ref wCi);
    }

    struct VertexPositionColor
    {
        public Vector2 Position;
        public RgbaFloat Color;

        public VertexPositionColor(Vector2 position, RgbaFloat color)
        {
            Position = position;
            Color = color;
        }

        public const uint SizeInBytes = 24;
    }
}