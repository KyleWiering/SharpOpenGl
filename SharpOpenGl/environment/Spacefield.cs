using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace SharpOpenGl.environment
{
    class Spacefield : IEnvironment
    {
        int glSpaceField;

        public Spacefield()
        {
            
        }

        public void Initialize()
        {
            Random rand = new Random();
            glSpaceField = GL.GenLists(1);
            GL.NewList(glSpaceField, ListMode.Compile);
            Console.Out.WriteLine("Space Field Initialized");
            // GL.Disable(EnableCap.Lighting);
            GL.Begin(PrimitiveType.Points);
            GL.Rotate(0, 1.0f, 0.0f, .0f);
            GL.Rotate(0, 0.0f, 1.0f, .0f);
            GL.Rotate(0, 0.0f, 0.0f, 1.0f);
            GL.Translate(0, 0, 0);

            float r, g, b;

            for (int i = 0; i < 1000; i++)
            {//Fun starfield making of random stars
                float xxx = (rand.Next(1000)) - 500;
                float yyy = (rand.Next(1000)) - 500;
                float zzz = (rand.Next(1000)) - 500;

                //GL.Color3(Color.White);
                r = rand.Next(5);
                g = rand.Next(5);
                b = rand.Next(5);
                //need to set color
                GL.Color3((r / 5) + .5, (g / 5) + .5, (b / 5) + .5);
                // GL.Color3(Color.White);
                GL.Vertex3(xxx, yyy, zzz);
            }
            GL.End();

            //GL.Enable(EnableCap.Lighting);
            GL.EndList();
        }

        public void Render()
        {
            GL.CallList(glSpaceField); // if you want to fly 'through' the starfield, don't set it to the camera position.
        }


        void IEnvironment.Update(double EllapsedTime)
        {

        }

        void IEnvironment.Destroy()
        {

        }
    }
}