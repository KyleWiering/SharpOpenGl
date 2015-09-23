using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;

namespace SharpOpenGl
{
    class Model
    {
        public int VertexCount { get; set; }
        public string ModelName { get; set; }
        public string ModelTextureList { get; set; }
        public string ModelMaterialList { get; set; }
        public int CallListId { get; set; }
        public Vector3 ModelOrientationVector { get; set; }
        public Vector3 ModelPositionVector { get; set; }

        public Vector3[] VertexList;
        public Vector3[] NormalList;

        public Model()
        {
            ModelOrientationVector = new Vector3(0,0,0);
            ModelPositionVector = new Vector3(0, 0, 0);
        }

        public void DrawModel()
        {
            GL.PushMatrix();
            GL.Translate(ModelPositionVector.X, ModelPositionVector.Y, ModelPositionVector.Z);
            GL.Rotate(ModelOrientationVector.X, 1.0f, 0.0f, 0.0f);
            GL.Rotate(ModelOrientationVector.Y, 0.0f, 1.0f, 0.0f);
            GL.Rotate(ModelOrientationVector.Z, 0.0f, 0.0f, 1.0f);
            GL.CallList(CallListId);
            GL.PopMatrix();
        }

        public void RenderModel(int CallListPosition)
        {
            RenderModel(CallListPosition, new Vector3((float)0.0, (float)0.0, (float)0.0), new Vector3((float)0.0, (float)0.0, (float)0.0));
        }

        /// <summary>
        /// @TODO - You were here.
        /// </summary>
        /// <param name="CallListPosition"></param>
        /// <param name="Position"></param>
        /// <param name="Orientation"></param>
        public void RenderModel(int CallListPosition, Vector3 Position, Vector3 Orientation)
        {
            CallListId = GL.GenLists(CallListPosition);
            GL.NewList(CallListId, ListMode.Compile);
            
             GL.Translate(Position);
            GL.Rotate(Orientation.X, 1.0f, 0.0f, .0f);
            GL.Rotate(Orientation.Y, 0.0f, 1.0f, .0f);
            GL.Rotate(Orientation.Z, 0.0f, 0.0f, 1.0f);
           
            GL.Begin(PrimitiveType.Triangles);
            
           


            //glTranslatef(x,y,z);
            //glRotatef(xx,1.0f,0.0f,0.0f);
            //glRotatef(yy,0.0f,1.0f,0.0f);
            //glRotatef(zz,0.0f,0.0f,1.0f);
            //float r,g,b;
	
            ////glBegin(GL_LINE_STRIP);
            //glBegin(GL_TRIANGLES);				// start drawing a pyramid
            ////glBegin(GL_POINTS);

            //for(int i=0; i<ship.size; i++){
            //    r=rand()%5;
            //g=rand()%5;
            //b=rand()%5;
            //GLfloat the_color[] = {1.0f,1.0f,1.0f,1.0f};
            //the_color[0]=r; the_color[1]=g; the_color[2]=b; the_color[3] = 1.0f;
            //glMaterialfv(GL_FRONT,GL_SPECULAR,the_color);
	
            //glMaterialfv(GL_FRONT_AND_BACK, GL_AMBIENT_AND_DIFFUSE,the_color);
            //the_color[0]=the_color[1]=the_color[2]=the_color[3] = 0.1f;
            //glMaterialfv(GL_FRONT,GL_EMISSION,the_color);
            //GLfloat shiny = 1.0f;
            //glMaterialf(GL_FRONT_AND_BACK, GL_SHININESS,shiny);//Ships should be shiny
            //    float j=i;
            //    while(j>1)j=j/10;

            //    glVertex3f(ship.first_array[i].v0*scale, ship.first_array[i].v1*scale, ship.first_array[i].v2*scale);//hmm, not sure if scale will work, don't think so.  just a 'quick' idea, never full implemented
            //}
            //glEnd();


         
            // GL.Disable(EnableCap.Lighting);
           
            
            float r, g, b;
            Random random = new Random();

            for (int i = 0; i < 1000; i++)
            {//Fun starfield making of random stars
                float xxx = (random.Next(1000)) - 500;
                float yyy = (random.Next(1000)) - 500;
                float zzz = (random.Next(1000)) - 500;

                //GL.Color3(Color.White);
                r = random.Next(5);
                g = random.Next(5);
                b = random.Next(5);
                //need to set color
                GL.Color3((r / 5) + .5, (g / 5) + .5, (b / 5) + .5);
                // GL.Color3(Color.White);
                GL.Vertex3(xxx, yyy, zzz);
            }
            GL.End();

          
            GL.EndList();
        }

        public void SetLocation(float x, float y, float z, float xr, float yr, float zr)
        {
            ModelPositionVector = new Vector3(x,y,z);
            ModelOrientationVector = new Vector3(xr, yr, zr);
        }

        public void AddX(float x)
        {
            ModelPositionVector = new Vector3(ModelPositionVector.X + x, ModelPositionVector.Y, ModelPositionVector.Z);
        }

        public void AddY(float y)
        {
            ModelPositionVector = new Vector3(ModelPositionVector.X , ModelPositionVector.Y+y, ModelPositionVector.Z);
        }

        public void AddZ(float z)
        {
            ModelPositionVector = new Vector3(ModelPositionVector.X , ModelPositionVector.Y, ModelPositionVector.Z+z);
        }

        public void AddXRot(float x)
        {
            ModelOrientationVector = new Vector3(ModelOrientationVector.X+x, ModelOrientationVector.Y, ModelOrientationVector.Z);
        }

        public void AddYRot( float y)
        {
            ModelOrientationVector = new Vector3(ModelOrientationVector.X, ModelOrientationVector.Y+y, ModelOrientationVector.Z);
        }

        public void AddZRot(float z)
        {
            ModelOrientationVector = new Vector3(ModelOrientationVector.X, ModelOrientationVector.Y, ModelOrientationVector.Z + z);
        }
    }
}
