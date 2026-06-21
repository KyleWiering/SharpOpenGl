# WebGL2 Parity Plan

> **Note:** Browser build is Blazor WebAssembly (`SharpOpenGl.Browser/`), sharing `SharpOpenGl.Engine` with desktop.

## Overview

The SharpOpenGL desktop engine targets OpenGL 3.3 Core via OpenTK.
The WebGL2 browser target (`SharpOpenGl.Browser`) must provide equivalent functionality
using the WebGL2 API, which is a subset of OpenGL ES 3.0.

This document defines which engine features require special handling in the
WebGL2 port and outlines the approach for each.

---

## Feature Parity Matrix

| Feature | Desktop (OpenTK) | WebGL2 Status | Notes |
|---------|-----------------|---------------|-------|
| VAO / VBO | `GL.GenVertexArray` | ✅ Supported | `createVertexArray` (requires WebGL2) |
| Vertex shaders | GLSL 330 core | ⚠️ Adapt | Must use GLSL ES 300, `in`/`out` instead of `attribute`/`varying` |
| Fragment shaders | GLSL 330 core | ⚠️ Adapt | Add `precision mediump float;`, remove `layout(location)` on outputs |
| Uniform mat4 | `GL.UniformMatrix4` | ✅ Supported | `uniformMatrix4fv(loc, false, mat)` |
| DrawArrays | `GL.DrawArrays` | ✅ Supported | `gl.drawArrays` |
| Depth test | `GL.Enable(DepthTest)` | ✅ Supported | `gl.enable(gl.DEPTH_TEST)` |
| Point size | `gl_PointSize` in shader | ✅ Supported | WebGL2 supports `gl_PointSize` |
| Frame loop | `GameWindow.Run()` | ⚠️ Adapt | Use `requestAnimationFrame` |
| Screenshot | `GL.ReadPixels` | ✅ Supported | `gl.readPixels` into `Uint8Array` |
| Instanced draw | `GL.DrawArraysInstanced` | ✅ Supported | WebGL2 supports instancing |
| UBOs | `GL.BindBufferBase` | ✅ Supported | WebGL2 supports UBOs |
| MRT | `GL.DrawBuffers` | ✅ Supported | WebGL2 supports MRT |
| Compute shaders | N/A (Phase 8+) | ❌ Not in WebGL2 | Use GLSL ES fragment ping-pong if needed |

---

## Shader Adaptation

### GLSL 330 → GLSL ES 300 Diff

```glsl
// Desktop (GLSL 330 core)
#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aColor;
out vec3 fragColor;

// WebGL2 (GLSL ES 300)
#version 300 es
precision mediump float;
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aColor;
out vec3 fragColor;
```

Fragment shader output:
```glsl
// Desktop
out vec4 outputColor;

// WebGL2 — same syntax, ES 300 supports layout(location=0) out
out vec4 outputColor;  // location 0 is implicit
```

---

## Input Mapping

| Desktop | Browser |
|---------|---------|
| `KeyboardState.IsKeyDown(Keys.W)` | `keydown/keyup` events on `document` |
| `MouseState.Delta` | `mousemove` event |
| `MouseState.ScrollDelta` | `wheel` event |
| Touch (Phase 9) | `touchstart/touchmove/touchend` |

---

## Asset Loading

| Desktop | Browser |
|---------|---------|
| `File.ReadAllText` (synchronous) | `fetch()` (async) — preload all assets before game loop starts |
| JSON deserialization via `System.Text.Json` | `System.Text.Json` via `HttpAssetTextSource` |

**Strategy**: preload all required JSON assets during a loading screen phase.

---

## Build Pipeline

CI (`deploy-pages.yml`) publishes `SharpOpenGl.Browser` to `docs/` on every push to `master`. Local dev: `dotnet run --project SharpOpenGl.Browser`.

---

## Current Status

- **Blazor WASM** — full menus, missions, and gameplay via shared `SharpOpenGl.Engine`
- **Desktop** — OpenTK 4.8 via `SharpOpenGl` exe
- Legacy standalone `docs/engine.js` removed; shader notes above still apply

---

*Last updated: Blazor WASM browser build*