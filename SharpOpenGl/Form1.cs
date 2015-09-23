using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Diagnostics;

namespace SharpOpenGl
{
    public partial class Form1 : Form
    {
        bool isOpenGlInitialized = false;
        int x = 0;
        float rotation = 0;
        Stopwatch sw;
        Camera Camera;
        int SpaceField;
        

        Model Player;
        ButtonHandler ButtonHandler;

        public Form1()
        {
            sw = new Stopwatch(); // available to all event handlers
            InitializeComponent();
            ButtonHandler = new ButtonHandler();
        }

        // What to do for GL Initialization. 
        private void glControl1_Load(object sender, EventArgs e)
        {
            isOpenGlInitialized = true;
            Camera = new Camera();
            Player = new Model();
            InitializePlayer();
            InitializeSpaceField();
            SetupViewport();
           
            Application.Idle += new EventHandler(Application_Idle);  
        }

        void Application_Idle(object sender, EventArgs e)
        {
            while (glControl1.IsIdle)
            {
                sw.Stop(); // we've measured everything since last Idle run
                double milliseconds = sw.Elapsed.TotalMilliseconds;
                sw.Reset(); // reset stopwatch
                sw.Start(); // restart stopwatch

                
                rotation += (float)milliseconds / 20.0f;   

                TriggerCameraStuff();
                Render();
                sw.Start();   
            }
        }
        
        private void glControl1_Resize(object sender, EventArgs e)
        {
            if (!isOpenGlInitialized)
            return;
            
            SetupViewport();
            Render();
        }

        private void Render()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.MatrixMode(MatrixMode.Modelview);

            int w = glControl1.Width;
            int h = glControl1.Height;

            GL.Viewport(0, 0, w, h); // Use all of the glControl painting area
            GLPerspective(45.0f, w / h, 0.1f, 10000.0f);
           
            GL.LoadIdentity(); //start from zero
           
            
            Camera.AdjustCamera();
            GL.Translate(Camera.CameraPosition);
            GL.CallList(SpaceField); // if you want to fly 'through' the starfield, don't set it to the camera position.
           

            GL.Rotate(Player.ModelOrientationVector.X, 1.0f, 0.0f, .0f);
            GL.Rotate(Player.ModelOrientationVector.Y, 0.0f, 1.0f, .0f);
            GL.Rotate(Player.ModelOrientationVector.Z, 0.0f, 0.0f, 1.0f);
           
            GL.LoadIdentity();
            
            Camera.AdjustCamera();
           
             if (glControl1.Focused)
                GL.Color3(Color.Yellow);
            else
                GL.Color3(Color.Blue);
            GL.Rotate(rotation/2, Vector3.UnitZ); // OpenTK has this nice Vector3 class!

            
            GL.Begin(BeginMode.Triangles);
            
            GL.Vertex3(10, 20, 1);
            GL.Vertex3(100, 20, 1);
            GL.Vertex3(100, 50, 1);
            GL.End();
        
            
            
            glControl1.SwapBuffers();
        }

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            if (!isOpenGlInitialized) // Play nice
            return;
          Render();
        }

        private void SetupViewport()
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);		                                // This Will Clear The Background Color To Black
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);  // Enables Clearing Of The Depth Buffer
            GL.DepthFunc(DepthFunction.Less);                                           // The Type Of Depth Test To Do
            GL.Enable(EnableCap.DepthTest);                                             // Enables Depth Testing


            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();

            int w = glControl1.Width;
            int h = glControl1.Height;
            GL.Viewport(0, 0, w, h); // Use all of the glControl painting area
            GLPerspective(45.0f, w / h, 0.1f, 10000.0f);
        }

        private void glControl1_KeyDown(object sender, KeyEventArgs e)
        {
            ButtonHandler.KeyDown(sender, e);
        }

        private void glControl1_KeyUp(object sender, KeyEventArgs e)
        {
            ButtonHandler.KeyUp(sender, e);
        }

        private void TriggerCameraStuff()
        {
            Camera.MoveXAxis(ButtonHandler.AxisMovement.X);
            Camera.MoveYAxis(ButtonHandler.AxisMovement.Y);
            Camera.MoveZAxis(ButtonHandler.AxisMovement.Z);
            Camera.RotateX(ButtonHandler.AxisRotation.X);
            Camera.RotateY(ButtonHandler.AxisRotation.Y);
            Camera.RotateZ(ButtonHandler.AxisRotation.Z);

            Player.AddX(ButtonHandler.AxisMovement.X);
            Player.AddY(ButtonHandler.AxisMovement.Y);
            Player.AddZ(ButtonHandler.AxisMovement.Z);
            Player.AddXRot(ButtonHandler.AxisRotation.X);
            Player.AddYRot(ButtonHandler.AxisRotation.Y);
            Player.AddZRot(ButtonHandler.AxisRotation.Z);
        }

        public void InitializePlayer()
        {
            Player.SetLocation(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
        }

        public void InitializeSpaceField()
        {
            Random rand = new Random();
            SpaceField = GL.GenLists(1);
            GL.NewList(SpaceField, ListMode.Compile);
            Console.Out.WriteLine("Space Field Initialized");
           // GL.Disable(EnableCap.Lighting);
            GL.Begin(PrimitiveType.Points);
            GL.Rotate(0, 1.0f, 0.0f, .0f);
            GL.Rotate(0, 0.0f, 1.0f, .0f);
            GL.Rotate(0, 0.0f, 0.0f, 1.0f);
            GL.Translate(0,0,0);
        
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

        void GLPerspective(double fovY, double aspect, double zNear, double zFar)
        {
            double fW, fH;

            fH = Math.Tan(fovY / 360 * Math.PI) * zNear;
            fW = fH * aspect;

            GL.Frustum(-fW, fW, -fH, fH, zNear, zFar);
        }
    }
}
