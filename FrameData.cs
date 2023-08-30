
using eyecandy;
using OpenTK.Graphics.OpenGL;

namespace multipass;
internal class FrameData
{
    ///////////////////////////////////////////////////////////////////////////////
    // Two triangles forming a quad which covers the whole display area
    float[] vertices =
    {
          // position             texture coords
             1.0f,  1.0f, 0.0f,   1.0f, 1.0f,     // top right
             1.0f, -1.0f, 0.0f,   1.0f, 0.0f,     // bottom right
            -1.0f, -1.0f, 0.0f,   0.0f, 0.0f,     // bottom left
            -1.0f,  1.0f, 0.0f,   0.0f, 1.0f      // top left
    };

    readonly uint[] indices =
    {
        0, 1, 3,
        1, 2, 3
    };
    // End quad geometry
    ///////////////////////////////////////////////////////////////////////////////

    // Because all of the shaders use the same do-nothing vertex shader, we can
    // re-use the VAO, and the initialize call only needs one shader program to
    // use as a reference to find the vert attribute locations. If the vert shaders
    // were different, we'd have to store the VAO and run the init for each shader.
    int VertexArrayObject;

    public void InitializeVertexData(Shader shader)
    {
        int ElementBufferObject;
        int VertexBufferObject;

        shader.Use();

        VertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(VertexArrayObject);

        VertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        ElementBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        var locationVertices = shader.GetAttribLocation("vertices");
        GL.EnableVertexAttribArray(locationVertices);
        GL.VertexAttribPointer(locationVertices, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        //                                       ^ 3 vertex is 3 floats                   ^ 5 per row        ^ 0 offset per row

        var locationTexCoords = shader.GetAttribLocation("vertexTexCoords");
        GL.EnableVertexAttribArray(locationTexCoords);
        GL.VertexAttribPointer(locationTexCoords, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        //                                        ^ tex coords is 2 floats                 ^ 5 per row        ^ 4th and 5th float in each row
    }

    public void Draw()
    {
        GL.BindVertexArray(VertexArrayObject);
        GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);
    }
}
