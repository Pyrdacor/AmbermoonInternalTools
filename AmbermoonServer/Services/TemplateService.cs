using RazorLight;

namespace AmbermoonServer.Services;

public class TemplateService
{
    private readonly RazorLightEngine razorLightEngine;    

    public TemplateService()
    {
        this.razorLightEngine = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(typeof(Program))
            .UseMemoryCachingProvider()
            .EnableDebugMode()
            .Build();
    }

    public async Task<string> RenderTemplateAsync<T>(string templateKey, T model)
    {
        return await this.razorLightEngine.CompileRenderAsync(templateKey, model);
    }

    public async Task<string> RenderTemplateAsync(string templateKey)
    {
        return await this.razorLightEngine.CompileRenderAsync(templateKey, new EmptyModel());
    }

    private class EmptyModel { }
}
