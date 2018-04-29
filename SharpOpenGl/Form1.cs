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
using SharpOpenGl.environment;

namespace SharpOpenGl
{
    public partial class Form1 : Form
    {
        bool isOpenGlInitialized = false;
        int x = 0;
        float rotation = 0;
        Stopwatch sw;
        Camera Camera;
        EnvironmentController EnvironmentController;
        double EllapsedTime;
        ButtonHandler ButtonHandler;

        public Form1()
        {
            EllapsedTime = 0;
            sw = new Stopwatch(); // available to all event handlers
            InitializeComponent();
            ButtonHandler = new ButtonHandler();
            EnvironmentController = new EnvironmentController();
        }

        // What to do for GL Initialization. 
        private void glControl1_Load(object sender, EventArgs e)
        {
            isOpenGlInitialized = true;
            Camera = new Camera();
            EnvironmentController.Initialize();
            SetupViewport();
           
            Application.Idle += new EventHandler(Application_Idle);  
        }

        void Application_Idle(object sender, EventArgs e)
        {
            while (glControl1.IsIdle)
            {
                sw.Stop(); // we've measured everything since last Idle run
                EllapsedTime = sw.Elapsed.TotalMilliseconds;
                sw.Reset(); // reset stopwatch
                sw.Start(); // restart stopwatch

                TriggerCameraStuff();
                Render(EllapsedTime);
                sw.Start();   
            }
        }
        
        private void glControl1_Resize(object sender, EventArgs e)
        {
            if (!isOpenGlInitialized)
            return;
            
            SetupViewport();
            Render(EllapsedTime);
        }

        private void Render(double EllapsedTime)
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
            EnvironmentController.ExternalRender();
            GL.LoadIdentity();
            
            Camera.AdjustCamera();
           
             if (glControl1.Focused)
                GL.Color3(Color.Yellow);
            else
                GL.Color3(Color.Blue);

            GL.Rotate(rotation / 2, Vector3.UnitZ); // OpenTK has this nice Vector3 class!

            EnvironmentController.Update(EllapsedTime);
            EnvironmentController.Render();



            glControl1.SwapBuffers();
        }

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            if (!isOpenGlInitialized) // Play nice
            return;
             Render(EllapsedTime);
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
        }

        public void InitializeEnvironment()
        {
            EnvironmentController test = new EnvironmentController();
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
