using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;

namespace SharpOpenGl.environment
{
    class Model : IEnvironment
    {

        float rotation;
        int glTriangleObject;

        public Model()
        {

        }

        public void Initialize()
        {
            rotation = 0.0f;
            glTriangleObject = GL.GenLists(2);
            Console.Out.WriteLine("Model Initialized");
            GL.NewList(glTriangleObject, ListMode.Compile);
            GL.Begin(PrimitiveType.Lines);
            GL.Color3(Color.Yellow);
            
                
            GL.Vertex3(10, 20, 1);
            GL.Vertex3(100, 20, 1);
            GL.Vertex3(100, 20, 1);
            GL.Vertex3(100, 110, 1);


            GL.End();
            GL.EndList();
        }

        public void Render()
        {

            GL.PushMatrix();
            GL.Rotate(rotation / 2, Vector3.UnitZ);
            GL.CallList(glTriangleObject); // if you want to fly 'through' the starfield, don't set it to the camera position.
            GL.PopMatrix();
        }


        public void Update(double EllapsedTime)
        {
            //This will rotate everything...
            
           
            rotation += (float)EllapsedTime / 20.0f;
            





            //GL.PushMatrix();
            // OpenTK has this nice Vector3 class!
        }

        void IEnvironment.Destroy()
        {

        }

    }
}
