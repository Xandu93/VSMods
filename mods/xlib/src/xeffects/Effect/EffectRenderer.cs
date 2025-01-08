using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace XLib.XEffects
{
    /// <summary>
    /// The renderer for <seealso cref="ShaderEffect"/>s
    /// </summary>
    public class EffectRenderer : IRenderer
    {
        /// <summary>
        /// The client api
        /// </summary>
        ICoreClientAPI capi;

        /// <summary>
        /// The mesh reference
        /// </summary>
        MeshRef meshRef;

        /// <summary>
        /// Gets or sets the intensity.
        /// Is set by the effect and is the same as the effect intensity.
        /// </summary>
        /// <value>
        /// The intensity.
        /// </value>
        public float Intensity { get; set; }

        /// <summary>
        /// Gets the name of the shader.
        /// </summary>
        /// <value>
        /// The name of the shader.
        /// </value>
        public string ShaderName { get; private set; }

        /// <summary>
        /// Gets the shader.
        /// </summary>
        /// <value>
        /// The shader.
        /// </value>
        public IShaderProgram Shader { get; private set; }

        /// <summary>
        /// The render order.
        /// Will be rendered as an overlay after the world was rendered.
        /// </summary>
        public virtual double RenderOrder => 0.85;

        /// <summary>
        /// Within what range to the player OnRenderFrame() should be called (currently not used!)
        /// </summary>
        public virtual int RenderRange => 1;

        /// <summary>
        /// Gets or sets the time maximum.
        /// If time reaches this value time will be set to 0.
        /// </summary>
        /// <value>
        /// The time maximum.
        /// </value>
        public float TimeMax { get; set; }

        /// <summary>
        /// Gets or sets the time.
        /// This value accumulates the delta time and can be used 
        /// by the shader to create effects.
        /// </summary>
        /// <value>
        /// The time.
        /// </value>
        private float Time { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EffectRenderer"/> class.
        /// </summary>
        /// <param name="capi">The client api.</param>
        /// <param name="shaderName">Name of the shader.</param>
        public EffectRenderer(ICoreClientAPI capi, string shaderName)
        {
            this.capi = capi;
            this.Intensity = 0.0f;
            this.ShaderName = shaderName;
            this.Shader = null;
            this.TimeMax = 2.0f;
            this.Time = 0.0f;

            MeshData quadMesh = QuadMeshUtil.GetCustomQuadModelData(-1, -1, 0, 2, 2);
            quadMesh.Rgba = null;

            meshRef = capi.Render.UploadMesh(quadMesh);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            if (meshRef != null) capi.Render.DeleteMesh(meshRef);
            meshRef = null;
            Shader?.Dispose();
            Shader = null;
            capi.Event.ReloadShader -= LoadShader;
            capi.Event.UnregisterRenderer(this, EnumRenderStage.AfterFinalComposition);
        }

        /// <summary>
        /// Called every frame for rendering whatever you need to render.
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <param name="stage"></param>
        public virtual void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (meshRef == null || Shader == null) return;
            Time += deltaTime;
            if (Time > TimeMax) Time = 0.0f;

            IShaderProgram curShader = capi.Render.CurrentActiveShader;
            curShader?.Stop();

            if (Intensity != 0.0f)
            {
                Shader.Use();
                capi.Render.GlToggleBlend(true, EnumBlendMode.Overlay);
                capi.Render.GLDisableDepthTest();
                Shader.BindTexture2D("primaryScene", capi.Render.FrameBuffers[(int)EnumFrameBuffer.Primary].ColorTextureIds[0], 0);
                Shader.Uniform("intensity", Intensity);
                Shader.Uniform("flow", (float)Math.Sin(Time * Math.PI));
                Shader.Uniform("time", deltaTime);
                capi.Render.RenderMesh(meshRef);
                Shader.Stop();
            }
            curShader?.Use();
        }

        /// <summary>
        /// Loads and compiles shaders and registers this renderer.
        /// </summary>
        /// <returns></returns>
        public virtual bool Register()
        {
            if (!LoadShader())
            {
                return false;
            }
            capi.Event.ReloadShader += LoadShader;
            capi.Event.RegisterRenderer(this, EnumRenderStage.AfterFinalComposition);
            return true;
        }

        /// <summary>
        /// Loads the shader.
        /// You should use the register method instead.
        /// This method is primarily used for the shader reload.
        /// </summary>
        /// <returns></returns>
        public virtual bool LoadShader()
        {
            if (ShaderName == null) return false;
            Shader ??= capi.Shader.NewShaderProgram();
            if (Shader == null) return false;
            Shader.VertexShader ??= capi.Shader.NewShader(EnumShaderType.VertexShader);
            if (Shader.VertexShader == null) return false;
            Shader.FragmentShader ??= capi.Shader.NewShader(EnumShaderType.FragmentShader);
            if (Shader.FragmentShader == null) return false;

            string[] parts = ShaderName.Split(':');
            IAsset assetvs;
            IAsset assetfs;
            string file;

            if (parts.Length > 1){
                file = parts[0] + ":shaders/" + parts[1];
            }
            else{
                file = "game:shaders/" + parts[0];
            }

            assetvs = capi.Assets.TryGet(file + ".vsh");
            assetfs = capi.Assets.TryGet(file + ".fsh");
            if (assetvs == null || assetfs == null) return false;

            Shader.VertexShader.Code = assetvs.ToText();
            Shader.FragmentShader.Code = assetfs.ToText();

            if (capi.Shader.RegisterMemoryShaderProgram(ShaderName, Shader) < 0) return false;
            return Shader.Compile();
        }
    }
}

