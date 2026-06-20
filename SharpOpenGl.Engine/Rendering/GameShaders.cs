namespace SharpOpenGl.Engine.Rendering;

/// <summary>
/// GLSL sources shared by desktop (330 core) and browser (300 es) renderers.
/// Same uniforms: projection, view, model, overrideColor; pos+color vertex layout.
/// </summary>
public static class GameShaders
{
    public const string DesktopVertex = @"
#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aColor;

uniform mat4 projection;
uniform mat4 view;
uniform mat4 model;
uniform vec4 overrideColor;

out vec3 fragColor;

void main()
{
    gl_Position = projection * view * model * vec4(aPosition, 1.0);
    gl_PointSize = 2.0;
    if (overrideColor.a > 0.0)
        fragColor = overrideColor.rgb;
    else
        fragColor = aColor;
}";

    public const string DesktopFragment = @"
#version 330 core
in vec3 fragColor;
out vec4 outputColor;

void main()
{
    outputColor = vec4(fragColor, 1.0);
}";

    public const string WebVertex = @"
#version 300 es
precision mediump float;
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aColor;

uniform mat4 projection;
uniform mat4 view;
uniform mat4 model;
uniform vec4 overrideColor;

out vec3 fragColor;

void main()
{
    gl_Position = projection * view * model * vec4(aPosition, 1.0);
    gl_PointSize = 2.0;
    if (overrideColor.a > 0.0)
        fragColor = overrideColor.rgb;
    else
        fragColor = aColor;
}";

    public const string WebFragment = @"
#version 300 es
precision mediump float;
in vec3 fragColor;
out vec4 outputColor;

void main()
{
    outputColor = vec4(fragColor, 1.0);
}";
}